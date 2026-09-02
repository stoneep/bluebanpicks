using System;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 대기실(포맷/진영 편집) ~ 드래프트 진행 ~ 종료까지를 담당하는 호스트 권위형 세션.
///
/// 설계 원칙:
///  - RuleManager(순수 C# 규칙 엔진)는 오직 서버(호스트)에서만 인스턴스화되고 호출된다.
///    클라이언트는 RuleManager를 직접 만들지도, 갖고 있지도 않는다 (치팅/디싱크 방지).
///  - 클라이언트는 이 컴포넌트의 NetworkVariable/NetworkList만 구독해서 화면을 그린다.
///    즉 State/Format/ActionLog/CurrentSide/CurrentPhaseName이 "클라이언트가 보는 진실"이고,
///    이 값들은 전부 서버가 RuleManager 이벤트를 받아 갱신한다.
///  - 액션 제출은 반드시 SubmitActionServerRpc를 통해서만 이루어지고,
///    서버가 RuleManager로 검증한 뒤 성공하면 ActionLog(NetworkList)에 추가한다.
///    ActionLog가 곧 진행 기록이므로, 드래프트 도중 접속한 클라이언트도
///    NetworkList의 초기 동기화만으로 지금까지의 결과를 그대로 복원할 수 있다(late-join 대응).
///
/// 파일 구성 (모두 partial class로 같은 타입 - Netcode가 NetworkVariable/RPC를 인식하려면
/// 필드/RPC 메서드가 반드시 이 NetworkBehaviour 타입 자신에 있어야 하므로, 별도 컴포넌트로
/// 쪼개는 대신 책임별로 파일만 나눴다):
///  - DraftSessionServer.cs         : 필드 선언, 생명주기, LocalSide (이 파일)
///  - DraftSessionServer.Lobby.cs   : 대기실 편집 (호스트 전용 Set 계열 메서드)
///  - DraftSessionServer.Nicknames.cs : 접속/해제에 따른 닉네임 등록·해제
///  - DraftSessionServer.Flow.cs    : 드래프트 시작 → 씬 전환 → 로딩 → 진행 → 종료 흐름
///  - DraftSessionServer.Turns.cs   : 진행 중 액션 제출/일시정지 RPC, 턴 제한시간
/// </summary>
public partial class DraftSessionServer : NetworkBehaviour
{
    /// <summary>
    /// 현재 스폰돼 있는 세션 인스턴스. 서버/클라이언트 구분 없이,
    /// "이 로컬 프로세스에 이 NetworkObject가 동기화 완료된 시점"에 채워진다.
    /// DraftBoardController처럼 씬에 미리 배치되지 않은 스크립트가
    /// 런타임에 세션을 찾아 Bind()할 때 이 값을 쓰면 된다.
    /// </summary>
    public static DraftSessionServer Instance { get; private set; }

    /// <summary>
    /// Instance가 채워지는 시점(OnNetworkSpawn)에 발행. 이미 스폰된 이후에 구독하면
    /// 놓칠 수 있으므로, 구독 전에 항상 Instance가 이미 null이 아닌지 먼저 확인할 것.
    /// </summary>
    public static event Action<DraftSessionServer> OnSessionReady;

    // ---------- 대기실: 포맷 편집 ----------

    /// <summary>대기실에서 호스트가 편집 중인 라운드 목록. 서버만 수정, 전원이 구독 가능.</summary>
    public readonly NetworkList<NetworkDraftRoundConfig> Format = new();

    /// <summary>선공/후공에 배정된 클라이언트 ID. ulong.MaxValue면 미배정.</summary>
    public readonly NetworkVariable<ulong> FirstSideClientId = new(ulong.MaxValue);
    public readonly NetworkVariable<ulong> SecondSideClientId = new(ulong.MaxValue);
    
