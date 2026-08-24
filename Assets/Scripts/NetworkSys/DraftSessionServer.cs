using System;
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

    public readonly NetworkVariable<DraftSessionState> State = new(DraftSessionState.Lobby);

    [Header("Scene Transition")]
    [Tooltip("드래프트 시작 시 전환할 씬 이름. Build Settings(File > Build Settings > Scenes In Build)에 " +
             "먼저 등록되어 있어야 하고, NetworkManager 인스펙터에서 Enable Scene Management가 켜져 있어야 한다.")]
    [SerializeField] private string draftSceneName = "MainLobby";

    // ---------- 진행 중 상태 (서버가 갱신, 클라는 읽기만) ----------

    public readonly NetworkVariable<FixedString32Bytes> CurrentPhaseName = new();
    public readonly NetworkVariable<DraftSide> CurrentSide = new();
    public readonly NetworkList<NetworkDraftAction> ActionLog = new();

    /// <summary>액션 거부 사유. 요청을 보낸 클라이언트에게만 전달된다 (전체 브로드캐스트 아님).</summary>
    public event Action<string> OnActionRejected;

    // 서버 전용 - 클라이언트에는 절대 존재/노출되지 않음
    private RuleManager ruleManager;

    public override void OnNetworkSpawn()
    {
        Instance = this;
        OnSessionReady?.Invoke(this);
    }

    public override void OnNetworkDespawn()
    {
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

        FirstSideClientId.Value = firstClientId;
        SecondSideClientId.Value = secondClientId;
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
        State.Value = DraftSessionState.InProgress;
        ruleManager.StartDraft();
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

    // ==================== 서버 내부: RuleManager 이벤트 -> 동기화 데이터 반영 ====================

    private void HandleServerActionSubmitted(DraftSide side, string characterId, DraftResultType type)
    {
        ActionLog.Add(new NetworkDraftAction
        {
            side = side,
            characterId = characterId,
            resultType = type
        });
    }

    private void HandleServerPhaseChanged(IDraftPhase phase)
    {
        CurrentPhaseName.Value = phase.PhaseName;
        CurrentSide.Value = phase.CurrentSide;
    }

    private void HandleServerDraftCompleted()
    {
        State.Value = DraftSessionState.Completed;
    }

    public override void OnDestroy()
    {
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

/// <summary>대기실(설정 편집) → 진행 중 → 종료, 세션의 큰 흐름.</summary>
public enum DraftSessionState
{
    Lobby,
    InProgress,
    Completed
}
