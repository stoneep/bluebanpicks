using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// ==================== 진행 중: 액션 제출 / 일시정지 / 턴 제한시간 (모든 클라이언트가 호출) ====================
public partial class DraftSessionServer
{
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

    // 클릭 폭주(매크로 등)로부터 서버를 보호하기 위한 최소 요청 간격
    private readonly Dictionary<ulong, float> lastPreviewRequestTime = new();
    private const float MinPreviewIntervalSeconds = 0.1f; // 클라이언트당 최대 초당 10회

    [ServerRpc(RequireOwnership = false)]
    public void UpdatePendingPreviewServerRpc(string characterId, ServerRpcParams rpcParams = default)
    {
        var senderClientId = rpcParams.Receive.SenderClientId;

        // 실패해도 사용자에게 알릴 필요 없는 "가벼운" 요청이므로 조용히 무시한다.
        if (State.Value != DraftSessionState.InProgress || IsPaused.Value) return;
        if (!TryResolveSide(senderClientId, out var side)) return;
        if (side != CurrentSide.Value) return; // 지금 자기 턴이 아니면 무시

        // 초당 요청 횟수 제한 (치팅/매크로/버그로 인한 RPC 스팸 방지)
        float now = Time.realtimeSinceStartup;
        if (lastPreviewRequestTime.TryGetValue(senderClientId, out var last) &&
            now - last < MinPreviewIntervalSeconds)
        {
            return;
        }
        lastPreviewRequestTime[senderClientId] = now;

        // 이미 밴/픽된 캐릭터를 프리뷰로 세팅하려는 요청도 막는다.
        if (!string.IsNullOrEmpty(characterId) &&
            (ruleManager == null || !ruleManager.IsCharacterAvailable(characterId)))
        {
            return;
        }

        PendingPreviewCharacterId.Value = characterId ?? string.Empty;
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

    // ==================== 서버 내부: 밴/픽 턴 제한 시간 ====================

    private void RestartTurnTimer()
    {
        if (TurnTimeLimitSeconds.Value <= 0f)
        {
            turnCountdown.Stop();
            return;
        }

        turnCountdown.Begin(TurnTimeLimitSeconds.Value, HandleTurnTimedOut);
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
}
