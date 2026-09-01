using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 방 생성 시 호스트가 정한 "간단한 비밀번호"를 서버 쪽에서 검증하는 접속 승인(ConnectionApproval) 처리기.
/// 더불어, 같은 접속 승인 페이로드에 "닉네임"도 함께 실어 보내서 별도의 네트워크 왕복(RPC 등) 없이
/// 접속 시점에 딱 한 번만 닉네임을 서버에 전달한다.
/// NetworkManager와 같은 GameObject에 붙인다 (DraftSessionBootstrap과 동일 오브젝트 권장).
///
/// 페이로드 형식: "{password}\u0001{nickname}" 을 UTF8 바이트로 인코딩.
/// \u0001(구분자)은 일반 텍스트 입력에 나올 일이 거의 없는 제어 문자라 구분자로 사용했고,
/// 혹시라도 섞여 들어오면 SanitizeForPayload에서 제거한다.
///
/// 동작 방식 (Netcode for GameObjects 표준 패턴):
///  - 서버: NetworkConfig.ConnectionApproval = true 로 켜두면, 클라이언트가 붙을 때마다
///    ConnectionApprovalCallback이 호출된다. 이때 클라이언트가 보낸 Payload(byte[])를
///    비밀번호+닉네임 문자열로 해석한다.
///  - 클라이언트: StartClient()를 호출하기 "전"에 NetworkConfig.ConnectionData에
///    값을 바이트로 넣어두면, 그 값이 그대로 서버의 Payload로 전달된다.
///
/// 비밀번호를 비워두면(빈 문자열) 기존처럼 "누구나 접속 가능" 상태로 동작한다 - 하위 호환.
/// </summary>
[RequireComponent(typeof(NetworkManager))]
public class RoomAccessController : MonoBehaviour
{
    private const char PayloadSeparator = '\u0001';
    private const int MaxNicknameLength = 16;

    /// <summary>
    /// 서버(호스트)만 아는 현재 방 비밀번호. 네트워크로 전송/노출되지 않는다.
    /// 빈 문자열이면 비밀번호 없이 누구나 입장 가능.
    /// </summary>
    private string roomPassword = string.Empty;

    /// <summary>
    /// 서버 전용: ApprovalCheck 시점에 함께 받은 닉네임을 잠깐 보관해두는 저장소.
    /// ConnectionApprovalCallback은 아직 "정식 연결"이 성립하기 전에 호출되므로, 여기서 바로
    /// NetworkList에 반영하지 않고 DraftSessionServer가 클라이언트 연결이 확정된 뒤
    /// TryConsumePendingNickname()으로 꺼내가게 한다. 꺼내가면 즉시 제거된다.
    /// </summary>
    private readonly Dictionary<ulong, string> pendingNicknames = new();

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
    /// 클라이언트(호스트 자기 자신 포함)가 접속하기 전에 호출해서, 비밀번호와 닉네임을
    /// 하나의 접속 페이로드로 묶어 준비한다. StartClient()/StartHost()보다 먼저 호출해야
    /// 실제 접속 요청에 실려 나간다.
    ///
    /// 이미 존재하는 접속 승인 왕복 한 번에 닉네임을 얹어 보내는 것이므로,
    /// 이 기능을 위해 별도의 네트워크 트래픽(RPC 등)이 추가로 발생하지 않는다.
    /// </summary>
    public void ClientSetConnectionPayload(string password, string nickname)
    {
        string trimmedPassword = string.IsNullOrEmpty(password) ? string.Empty : password.Trim();
        string safeNickname = SanitizeForPayload(nickname);

        string combined = trimmedPassword + PayloadSeparator + safeNickname;
        networkManager.NetworkConfig.ConnectionData = Encoding.UTF8.GetBytes(combined);
    }

    private static string SanitizeForPayload(string nickname)
    {
        string trimmed = string.IsNullOrWhiteSpace(nickname) ? "Player" : nickname.Trim();
        // 구분자 문자가 우연히 섞여 들어오면 파싱이 깨지므로 제거해둔다.
        trimmed = trimmed.Replace(PayloadSeparator.ToString(), string.Empty);
        return trimmed.Length > MaxNicknameLength ? trimmed.Substring(0, MaxNicknameLength) : trimmed;
    }

    /// <summary>
    /// 서버(호스트)에서만 호출된다. 여기서 거절하면 클라이언트 쪽에서는
    /// NetworkManager.DisconnectReason에 response.Reason 값이 그대로 전달된다.
    /// </summary>
    private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request,
                                NetworkManager.ConnectionApprovalResponse response)
    {
        string payload = request.Payload is { Length: > 0 }
            ? Encoding.UTF8.GetString(request.Payload)
            : string.Empty;

        int sepIndex = payload.IndexOf(PayloadSeparator);
        string incomingPassword = sepIndex >= 0 ? payload.Substring(0, sepIndex) : payload;
        string incomingNickname = sepIndex >= 0 ? payload.Substring(sepIndex + 1) : string.Empty;

        if (!string.IsNullOrEmpty(roomPassword) && incomingPassword != roomPassword)
        {
            Debug.LogWarning($"[{nameof(RoomAccessController)}] clientId={request.ClientNetworkId} " +
                              "비밀번호 불일치로 접속을 거절합니다.");
            response.Approved = false;
            response.Reason = "비밀번호가 올바르지 않습니다.";
            response.CreatePlayerObject = false;
            return;
        }

        pendingNicknames[request.ClientNetworkId] =
            string.IsNullOrEmpty(incomingNickname) ? $"Player{request.ClientNetworkId}" : incomingNickname;

        // 이 프로젝트는 클라이언트마다 별도의 Player NetworkObject를 스폰하지 않는다
        // (드래프트 세션은 DraftSessionServer 단일 오브젝트가 전체 상태를 관리).
        response.CreatePlayerObject = false;
        response.Approved = true;
    }

    /// <summary>
    /// DraftSessionServer가 (해당 clientId의 접속이 확정된 시점에) 한 번만 호출해서 닉네임을 꺼내간다.
    /// 꺼내간 항목은 즉시 제거되므로, 재연결 시에는 다시 ApprovalCheck에서 채워진 새 값을 받게 된다.
    /// </summary>
    public bool TryConsumePendingNickname(ulong clientId, out string nickname)
    {
        if (pendingNicknames.TryGetValue(clientId, out nickname))
        {
            pendingNicknames.Remove(clientId);
            return true;
        }
        nickname = null;
        return false;
    }
}
