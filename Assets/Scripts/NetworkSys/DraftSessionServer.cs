using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

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
/// </summary>
public class DraftSessionServer : NetworkBehaviour
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
    /// true면 호스트(ServerClientId)도 선공/후공에 배정될 수 있다 ("2인 연습 모드").
    /// 기본값 false = 기존 규칙 그대로 "호스트는 항상 관전자".
    /// 관전자 역할의 3번째 인원 없이, 실제로 대결할 두 사람 중 한 명이 방을 만들고
    /// 자기 자신도 선공/후공 중 하나로 배정해 바로 시작하고 싶을 때 이 값을 켠다.
    /// Lobby 상태에서만 변경 가능(HostSetHostCanPlay).
    /// </summary>
    public readonly NetworkVariable<bool> HostCanPlay = new(false);

    public readonly NetworkVariable<DraftSessionState> State = new(DraftSessionState.Lobby);

    [Header("Scene Transition")]
    [Tooltip("드래프트 시작 시 전환할 씬 이름. Build Settings(File > Build Settings > Scenes In Build)에 " +
             "먼저 등록되어 있어야 하고, NetworkManager 인스펙터에서 Enable Scene Management가 켜져 있어야 한다.")]
    [SerializeField] private string draftSceneName = "MainLobby";

    [Header("Timers")]
    [Tooltip("밴픽씬(MainLobby) 로드가 끝난 직후, 혹시 모를 클라이언트 UI/에셋 로딩 지연을 위해 " +
             "실제 밴/픽 시작 전에 대기하는 시간(초). 이 시간 동안 State는 Loading이다.")]
    [SerializeField] private float preDraftLoadBufferSeconds = 15f;

    [Tooltip("밴/픽 각 턴마다 주어지는 제한 시간(초). 시간 안에 선택하지 않으면 서버가 " +
             "남아있는 캐릭터 중 하나를 자동으로 대신 선택한다. 0 이하로 두면 턴 타이머를 쓰지 않는다.")]
    [SerializeField] private float turnTimeLimitSeconds = 30f;

    /// <summary>Loading 상태에서 남은 대기 시간(초). Loading이 아닐 때는 0.</summary>
    public readonly NetworkVariable<float> PreDraftSecondsRemaining = new(0f);

    /// <summary>현재 턴에 남은 제한 시간(초). 턴 타이머가 꺼져있거나 진행 중이 아니면 0.</summary>
    public readonly NetworkVariable<float> TurnSecondsRemaining = new(0f);

    /// <summary>
    /// true면 PreDraft/턴 타이머가 그 자리에서 멈추고, 밴/픽 제출도 서버에서 거부된다("완전 정지").
    /// 코루틴 자체를 취소/재시작하지 않고 매 프레임 값 감소만 건너뛰는 방식이라, 해제 시 정확히
    /// 멈췄던 남은 시간부터 다시 흐른다. 호스트 또는 배정된 참가자(선공/후공)만 토글 가능
    /// (RequestPauseServerRpc 참고) - 네트워크 문제/분쟁 상황 등을 위한 보험용 기능이다.
    /// </summary>
    public readonly NetworkVariable<bool> IsPaused = new(false);

    private Coroutine preDraftCountdownRoutine;
    private Coroutine turnTimerRoutine;

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
        Debug.Log($"[{nameof(DraftSessionServer)}] OnNetworkSpawn (session={GetEntityId()}, " +
                  $"scene={gameObject.scene.name}) @ frame {Time.frameCount}");
        OnSessionReady?.Invoke(this);
    }

    public override void OnNetworkDespawn()
    {
        Debug.Log($"[{nameof(DraftSessionServer)}] OnNetworkDespawn (session={GetEntityId()}, " +
                  $"scene={gameObject.scene.name}) @ frame {Time.frameCount}");
        if (Instance == this) Instance = null;
    }

    // ==================== 대기실: 포맷/진영 편집 (호스트 전용) ====================

    public void HostSetFormat(DraftFormatData data)
    {
        if (!IsServer)
        {
            Debug.LogWarning($"[{nameof(DraftSessionServer)}] HostSetFormat은 서버(호스트)에서만 호출할 수 있습니다.");
            return;
        }
        if (State.Value != DraftSessionState.Lobby)
        {
            Debug.LogWarning($"[{nameof(DraftSessionServer)}] 드래프트 시작 후에는 포맷을 바꿀 수 없습니다.");
            return;
        }

        data.CopyTo(Format);
    }

    public void HostAssignSides(ulong firstClientId, ulong secondClientId)
    {
        if (!IsServer)
        {
            Debug.LogWarning($"[{nameof(DraftSessionServer)}] HostAssignSides는 서버(호스트)에서만 호출할 수 있습니다.");
            return;
        }
        if (State.Value != DraftSessionState.Lobby)
        {
            Debug.LogWarning($"[{nameof(DraftSessionServer)}] 드래프트 시작 후에는 진영을 다시 배정할 수 없습니다.");
            return;
        }
        if (firstClientId == secondClientId)
        {
            Debug.LogError($"[{nameof(DraftSessionServer)}] 선공/후공에 같은 클라이언트를 배정할 수 없습니다.");
            return;
        }
        if (!HostCanPlay.Value &&
            (firstClientId == NetworkManager.ServerClientId || secondClientId == NetworkManager.ServerClientId))
        {
            // 역할 규칙(기본값): 호스트(=ServerClientId)는 관전자다. 드래프트 참가자(선공/후공)는
            // 반드시 호스트가 아닌 클라이언트여야 한다. 이 체크는 서버 권위 지점이므로
            // 호출부(UI)가 실수로 호스트를 넘기더라도 여기서 최종적으로 막는다.
            // 단, HostCanPlay가 켜져 있으면("2인 연습 모드") 호스트도 참가자가 될 수 있으므로
            // 이 방어를 건너뛴다.
            Debug.LogError($"[{nameof(DraftSessionServer)}] 호스트(clientId={NetworkManager.ServerClientId})는 관전자이므로 " +
                            "선공/후공에 배정할 수 없습니다. (HostCanPlay를 켜면 호스트도 참가 가능)");
            return;
        }

        FirstSideClientId.Value = firstClientId;
        SecondSideClientId.Value = secondClientId;
    }

    /// <summary>
    /// "2인 연습 모드" 토글. true로 켜면 호스트 자신도 선공/후공 후보에 포함될 수 있다.
    /// Lobby 상태에서만, 그리고 아직 진영이 배정되지 않았을 때만 바꾸도록 한다
    /// (진행 중간에 규칙이 바뀌는 걸 막기 위함 - 이미 배정된 뒤에 끄면 참가자 중 하나가
    /// 갑자기 관전자 취급되는 모순이 생길 수 있다).
    /// </summary>
    public void HostSetHostCanPlay(bool value)
    {
        if (!IsServer)
        {
            Debug.LogWarning($"[{nameof(DraftSessionServer)}] HostSetHostCanPlay는 서버(호스트)에서만 호출할 수 있습니다.");
            return;
        }
        if (State.Value != DraftSessionState.Lobby)
        {
            Debug.LogWarning($"[{nameof(DraftSessionServer)}] 드래프트 시작 후에는 이 설정을 바꿀 수 없습니다.");
            return;
        }
        if (FirstSideClientId.Value != ulong.MaxValue || SecondSideClientId.Value != ulong.MaxValue)
        {
            Debug.LogWarning($"[{nameof(DraftSessionServer)}] 진영이 이미 배정된 후에는 이 설정을 바꿀 수 없습니다. " +
                              "먼저 진영 배정을 초기화하세요.");
            return;
        }

        HostCanPlay.Value = value;
    }

    // ==================== 대기실 -> 드래프트 시작 (호스트 전용) ====================

    public void HostStartDraft()
    {
        if (!IsServer)
        {
            Debug.LogWarning($"[{nameof(DraftSessionServer)}] HostStartDraft는 서버(호스트)에서만 호출할 수 있습니다.");
            return;
        }
        if (State.Value != DraftSessionState.Lobby)
        {
            Debug.LogWarning($"[{nameof(DraftSessionServer)}] 이미 시작됐거나 종료된 세션입니다.");
            return;
        }
        if (Format.Count == 0)
        {
            Debug.LogError($"[{nameof(DraftSessionServer)}] 라운드가 1개 이상 있어야 드래프트를 시작할 수 있습니다.");
            return;
        }
        if (FirstSideClientId.Value == ulong.MaxValue || SecondSideClientId.Value == ulong.MaxValue)
        {
            Debug.LogError($"[{nameof(DraftSessionServer)}] 선공/후공 진영이 아직 배정되지 않았습니다.");
            return;
        }

        var sceneManager = NetworkManager.SceneManager;
        if (sceneManager == null)
        {
            Debug.LogError($"[{nameof(DraftSessionServer)}] NetworkManager의 Scene Management가 꺼져 있습니다. " +
                            "인스펙터에서 Enable Scene Management를 켜주세요.");
            return;
        }

        sceneManager.OnLoadEventCompleted += HandleDraftSceneLoaded;
        var status = sceneManager.LoadScene(draftSceneName, LoadSceneMode.Single);

        if (status != SceneEventProgressStatus.Started)
        {
            sceneManager.OnLoadEventCompleted -= HandleDraftSceneLoaded;
            Debug.LogError($"[{nameof(DraftSessionServer)}] 씬 전환을 시작하지 못했습니다: {status}. " +
                            $"씬 '{draftSceneName}'이 Build Settings에 등록되어 있는지 확인하세요.");
        }
    }

    /// <summary>
    /// 서버와 모든 클라이언트가 draftSceneName 로드를 마쳤을 때 호출됨.
    /// 이 시점부터 실제로 밴/픽을 시작한다 - 씬 전환 중에 이미 턴이 진행되어
    /// 일부 클라이언트가 첫 턴을 놓치는 상황을 막기 위함.
    /// </summary>
    private void HandleDraftSceneLoaded(string sceneName, LoadSceneMode mode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        if (sceneName != draftSceneName) return;

        NetworkManager.SceneManager.OnLoadEventCompleted -= HandleDraftSceneLoaded;

        if (clientsTimedOut != null && clientsTimedOut.Count > 0)
        {
            Debug.LogWarning($"[{nameof(DraftSessionServer)}] 씬 로드에 실패한 클라이언트: " +
                              string.Join(",", clientsTimedOut));
        }

        BeginPreDraftCountdown();
    }

    /// <summary>
    /// 밴픽씬 로드가 전원 완료된 시점 ~ 실제 드래프트 시작 사이에 유예 시간을 둔다.
    /// 씬 전환 자체는 끝났어도 캐릭터 아이콘 로드(Addressables 등) 같은 클라이언트 UI 준비가
    /// 아직 안 끝났을 수 있어서, "혹시 모를" 여유 시간을 준 뒤 자동으로 밴/픽을 시작시킨다.
    /// </summary>
    private void BeginPreDraftCountdown()
    {
        State.Value = DraftSessionState.Loading;
        IsPaused.Value = false; // Loading 진입 시에도 이전 상태가 남아있지 않도록 확실히 초기화
        Debug.Log($"[{nameof(DraftSessionServer)}] State.Value set to Loading, " +
                  $"{preDraftLoadBufferSeconds}초 후 자동으로 드래프트를 시작합니다.");

        if (preDraftCountdownRoutine != null) StopCoroutine(preDraftCountdownRoutine);
        preDraftCountdownRoutine = StartCoroutine(PreDraftCountdownRoutine());
    }

    private IEnumerator PreDraftCountdownRoutine()
    {
        float remaining = Mathf.Max(0f, preDraftLoadBufferSeconds);
        PreDraftSecondsRemaining.Value = Mathf.Ceil(remaining);

        while (remaining > 0f)
        {
            yield return null;

            // 일시정지 중에는 이번 프레임의 경과 시간을 그냥 버린다 - remaining을 건드리지 않으므로
            // 코루틴을 취소/재시작하지 않고도 정확히 멈췄던 지점에서 다시 흐르게 된다.
            if (IsPaused.Value)
            {
                Debug.Log($"[PreDraftTimer] paused, skip. remaining={remaining}"); // 임시
                continue;
            }

            remaining -= Time.deltaTime;

            // NetworkVariable은 값이 실제로 바뀔 때만 트래픽을 보내므로, 프레임마다가 아니라
            // 초 단위(올림)로만 갱신해서 불필요한 동기화를 줄인다.
            float rounded = Mathf.Max(0f, Mathf.Ceil(remaining));
            if (!Mathf.Approximately(rounded, PreDraftSecondsRemaining.Value))
            {
                PreDraftSecondsRemaining.Value = rounded;
                Debug.Log($"[PreDraftTimer] tick -> {rounded}"); // 임시
            }
        }

        PreDraftSecondsRemaining.Value = 0f;
        preDraftCountdownRoutine = null;
        BeginDraft();
    }

    private void BeginDraft()
    {
        var formatData = Format.ToDraftFormatData();

        ruleManager = new RuleManager(formatData);
        ruleManager.OnActionSubmitted += HandleServerActionSubmitted;
        ruleManager.OnPhaseChanged += HandleServerPhaseChanged;
        ruleManager.OnDraftCompleted += HandleServerDraftCompleted;

        ActionLog.Clear();
        IsPaused.Value = false; // 혹시 남아있을 수 있는 이전 상태를 새 드래프트 시작 시 확실히 초기화
        State.Value = DraftSessionState.InProgress;
        Debug.Log($"[{nameof(DraftSessionServer)}] State.Value set to InProgress " +
                  $"(session={GetEntityId()}, IsSpawned={NetworkObject.IsSpawned}, " +
                  $"scene={gameObject.scene.name}) @ frame {Time.frameCount}");
        ruleManager.StartDraft(); // 내부에서 OnPhaseChanged가 발행되어 첫 턴 타이머도 자동으로 시작된다.
    }

    // ==================== 진행 중: 액션 제출 (모든 클라이언트) ====================

    [ServerRpc(RequireOwnership = false)]
    public void SubmitActionServerRpc(string characterId, ServerRpcParams rpcParams = default)
    {
        var senderClientId = rpcParams.Receive.SenderClientId;

        if (State.Value != DraftSessionState.InProgress || ruleManager == null)
        {
            RejectClientRpc("드래프트가 진행 중이 아닙니다.", ToTarget(senderClientId));
            return;
        }

        if (IsPaused.Value)
        {
            RejectClientRpc("일시정지 중에는 밴/픽을 제출할 수 없습니다.", ToTarget(senderClientId));
            return;
        }

        if (!TryResolveSide(senderClientId, out var side))
        {
            RejectClientRpc("이 세션에 배정된 진영이 아닙니다.", ToTarget(senderClientId));
            return;
        }

        if (!ruleManager.SubmitAction(side, characterId, out var error))
        {
            RejectClientRpc(error, ToTarget(senderClientId));
        }

        // 성공했을 때는 별도로 브로드캐스트하지 않는다.
        // HandleServerActionSubmitted가 ActionLog(NetworkList)에 추가하고,
        // NetworkList/NetworkVariable의 자동 동기화가 모든 클라이언트(및 이후 접속자)에게 전파한다.
    }

    private bool TryResolveSide(ulong clientId, out DraftSide side)
    {
        if (clientId == FirstSideClientId.Value) { side = DraftSide.First; return true; }
        if (clientId == SecondSideClientId.Value) { side = DraftSide.Second; return true; }
        side = default;
        return false;
    }

    private static ClientRpcParams ToTarget(ulong clientId) => new ClientRpcParams
    {
        Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId } }
    };

    [ClientRpc]
    private void RejectClientRpc(string reason, ClientRpcParams rpcParams = default) =>
        OnActionRejected?.Invoke(reason);

    // ==================== 진행 중: 일시정지(보험용 긴급 정지) ====================

    /// <summary>
    /// 일시정지 상태를 요청한다. pause=true면 정지, false면 해제.
    /// 호스트(ServerClientId) 또는 이 세션에 배정된 참가자(선공/후공)만 호출할 수 있고,
    /// 그 외(관전자 등)의 요청은 서버에서 거부한다.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void RequestPauseServerRpc(bool pause, ServerRpcParams rpcParams = default)
    {
        var senderClientId = rpcParams.Receive.SenderClientId;

        if (State.Value != DraftSessionState.Loading && State.Value != DraftSessionState.InProgress)
        {
            RejectClientRpc("드래프트 진행 중(로딩 포함)이 아닐 때는 일시정지를 사용할 수 없습니다.", ToTarget(senderClientId));
            return;
        }

        bool isHostOrParticipant = senderClientId == NetworkManager.ServerClientId ||
                                    senderClientId == FirstSideClientId.Value ||
                                    senderClientId == SecondSideClientId.Value;
        if (!isHostOrParticipant)
        {
            RejectClientRpc("호스트 또는 이 세션의 참가자만 일시정지를 사용할 수 있습니다.", ToTarget(senderClientId));
            return;
        }

        if (IsPaused.Value == pause) return; // 이미 같은 상태면 아무것도 하지 않음

        IsPaused.Value = pause;
        Debug.Log($"[{nameof(DraftSessionServer)}] IsPaused set to {pause} (요청자 clientId={senderClientId}) " +
                  $"@ frame {Time.frameCount}");
    }

    // ==================== 서버 내부: RuleManager 이벤트 -> 동기화 데이터 반영 ====================

    private void HandleServerActionSubmitted(DraftSide side, string characterId, DraftResultType type)
    {
        ActionLog.Add(new NetworkDraftAction
        {
            side = side,
            characterId = characterId,
            resultType = type
        });

        // 페이즈가 아직 안 끝났다면(=같은 페이즈 안에서 다음 사람 차례로 넘어간 것) 여기서 턴 타이머를
        // 다시 시작한다. 페이즈가 끝났다면 곧이어 HandleServerPhaseChanged가 호출되므로 거기서 시작한다.
        if (ruleManager != null && ruleManager.CurrentPhase != null && !ruleManager.CurrentPhase.IsComplete)
        {
            CurrentSide.Value = ruleManager.CurrentPhase.CurrentSide; // 같은 페이즈 내 턴 교대도 반영
            RestartTurnTimer();
        }
    }

    private void HandleServerPhaseChanged(IDraftPhase phase)
    {
        CurrentPhaseName.Value = phase.PhaseName;
        CurrentSide.Value = phase.CurrentSide;
        RestartTurnTimer();
    }

    private void HandleServerDraftCompleted()
    {
        State.Value = DraftSessionState.Completed;
        StopTurnTimer();
    }

    // ==================== 서버 내부: 밴/픽 턴 제한 시간 ====================

    private void RestartTurnTimer()
    {
        if (turnTimerRoutine != null) StopCoroutine(turnTimerRoutine);

        if (turnTimeLimitSeconds <= 0f)
        {
            TurnSecondsRemaining.Value = 0f;
            turnTimerRoutine = null;
            return;
        }

        turnTimerRoutine = StartCoroutine(TurnTimerRoutine());
    }

    private void StopTurnTimer()
    {
        if (turnTimerRoutine != null) StopCoroutine(turnTimerRoutine);
        turnTimerRoutine = null;
        TurnSecondsRemaining.Value = 0f;
    }

    private IEnumerator TurnTimerRoutine()
    {
        float remaining = turnTimeLimitSeconds;
        TurnSecondsRemaining.Value = Mathf.Ceil(remaining);

        while (remaining > 0f)
        {
            yield return null;

            // PreDraftCountdownRoutine과 동일한 방식: 일시정지 중엔 경과 시간을 버려서 그 자리에서 멈춘다.
            if (IsPaused.Value) continue;

            remaining -= Time.deltaTime;

            float rounded = Mathf.Max(0f, Mathf.Ceil(remaining));
            if (!Mathf.Approximately(rounded, TurnSecondsRemaining.Value))
                TurnSecondsRemaining.Value = rounded;
        }

        TurnSecondsRemaining.Value = 0f;
        turnTimerRoutine = null;
        HandleTurnTimedOut();
    }

    /// <summary>
    /// 제한 시간 안에 아무도 선택하지 않았을 때, 서버가 현재 차례인 진영을 대신해
    /// 아직 사용되지 않은 캐릭터 중 하나를 무작위로 골라 제출한다.
    /// SubmitAction이 성공하면 RuleManager의 OnActionSubmitted/OnPhaseChanged가 그대로 발행되므로
    /// ActionLog 반영이나 다음 턴 타이머 시작은 기존 핸들러가 알아서 처리한다.
    /// </summary>
    private void HandleTurnTimedOut()
    {
        if (ruleManager == null || State.Value != DraftSessionState.InProgress) return;

        var phase = ruleManager.CurrentPhase;
        if (phase == null || phase.IsComplete) return;

        var side = phase.CurrentSide;
        var autoPickId = PickRandomAvailableCharacterId();

        if (autoPickId == null)
        {
            Debug.LogWarning($"[{nameof(DraftSessionServer)}] 턴 시간 초과: 자동으로 선택할 수 있는 캐릭터가 없습니다.");
            return;
        }

        if (!ruleManager.SubmitAction(side, autoPickId, out var error))
        {
            Debug.LogError($"[{nameof(DraftSessionServer)}] 턴 시간 초과 자동 선택 실패: {error}");
            return;
        }

        Debug.Log($"[{nameof(DraftSessionServer)}] 턴 시간 초과 - {side}의 {phase.PhaseName}을(를) 자동으로 대신 선택: {autoPickId}");
    }

    /// <summary>CharDatabaseLoader에 로드되어 있는 전체 캐릭터 중, 아직 밴/픽되지 않은 것 하나를 무작위로 반환.</summary>
    private string PickRandomAvailableCharacterId()
    {
        var candidates = new List<string>();
        foreach (var id in CharDatabaseLoader.AllIds)
        {
            if (ruleManager.IsCharacterAvailable(id))
                candidates.Add(id);
        }

        if (candidates.Count == 0) return null;
        return candidates[UnityEngine.Random.Range(0, candidates.Count)];
    }

    public override void OnDestroy()
    {
        if (preDraftCountdownRoutine != null) StopCoroutine(preDraftCountdownRoutine);
        if (turnTimerRoutine != null) StopCoroutine(turnTimerRoutine);

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

/// <summary>대기실(설정 편집) → 로딩 대기 → 진행 중 → 종료, 세션의 큰 흐름.</summary>
public enum DraftSessionState
{
    Lobby,

    /// <summary>밴픽씬 로드 완료 ~ 실제 드래프트 시작 전, UI 로딩 유예 시간(기본 15초) 동안의 상태.</summary>
    Loading,

    InProgress,
    Completed
}
