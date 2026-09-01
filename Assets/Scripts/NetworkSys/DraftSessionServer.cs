using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DraftSessionServer : NetworkBehaviour
{
    public static DraftSessionServer Instance { get; private set; }
    
    public static event Action<DraftSessionServer> OnSessionReady;
    
    
    public readonly NetworkList<NetworkDraftRoundConfig> Format = new();
    
    public readonly NetworkVariable<ulong> FirstSideClientId = new(ulong.MaxValue);
    public readonly NetworkVariable<ulong> SecondSideClientId = new(ulong.MaxValue);
    
    public readonly NetworkVariable<bool> HostCanPlay = new(false);

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
    
    public readonly NetworkVariable<float> PreDraftLoadBufferSeconds = new(15f);
    
    public readonly NetworkVariable<float> TurnTimeLimitSeconds = new(30f);
    
    public readonly NetworkVariable<float> PostDraftDisplaySeconds = new(10f);
    
    public readonly NetworkVariable<float> PreDraftSecondsRemaining = new(0f);
    
    public readonly NetworkVariable<float> TurnSecondsRemaining = new(0f);
    
    public readonly NetworkVariable<float> PostDraftSecondsRemaining = new(0f);
    
    public readonly NetworkVariable<bool> IsPaused = new(false);

    private Coroutine preDraftCountdownRoutine;
    private Coroutine turnTimerRoutine;
    private Coroutine postDraftCountdownRoutine;
    

    public readonly NetworkVariable<FixedString32Bytes> CurrentPhaseName = new();
    public readonly NetworkVariable<DraftSide> CurrentSide = new();
    public readonly NetworkList<NetworkDraftAction> ActionLog = new();
    
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
    
    public event Action<string> OnActionRejected;
    
    private RuleManager ruleManager;

    public override void OnNetworkSpawn()
    {
        Instance = this;

        if (IsServer)
        {
            PreDraftLoadBufferSeconds.Value = defaultPreDraftLoadBufferSeconds;
            TurnTimeLimitSeconds.Value = defaultTurnTimeLimitSeconds;
            PostDraftDisplaySeconds.Value = defaultPostDraftDisplaySeconds;
        }

        Debug.Log($"[{nameof(DraftSessionServer)}] OnNetworkSpawn (session={GetEntityId()}, " +
                  $"scene={gameObject.scene.name}) @ frame {Time.frameCount}");
        RaiseSessionReadySafely();
    }
    
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
    }
    

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
            Debug.LogError($"[{nameof(DraftSessionServer)}] 호스트(clientId={NetworkManager.ServerClientId})는 관전자이므로 " +
                            "선공/후공에 배정할 수 없습니다. (HostCanPlay를 켜면 호스트도 참가 가능)");
            return;
        }

        FirstSideClientId.Value = firstClientId;
        SecondSideClientId.Value = secondClientId;
    }
    
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
    
    public void HostSetTimerSettings(float preDraftBufferSeconds, float turnTimeLimitSecondsValue, float postDraftDisplaySecondsValue)
    {
        if (!IsServer)
        {
            Debug.LogWarning($"[{nameof(DraftSessionServer)}] HostSetTimerSettings는 서버(호스트)에서만 호출할 수 있습니다.");
            return;
        }
        if (State.Value != DraftSessionState.Lobby)
        {
            Debug.LogWarning($"[{nameof(DraftSessionServer)}] 드래프트 시작 후에는 타이머 설정을 바꿀 수 없습니다.");
            return;
        }

        PreDraftLoadBufferSeconds.Value = Mathf.Max(0f, preDraftBufferSeconds);
        TurnTimeLimitSeconds.Value = Mathf.Max(0f, turnTimeLimitSecondsValue);
        PostDraftDisplaySeconds.Value = Mathf.Max(0f, postDraftDisplaySecondsValue);
    }
    

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
    
    private void BeginPreDraftCountdown()
    {
        State.Value = DraftSessionState.Loading;
        IsPaused.Value = false;
        Debug.Log($"[{nameof(DraftSessionServer)}] State.Value set to Loading, " +
                  $"{PreDraftLoadBufferSeconds.Value}초 후 자동으로 드래프트를 시작합니다.");

        if (preDraftCountdownRoutine != null) StopCoroutine(preDraftCountdownRoutine);
        preDraftCountdownRoutine = StartCoroutine(PreDraftCountdownRoutine());
    }

    private IEnumerator PreDraftCountdownRoutine()
    {
        float remaining = Mathf.Max(0f, PreDraftLoadBufferSeconds.Value);
        PreDraftSecondsRemaining.Value = Mathf.Ceil(remaining);

        while (remaining > 0f)
        {
            yield return null;
            
            if (IsPaused.Value)
            {
                Debug.Log($"[PreDraftTimer] paused, skip. remaining={remaining}");
                continue;
            }

            remaining -= Time.deltaTime;
            
            float rounded = Mathf.Max(0f, Mathf.Ceil(remaining));
            if (!Mathf.Approximately(rounded, PreDraftSecondsRemaining.Value))
            {
                PreDraftSecondsRemaining.Value = rounded;
                Debug.Log($"[PreDraftTimer] tick -> {rounded}");
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
        IsPaused.Value = false;
        State.Value = DraftSessionState.InProgress;
        Debug.Log($"[{nameof(DraftSessionServer)}] State.Value set to InProgress " +
                  $"(session={GetEntityId()}, IsSpawned={NetworkObject.IsSpawned}, " +
                  $"scene={gameObject.scene.name}) @ frame {Time.frameCount}");
        ruleManager.StartDraft();
    }
    

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

        if (IsPaused.Value == pause) return;

        IsPaused.Value = pause;
        Debug.Log($"[{nameof(DraftSessionServer)}] IsPaused set to {pause} (요청자 clientId={senderClientId}) " +
                  $"@ frame {Time.frameCount}");
    }
    

    private void HandleServerActionSubmitted(DraftSide side, string characterId, DraftResultType type)
    {
        ActionLog.Add(new NetworkDraftAction
        {
            side = side,
            characterId = characterId,
            resultType = type
        });
        
        if (ruleManager != null && ruleManager.CurrentPhase != null && !ruleManager.CurrentPhase.IsComplete)
        {
            CurrentSide.Value = ruleManager.CurrentPhase.CurrentSide;
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
        BeginPostDraftCountdown();
    }
    
    
    private void BeginPostDraftCountdown()
    {
        if (postDraftCountdownRoutine != null) StopCoroutine(postDraftCountdownRoutine);

        if (PostDraftDisplaySeconds.Value <= 0f)
        {
            PostDraftSecondsRemaining.Value = 0f;
            postDraftCountdownRoutine = null;
            return;
        }

        postDraftCountdownRoutine = StartCoroutine(PostDraftCountdownRoutine());
    }

    private IEnumerator PostDraftCountdownRoutine()
    {
        float remaining = PostDraftDisplaySeconds.Value;
        PostDraftSecondsRemaining.Value = Mathf.Ceil(remaining);

        while (remaining > 0f)
        {
            yield return null;

            remaining -= Time.deltaTime;

            float rounded = Mathf.Max(0f, Mathf.Ceil(remaining));
            if (!Mathf.Approximately(rounded, PostDraftSecondsRemaining.Value))
                PostDraftSecondsRemaining.Value = rounded;
        }

        PostDraftSecondsRemaining.Value = 0f;
        postDraftCountdownRoutine = null;
    }
    

    private void RestartTurnTimer()
    {
        if (turnTimerRoutine != null) StopCoroutine(turnTimerRoutine);

        if (TurnTimeLimitSeconds.Value <= 0f)
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
        float remaining = TurnTimeLimitSeconds.Value;
        TurnSecondsRemaining.Value = Mathf.Ceil(remaining);

        while (remaining > 0f)
        {
            yield return null;
            
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
        if (postDraftCountdownRoutine != null) StopCoroutine(postDraftCountdownRoutine);

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

public enum DraftSessionState
{
    Lobby,
    
    Loading,

    InProgress,
    Completed
}
