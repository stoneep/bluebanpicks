using UnityEngine;
using Unity.Netcode;

// ==================== 대기실: 참가자 닉네임 등록/해제 (서버 전용) ====================
public partial class DraftSessionServer
{
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

        string nickname = roomAccess != null && roomAccess.TryConsumePendingNickname(clientId, out var pending)
            ? pending
            : $"Player{clientId}";

        RemoveNicknameEntry(clientId); // 재연결 등으로 중복 등록되는 것 방지
        Nicknames.Add(new ClientNicknameEntry { ClientId = clientId, Nickname = nickname });
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