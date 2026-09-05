using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

// ==================== 대기실 -> 드래프트 시작 -> 진행 -> 종료 흐름 (서버 전용) ====================
public partial class DraftSessionServer
{
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
    ///
    /// duration이 0 이하여도 NetworkCountdown.Begin은 그대로 코루틴을 시작해 곧바로 BeginDraft를
    /// 호출한다 - "유예 시간 0초 = 즉시 시작"이 기존 의도된 동작이므로 별도 분기를 두지 않는다.
    /// </summary>
    private void BeginPreDraftCountdown()
    {
        State.Value = DraftSessionState.Loading;
        IsPaused.Value = false; // Loading 진입 시에도 이전 상태가 남아있지 않도록 확실히 초기화
        Debug.Log($"[{nameof(DraftSessionServer)}] State.Value set to Loading, " +
                  $"{PreDraftLoadBufferSeconds.Value}초 후 자동으로 드래프트를 시작합니다.");

        preDraftCountdown.Begin(PreDraftLoadBufferSeconds.Value, BeginDraft);
    }

    private void BeginDraft()
    {
        var formatData = Format.ToDraftFormatData();

        ruleManager = new RuleManager(formatData);
        ruleManager.OnActionSubmitted += HandleServerActionSubmitted;
        ruleManager.OnPhaseChanged += HandleServerPhaseChanged;
        ruleManager.OnDraftCompleted += HandleServerDraftCompleted;

        ActionLog.Clear();
        PendingPreviewCharacterId.Value = string.Empty; // 추가
        IsPaused.Value = false; // 혹시 남아있을 수 있는 이전 상태를 새 드래프트 시작 시 확실히 초기화
        State.Value = DraftSessionState.InProgress;
        Debug.Log($"[{nameof(DraftSessionServer)}] State.Value set to InProgress " +
                  $"(session={GetEntityId()}, IsSpawned={NetworkObject.IsSpawned}, " +
                  $"scene={gameObject.scene.name}) @ frame {Time.frameCount}");
        ruleManager.StartDraft(); // 내부에서 OnPhaseChanged가 발행되어 첫 턴 타이머도 자동으로 시작된다.
    }

    // ==================== 서버 내부: RuleManager 이벤트 -> 동기화 데이터 반영 ====================

    private void HandleServerActionSubmitted(DraftSide side, string characterId, DraftResultType type)
    {
        PendingPreviewCharacterId.Value = string.Empty; // 추가 - 확정되었으니 프리뷰는 무의미해짐
        
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
        PendingPreviewCharacterId.Value = string.Empty; // 추가 (안전망 - 위에서 이미 비워지지만 이중 방지)
        CurrentPhaseName.Value = phase.PhaseName;
        CurrentSide.Value = phase.CurrentSide;
        RestartTurnTimer();
    }

    private void HandleServerDraftCompleted()
    {
        PendingPreviewCharacterId.Value = string.Empty; // 추가
        State.Value = DraftSessionState.Completed;
        turnCountdown.Stop();
        BeginPostDraftCountdown();
    }

    // ==================== 서버 내부: 종료 후 안내 카운트다운 ====================

    /// <summary>
    /// PostDraftDisplaySeconds가 0보다 크면 PostDraftSecondsRemaining을 그 값에서 0까지
    /// 서버 권위로 카운트다운한다(PreDraftCountdownRoutine과 동일한 패턴). 0 이하로 설정되어 있으면
    /// 카운트다운을 시작하지 않고 0으로 둔다 - 이 경우 PostDraftTimerIndicator가 로컬 경과 시간
    /// 표시로 대체한다.
    /// </summary>
    private void BeginPostDraftCountdown()
    {
        if (PostDraftDisplaySeconds.Value <= 0f)
        {
            postDraftCountdown.Stop();
            return;
        }

        postDraftCountdown.Begin(PostDraftDisplaySeconds.Value, onComplete: null);
    }
}
