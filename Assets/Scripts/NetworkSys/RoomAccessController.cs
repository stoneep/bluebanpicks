using System.Text;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkManager))]
public class RoomAccessController : MonoBehaviour
{
    private string roomPassword = string.Empty;

    private NetworkManager networkManager;

    private void Awake()
    {
        networkManager = GetComponent<NetworkManager>();
        
        networkManager.NetworkConfig.ConnectionApproval = true;
        networkManager.ConnectionApprovalCallback = ApprovalCheck;
    }
    
    public void HostSetPassword(string password)
    {
        roomPassword = string.IsNullOrEmpty(password) ? string.Empty : password.Trim();

        Debug.Log(string.IsNullOrEmpty(roomPassword)
            ? $"[{nameof(RoomAccessController)}] 비밀번호 없이 방을 엽니다."
            : $"[{nameof(RoomAccessController)}] 비밀번호가 설정된 방을 엽니다.");
    }
    
    public void ClientSetJoinPassword(string password)
    {
        string trimmed = string.IsNullOrEmpty(password) ? string.Empty : password.Trim();
        networkManager.NetworkConfig.ConnectionData = Encoding.UTF8.GetBytes(trimmed);
    }
    
    private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request,
                                NetworkManager.ConnectionApprovalResponse response)
    {
        string incoming = request.Payload is { Length: > 0 }
            ? Encoding.UTF8.GetString(request.Payload)
            : string.Empty;

        if (!string.IsNullOrEmpty(roomPassword) && incoming != roomPassword)
        {
            Debug.LogWarning($"[{nameof(RoomAccessController)}] clientId={request.ClientNetworkId} " +
                              "비밀번호 불일치로 접속을 거절합니다.");
            response.Approved = false;
            response.Reason = "비밀번호가 올바르지 않습니다.";
            response.CreatePlayerObject = false;
            return;
        }
        
        response.CreatePlayerObject = false;
        response.Approved = true;
    }
}
