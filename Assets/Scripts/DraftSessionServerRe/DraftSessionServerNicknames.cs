using UnityEngine;
using Unity.Netcode;

// ==================== 대기실: 참가자 닉네임 등록/해제 (서버 전용) ====================
public partial class DraftSessionServer
{
    // ==================== 서버 내부: 참가자 닉네임 등록/해제 ====================

    /// <summary>
    /// RoomAccessController가 ApprovalCheck 때 잠깐 보관해둔 닉네임을 꺼내와 Nicknames에 등록한다.
    /// pendingNickname이 없으면(예외적 상황) "PlayerN"으로 대체한다.
    /// </summary>
    private void HandleClientConnectedForNickname(ulong clientId)
    {
        if (!IsServer) return;

        var roomAccess = NetworkManager.Singleton != null
            ? NetworkManager.Singleton.GetComponent<RoomAccessController>()
            : null;

        if (roomAccess != null && roomAccess.TryConsumePendingNickname(clientId, out var pending))
        {
            RemoveNicknameEntry(clientId); // 재연결 등으로 중복 등록되는 것 방지
            Nicknames.Add(new ClientNicknameEntry { ClientId = clientId, Nickname = pending });
            return;
        }

        // pending이 없다는 건 "이미 이 clientId에 대해 한 번 등록을 마쳤다"는 뜻일 수 있다
        // (초기 스캔 + OnClientConnectedCallback 중복 호출 등). 이미 등록돼 있으면 손대지 않는다.
        if (HasNicknameEntry(clientId)) return;

        // 정말로 pending 닉네임도 없고 기존 등록도 없는 예외적 상황에서만 기본값 사용.
        Nicknames.Add(new ClientNicknameEntry { ClientId = clientId, Nickname = $"Player{clientId}" });
    }

    private bool HasNicknameEntry(ulong clientId)
    {
        foreach (var entry in Nicknames)
            if (entry.ClientId == clientId) return true;
        return false;
    }
    
    private void HandleClientDisconnectedForNickname(ulong clientId)
    {
        if (!IsServer) return;
        RemoveNicknameEntry(clientId);
    }

    private void RemoveNicknameEntry(ulong clientId)
    {
        for (int i = Nicknames.Count - 1; i >= 0; i--)
        {
            if (Nicknames[i].ClientId == clientId)
            {
                Nicknames.RemoveAt(i);
                return;
            }
        }
    }

    /// <summary>주어진 clientId의 현재 닉네임. 아직 등록 전이면 "PlayerN"으로 대체.</summary>
    public string GetNickname(ulong clientId)
    {
        foreach (var entry in Nicknames)
            if (entry.ClientId == clientId) return entry.Nickname.ToString();
        return $"Player{clientId}";
    }
}