using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 접속 화면(Title 등 별도 씬에 두는 걸 권장): "방 만들기"(호스트) / "참가하기"(클라이언트) 버튼을 제공한다.
///
/// 기존 버전과의 차이:
///  - Client가 접속할 주소/포트를 화면에서 직접 입력할 수 있다 (기존에는 Transport 인스펙터에
///    미리 박아둔 주소로만 접속 가능했음).
///  - 호스트는 방을 만들 때 간단한 비밀번호를 설정할 수 있고, 클라이언트는 참가할 때 그 비밀번호를
///    입력해야 한다. 비밀번호 확인은 RoomAccessController(서버 쪽 ConnectionApprovalCallback)가 담당하고,
///    이 스크립트는 입력값을 모아서 넘겨주는 역할만 한다.
///  - 비밀번호를 틀렸을 때 등 접속이 거절된 이유(NetworkManager.DisconnectReason)를 화면에 보여준다.
///
/// 씬 분리를 전제로 한 사용법:
///  1) 이 스크립트는 NetworkManager가 파괴되지 않고 유지되는 "접속" 씬(예: Title)에 배치한다.
///  2) 호스트가 "방 만들기"를 누르면 DraftSessionBootstrap이 대기실 씬(예: MainLobby)으로 자동 전환한다
///     (DraftSessionBootstrap.lobbySceneName 참고). 클라이언트는 접속에 성공하면 Netcode 씬 동기화로
///     같은 씬을 자동으로 따라간다.
///  3) 즉 이 컴포넌트/이 씬은 "접속 전" 상태만 신경 쓰면 되고, 대기실 화면(DraftLobbyController)과는
///     완전히 분리된다.
/// </summary>
public class NetworkConnectionUI : MonoBehaviour
{
    [Header("Room 생성 (Host)")]
    [SerializeField] private TMP_InputField hostPasswordInput; // 비워두면 비밀번호 없이 오픈
    [SerializeField] private Button createRoomButton;

    [Header("Room 참가 (Client)")]
    [SerializeField] private TMP_InputField joinAddressInput; // 예: 127.0.0.1, 192.168.0.5
    [SerializeField] private TMP_InputField joinPortInput;     // 비워두면 Transport에 설정된 기본 포트 사용
    [SerializeField] private TMP_InputField joinPasswordInput;
    [SerializeField] private Button joinRoomButton;

    [Header("Status")]
    [SerializeField] private TMP_Text statusText;

    private NetworkManager networkManager;
    private RoomAccessController roomAccess;

    private void Awake()
    {
        createRoomButton.onClick.AddListener(HandleCreateRoom);
        joinRoomButton.onClick.AddListener(HandleJoinRoom);
    }

    private void OnEnable()
    {
        networkManager = NetworkManager.Singleton;
        if (networkManager == null)
        {
            Debug.LogError($"[{nameof(NetworkConnectionUI)}] NetworkManager.Singleton이 없습니다. 씬에 배치되어 있는지 확인하세요.");
            return;
        }

        roomAccess = networkManager.GetComponent<RoomAccessController>();
        if (roomAccess == null)
        {
            Debug.LogError($"[{nameof(NetworkConnectionUI)}] RoomAccessController가 NetworkManager 오브젝트에 없습니다. " +
                            "비밀번호 기능을 쓰려면 같은 오브젝트에 추가하세요.");
        }

        networkManager.OnServerStarted += RefreshStatus;
        networkManager.OnClientConnectedCallback += HandleClientConnected;
        networkManager.OnClientDisconnectCallback += HandleClientDisconnected;
        RefreshStatus();
    }

    private void OnDisable()
    {
        if (networkManager == null) return;

        networkManager.OnServerStarted -= RefreshStatus;
        networkManager.OnClientConnectedCallback -= HandleClientConnected;
        networkManager.OnClientDisconnectCallback -= HandleClientDisconnected;
    }

    // ==================== 방 만들기 (Host) ====================

    private void HandleCreateRoom()
    {
        if (roomAccess != null)
        {
            roomAccess.HostSetPassword(hostPasswordInput != null ? hostPasswordInput.text : string.Empty);
        }

        networkManager.StartHost();
        RefreshStatus();
    }

    // ==================== 참가하기 (Client) ====================

    private void HandleJoinRoom()
    {
        if (!TryApplyJoinAddress())
        {
            return; // 주소 파싱 실패 시 접속 시도 자체를 하지 않는다.
        }

        if (roomAccess != null)
        {
            roomAccess.ClientSetJoinPassword(joinPasswordInput != null ? joinPasswordInput.text : string.Empty);
        }

        networkManager.StartClient();
        RefreshStatus();
    }

    /// <summary>
    /// 입력된 주소/포트를 UnityTransport(ConnectionData)에 반영한다.
    /// 이 프로젝트의 Transport가 UnityTransport(UTP)라는 전제 - 다른 Transport를 쓴다면
    /// 이 메서드만 해당 Transport의 접속 주소 설정 API로 교체하면 된다.
    /// </summary>
    private bool TryApplyJoinAddress()
    {
        if (networkManager.NetworkConfig.NetworkTransport is not UnityTransport utp)
        {
            Debug.LogError($"[{nameof(NetworkConnectionUI)}] UnityTransport가 아닌 Transport가 설정되어 있습니다. " +
                            "주소 입력 기능을 쓰려면 UnityTransport를 사용하세요.");
            return true; // Transport가 다르면 그냥 인스펙터 기본값으로 접속 시도하게 둔다.
        }

        string address = joinAddressInput != null && !string.IsNullOrWhiteSpace(joinAddressInput.text)
            ? joinAddressInput.text.Trim()
            : "127.0.0.1";

        ushort port = utp.ConnectionData.Port;
        if (joinPortInput != null && !string.IsNullOrWhiteSpace(joinPortInput.text))
        {
            if (!ushort.TryParse(joinPortInput.text.Trim(), out port))
            {
                SetStatus($"포트 번호가 올바르지 않습니다: {joinPortInput.text}");
                return false;
            }
        }

        utp.ConnectionData.Address = address;
        utp.ConnectionData.Port = port;
        return true;
    }

    // ==================== 상태 표시 ====================

    private void HandleClientConnected(ulong clientId) => RefreshStatus();

    private void HandleClientDisconnected(ulong clientId)
    {
        // 접속 승인이 거절된 경우(예: 비밀번호 불일치) 여기서 사유를 확인할 수 있다.
        string reason = networkManager.DisconnectReason;
        if (!string.IsNullOrEmpty(reason))
        {
            SetStatus($"접속 실패: {reason}");
            return;
        }

        RefreshStatus();
    }

    private void RefreshStatus()
    {
        if (!networkManager.IsClient && !networkManager.IsServer)
        {
            SetStatus("연결 안 됨");
            SetInteractable(true);
            return;
        }

        SetInteractable(false);

        string role = networkManager.IsHost ? "호스트" : (networkManager.IsServer ? "서버" : "클라이언트");
        SetStatus($"{role} (내 clientId={networkManager.LocalClientId}, 접속자 {networkManager.ConnectedClientsIds.Count}명)");
    }

    private void SetInteractable(bool interactable)
    {
        createRoomButton.interactable = interactable;
        joinRoomButton.interactable = interactable;
    }

    private void SetStatus(string message)
    {
        if (statusText) statusText.text = message;
    }
}
