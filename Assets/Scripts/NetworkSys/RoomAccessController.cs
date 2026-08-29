using System.Text;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 방 생성 시 호스트가 정한 "간단한 비밀번호"를 서버 쪽에서 검증하는 접속 승인(ConnectionApproval) 처리기.
/// NetworkManager와 같은 GameObject에 붙인다 (DraftSessionBootstrap과 동일 오브젝트 권장).
///
/// 동작 방식 (Netcode for GameObjects 표준 패턴):
///  - 서버: NetworkConfig.ConnectionApproval = true 로 켜두면, 클라이언트가 붙을 때마다
///    ConnectionApprovalCallback이 호출된다. 이때 클라이언트가 보낸 Payload(byte[])를
///    비밀번호 문자열로 해석해서 호스트가 설정한 비밀번호와 비교한다.
///  - 클라이언트: StartClient()를 호출하기 "전"에 NetworkConfig.ConnectionData에
///    비밀번호를 바이트로 넣어두면, 그 값이 그대로 서버의 Payload로 전달된다.
///    (정식 커넥션이 열리기 전에 오가는 값이라 순수 텍스트 비교로도 로컬/사설 테스트용으로는 충분하지만,
///     공인 서버로 운영할 경우 Relay/DTLS 등 전송 자체의 암호화에 의존해야 한다 - 이 클래스는
///     "아무나 못 들어오게" 막는 수준의 간단한 잠금이지, 보안 등급의 인증이 아니다.)
///
/// 비밀번호를 비워두면(빈 문자열) 기존처럼 "누구나 접속 가능" 상태로 동작한다 - 하위 호환.
/// </summary>
[RequireComponent(typeof(NetworkManager))]
public class RoomAccessController : MonoBehaviour
{
    /// <summary>
    /// 서버(호스트)만 아는 현재 방 비밀번호. 네트워크로 전송/노출되지 않는다.
    /// 빈 문자열이면 비밀번호 없이 누구나 입장 가능.
    /// </summary>
    private string roomPassword = string.Empty;

    private NetworkManager networkManager;

    private void Awake()
    {
        networkManager = GetComponent<NetworkManager>();

        // ConnectionApproval을 켜지 않으면 ConnectionApprovalCallback 자체가 호출되지 않고
        // 전원 자동 승인되므로, 반드시 StartHost/StartClient 호출 전에 켜져 있어야 한다.
        networkManager.NetworkConfig.ConnectionApproval = true;
        networkManager.ConnectionApprovalCallback = ApprovalCheck;
    }

    /// <summary>
    /// 호스트가 "방 만들기"를 누르는 시점에 호출. StartHost()보다 먼저 호출해야 한다.
    /// </summary>
    public void HostSetPassword(string password)
    {
        roomPassword = string.IsNullOrEmpty(password) ? string.Empty : password.Trim();

        Debug.Log(string.IsNullOrEmpty(roomPassword)
            ? $"[{nameof(RoomAccessController)}] 비밀번호 없이 방을 엽니다."
            : $"[{nameof(RoomAccessController)}] 비밀번호가 설정된 방을 엽니다.");
    }

    /// <summary>
    /// 클라이언트가 "참가하기"를 누르기 전에 호출해서 접속 페이로드(비밀번호)를 준비한다.
    /// StartClient()보다 먼저 호출해야 실제 접속 요청에 실려 나간다.
    /// </summary>
    public void ClientSetJoinPassword(string password)
    {
        string trimmed = string.IsNullOrEmpty(password) ? string.Empty : password.Trim();
        networkManager.NetworkConfig.ConnectionData = Encoding.UTF8.GetBytes(trimmed);
    }

    /// <summary>
    /// 서버(호스트)에서만 호출된다. 여기서 거절하면 클라이언트 쪽에서는
    /// NetworkManager.DisconnectReason에 response.Reason 값이 그대로 전달된다.
    /// </summary>
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

        // 이 프로젝트는 클라이언트마다 별도의 Player NetworkObject를 스폰하지 않는다
        // (드래프트 세션은 DraftSessionServer 단일 오브젝트가 전체 상태를 관리).
        response.CreatePlayerObject = false;
        response.Approved = true;
    }
}
