using UnityEngine;
using Unity.Netcode;

// ==================== 대기실: 포맷/진영/타이머 편집 (호스트 전용) ====================
public partial class DraftSessionServer
{
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

    /// <summary>
    /// 대기실 참가자 목록 UI에서 특정 클라이언트 한 명의 역할(관전자/선공/후공)을 명시적으로 바꾼다.
    /// 기존 HostAssignSides가 "선공+후공 두 명을 한 번에" 지정하는 API였다면, 이건 "한 명만" 바꿀 때 쓴다 -
    /// 예를 들어 이미 참가 중인 사람을 관전자로 내리거나(role=null), 관전자를 특정 진영에 새로 앉힐 때.
    ///
    /// 역할은 FirstSideClientId/SecondSideClientId 두 값만으로 파생되고 별도의 "관전자 목록"이 없으므로,
    /// 어떤 클라이언트를 새로 선공/후공에 앉히면 그 자리에 있던 기존 클라이언트는 자동으로
    /// (변수값이 더 이상 자기 id와 일치하지 않게 되어) 관전자로 밀려난다.
    /// </summary>
    public void HostSetParticipantRole(ulong clientId, DraftSide? role)
    {
        if (!IsServer)
        {
            Debug.LogWarning($"[{nameof(DraftSessionServer)}] HostSetParticipantRole은 서버(호스트)에서만 호출할 수 있습니다.");
            return;
        }
        if (State.Value != DraftSessionState.Lobby)
        {
            Debug.LogWarning($"[{nameof(DraftSessionServer)}] 드래프트 시작 후에는 참가자 역할을 바꿀 수 없습니다.");
            return;
        }
        if (role != null && !HostCanPlay.Value && clientId == NetworkManager.ServerClientId)
        {
            // HostAssignSides와 동일한 방어: "2인 연습 모드"가 아니면 호스트는 항상 관전자.
            Debug.LogError($"[{nameof(DraftSessionServer)}] 호스트(clientId={NetworkManager.ServerClientId})는 관전자이므로 " +
                            "참가자로 배정할 수 없습니다. (HostCanPlay를 켜면 호스트도 참가 가능)");
            return;
        }

        switch (role)
        {
            case null: // 관전자로 내림
                if (FirstSideClientId.Value == clientId) FirstSideClientId.Value = ulong.MaxValue;
                if (SecondSideClientId.Value == clientId) SecondSideClientId.Value = ulong.MaxValue;
                break;

            case DraftSide.First:
                if (SecondSideClientId.Value == clientId) SecondSideClientId.Value = ulong.MaxValue; // 같은 사람이 양쪽에 겹치지 않도록
                FirstSideClientId.Value = clientId; // 기존 선공이 있었다면 값이 바뀌는 순간 자동으로 관전자 취급됨
                break;

            case DraftSide.Second:
                if (FirstSideClientId.Value == clientId) FirstSideClientId.Value = ulong.MaxValue;
                SecondSideClientId.Value = clientId;
                break;
        }
    }

    /// <summary>
    /// 대기실에서 preDraft 로딩 유예시간 / 턴 제한시간 / 종료 후 안내 카운트다운 시간을
    /// 세션 공통값으로 설정한다. 라운드별로 다르지 않고 세션 전체에 하나만 존재하는 값이므로,
    /// 어느 라운드 행(UI)에서 값을 바꾸든 이 메서드를 거쳐 전체(NetworkVariable)에 반영되고
    /// 모든 클라이언트 화면에 동기화된다.
    /// Lobby 상태에서만 변경 가능(진행 중에 바뀌면 이미 시작된 카운트다운과 어긋날 수 있으므로).
    /// </summary>
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
}