    /// <summary>
    /// 호스트가 참가자 목록에서 "선수"로 지정한 clientId 목록. 최대 2명까지만 들어간다.
    /// 여기 없는 접속자는 전부 갤러리(관전자)이며, 자동 배정(DraftLobbyController.HandleAutoAssignSides)의
    /// 후보군은 이 목록으로 제한된다. 접속 해제 시 자동으로 제거된다.
    /// </summary>
    public readonly NetworkList<ulong> PlayerCandidateClientIds = new();
    
    // ---------- 대기실: 참가자 닉네임 ----------
    /// <summary>clientId별 닉네임. 접속 시 등록되고 접속 해제 시 제거된다. 전원 구독 가능.</summary>
    public readonly NetworkList<ClientNicknameEntry> Nicknames = new();

    public readonly NetworkVariable<DraftSessionState> State = new(DraftSessionState.Lobby);

    [Header("Scene Transition")]
    [Tooltip("드래프트 시작 시 전환할 씬 이름. Build Settings(File > Build Settings > Scenes In Build)에 " +
             "먼저 등록되어 있어야 하고, NetworkManager 인스펙터에서 Enable Scene Management가 켜져 있어야 한다.")]
    [SerializeField] private string draftSceneName = "MainLobby";

    [Header("Timers (기본값 - 대기실에서 HostSetTimerSettings로 덮어쓸 수 있음)")]
    [Tooltip("밴픽씬(MainLobby) 로드가 끝난 직후, 혹시 모를 클라이언트 UI/에셋 로딩 지연을 위해 " +
             "실제 밴/픽 시작 전에 대기하는 시간(초). 이 시간 동안 State는 Loading이다.")]
    [SerializeField] private float defaultPreDraftLoadBufferSeconds = 15f;

    [Tooltip("밴/픽 각 턴마다 주어지는 제한 시간(초). 시간 안에 선택하지 않으면 서버가 " +
             "남아있는 캐릭터 중 하나를 자동으로 대신 선택한다. 0 이하로 두면 턴 타이머를 쓰지 않는다.")]
    [SerializeField] private float defaultTurnTimeLimitSeconds = 30f;

    [Tooltip("밴/픽이 모두 끝난(Completed) 직후 보여줄 서버 권위 카운트다운 시간(초). " +
             "0보다 크면 PostDraftSecondsRemaining이 이 값에서 0까지 카운트다운된다(모든 클라이언트 동일). " +
             "0 이하로 두면 카운트다운을 쓰지 않고, PostDraftTimerIndicator가 대신 종료 시점부터의 " +
             "경과 시간을 각자 로컬로 세어 보여준다(기존 방식).")]
    [SerializeField] private float defaultPostDraftDisplaySeconds = 10f;

    /// <summary>
    /// 실제로 쓰이는 preDraftLoadBufferSeconds 값. 인스펙터 기본값(defaultPreDraftLoadBufferSeconds)으로
    /// OnNetworkSpawn 때 초기화되고, 이후 대기실(Lobby)에서 HostSetTimerSettings로 바꿀 수 있다.
    /// 세션 전체에 공통으로 적용되는 값이라 라운드마다 다르지 않다.
    /// </summary>
    public readonly NetworkVariable<float> PreDraftLoadBufferSeconds = new(15f);

    /// <summary>실제로 쓰이는 turnTimeLimitSeconds 값. PreDraftLoadBufferSeconds와 동일한 방식.</summary>
    public readonly NetworkVariable<float> TurnTimeLimitSeconds = new(30f);

    /// <summary>
    /// 실제로 쓰이는 postDraftDisplaySeconds 값. PreDraftLoadBufferSeconds와 동일한 방식으로
    /// OnNetworkSpawn 때 defaultPostDraftDisplaySeconds로 초기화되고, 이후 대기실에서
    /// HostSetTimerSettings로 바꿀 수 있다. 0 이하면 PostDraftTimerIndicator가 카운트다운 대신
    /// 경과 시간 표시로 동작한다.
    /// </summary>
    public readonly NetworkVariable<float> PostDraftDisplaySeconds = new(10f);

    /// <summary>Loading 상태에서 남은 대기 시간(초). Loading이 아닐 때는 0.</summary>
    public readonly NetworkVariable<float> PreDraftSecondsRemaining = new(0f);

    /// <summary>현재 턴에 남은 제한 시간(초). 턴 타이머가 꺼져있거나 진행 중이 아니면 0.</summary>
    public readonly NetworkVariable<float> TurnSecondsRemaining = new(0f);

    /// <summary>
    /// Completed 상태에서 남은 안내 카운트다운 시간(초). PostDraftDisplaySeconds가 0 이하로 설정된
    /// 경우에는 항상 0으로 유지되며, 이때는 PostDraftTimerIndicator가 경과 시간 표시로 대체한다.
    /// </summary>
    public readonly NetworkVariable<float> PostDraftSecondsRemaining = new(0f);

    /// <summary>
    /// true면 PreDraft/턴 타이머가 그 자리에서 멈추고, 밴/픽 제출도 서버에서 거부된다("완전 정지").
    /// 코루틴 자체를 취소/재시작하지 않고 매 프레임 값 감소만 건너뛰는 방식이라, 해제 시 정확히
    /// 멈췄던 남은 시간부터 다시 흐른다. 호스트 또는 배정된 참가자(선공/후공)만 토글 가능
    /// (RequestPauseServerRpc 참고) - 네트워크 문제/분쟁 상황 등을 위한 보험용 기능이다.
    /// </summary>
    public readonly NetworkVariable<bool> IsPaused = new(false);

    // PreDraft/Turn/PostDraft 세 카운트다운이 공유하던 코루틴 로직은 NetworkCountdown으로 위임한다.
    // 서버에서만 쓰이므로 OnNetworkSpawn의 IsServer 분기에서 생성한다.
    private NetworkCountdown preDraftCountdown;
    private NetworkCountdown turnCountdown;
    private NetworkCountdown postDraftCountdown;

    // ---------- 진행 중 상태 (서버가 갱신, 클라는 읽기만) ----------

    public readonly NetworkVariable<FixedString32Bytes> CurrentPhaseName = new();
    public readonly NetworkVariable<DraftSide> CurrentSide = new();
    public readonly NetworkList<NetworkDraftAction> ActionLog = new();

    /// <summary>
    /// 이 로컬 클라이언트가 배정된 진영. 참가자가 아니거나(관전자/호스트) 
    /// 아직 배정되지 않았다면 null.
    /// </summary>
    public DraftSide? LocalSide
    {
        get
        {
            ulong localId = NetworkManager.Singleton != null ? NetworkManager.Singleton.LocalClientId : ulong.MaxValue;
            if (localId == FirstSideClientId.Value) return DraftSide.First;
            if (localId == SecondSideClientId.Value) return DraftSide.Second;
            return null;
        }
    }

    /// <summary>액션 거부 사유. 요청을 보낸 클라이언트에게만 전달된다 (전체 브로드캐스트 아님).</summary>
    public event Action<string> OnActionRejected;

    // 서버 전용 - 클라이언트에는 절대 존재/노출되지 않음
    private RuleManager ruleManager;

    public override void OnNetworkSpawn()
    {
        Instance = this;

        if (IsServer)
        {
            // NetworkVariable의 생성자 기본값이 아니라 인스펙터에 설정된 값으로 시작하도록 스폰 시점에 반영.
            PreDraftLoadBufferSeconds.Value = defaultPreDraftLoadBufferSeconds;
            TurnTimeLimitSeconds.Value = defaultTurnTimeLimitSeconds;
            PostDraftDisplaySeconds.Value = defaultPostDraftDisplaySeconds;

            // PreDraft/Turn 카운트다운은 IsPaused 체크가 필요하고, PostDraft(종료 후 안내)는
            // 필요 없다(드래프트가 이미 끝난 뒤라 "일시정지"라는 개념 자체가 적용되지 않음).
            preDraftCountdown = new NetworkCountdown(this, PreDraftSecondsRemaining, () => IsPaused.Value);
            turnCountdown = new NetworkCountdown(this, TurnSecondsRemaining, () => IsPaused.Value);
            postDraftCountdown = new NetworkCountdown(this, PostDraftSecondsRemaining);

            // 이미 붙어있는 클라이언트(호스트 자신 포함)도 놓치지 않도록, 구독 직후 한 번 훑어준다.
            NetworkManager.OnClientConnectedCallback += HandleClientConnectedForNickname;
            NetworkManager.OnClientDisconnectCallback += HandleClientDisconnectedForNickname;
            foreach (var id in NetworkManager.ConnectedClientsIds)
                HandleClientConnectedForNickname(id);
        }

        Debug.Log($"[{nameof(DraftSessionServer)}] OnNetworkSpawn (session={GetEntityId()}, " +
                  $"scene={gameObject.scene.name}) @ frame {Time.frameCount}");
        RaiseSessionReadySafely();
    }

    /// <summary>
    /// OnSessionReady 구독자 중 하나가 예외를 던지면, 일반 멀티캐스트 delegate 호출(?.Invoke)은
    /// 나머지 구독자 호출을 건너뛰고 예외를 그대로 호출자(OnNetworkSpawn → NetworkObject.Spawn())
    /// 위로 전파시킨다. 그러면 Spawn()을 호출한 DraftSessionBootstrap.SpawnSession()의
    /// 이후 코드(스폰 완료 로그, 대기실 씬 전환)까지 통째로 실행되지 않는다.
    /// 구독자 하나의 버그가 씬 전환 같은 핵심 흐름을 막지 않도록, 각 구독자를 개별적으로
    /// try/catch로 감싸서 호출한다.
    /// </summary>
    private void RaiseSessionReadySafely()
    {
        var handler = OnSessionReady;
        if (handler == null) return;

        foreach (var d in handler.GetInvocationList())
        {
            var action = (Action<DraftSessionServer>)d;
            try
            {
                action(this);
            }
            catch (Exception e)
            {
                Debug.LogError($"[{nameof(DraftSessionServer)}] OnSessionReady 구독자({action.Target}) 처리 중 예외 발생. " +
                                "이 구독자는 건너뛰고 나머지 초기화(씬 전환 포함)는 계속 진행합니다.");
                Debug.LogException(e);
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        Debug.Log($"[{nameof(DraftSessionServer)}] OnNetworkDespawn (session={GetEntityId()}, " +
                  $"scene={gameObject.scene.name}) @ frame {Time.frameCount}");
        if (Instance == this) Instance = null;

        if (IsServer && NetworkManager != null)
        {
            NetworkManager.OnClientConnectedCallback -= HandleClientConnectedForNickname;
            NetworkManager.OnClientDisconnectCallback -= HandleClientDisconnectedForNickname;
        }
    }

    public override void OnDestroy()
    {
        // NetworkVariable 값(Stop())까지는 건드리지 않고 코루틴만 멈춘다 - 이미 스폰 해제 중일 수 있는
        // 시점이라 NetworkVariable 쓰기가 안전하지 않을 수 있다 (클라이언트에서는 애초에 null이라 no-op).
        preDraftCountdown?.Cancel();
        turnCountdown?.Cancel();
        postDraftCountdown?.Cancel();

        if (ruleManager != null)
        {
            ruleManager.OnActionSubmitted -= HandleServerActionSubmitted;
            ruleManager.OnPhaseChanged -= HandleServerPhaseChanged;
            ruleManager.OnDraftCompleted -= HandleServerDraftCompleted;
        }
        if (NetworkManager != null && NetworkManager.SceneManager != null)
        {
            NetworkManager.SceneManager.OnLoadEventCompleted -= HandleDraftSceneLoaded;
        }
        if (Instance == this) Instance = null;
        base.OnDestroy();
    }
}
