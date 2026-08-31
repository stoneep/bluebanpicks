using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 접속 화면(Title 등 별도 씬에 두는 걸 권장): "방 만들기"(호스트) / "참가하기"(클라이언트) 버튼을 제공한다.
///
/// 완전히 분리된 네트워크(서로 다른 인터넷 회선)에 있는 플레이어끼리 붙어야 하는 전제라,
/// IP 직접 입력 대신 Unity Relay를 사용한다:
///  - 호스트: "방 만들기" → RelayRoomService가 Relay 할당을 만들고 "방 코드"를 돌려줌 → 화면에 표시.
///  - 클라이언트: 그 방 코드를 입력하고 "참가하기".
///  - 비밀번호는 Relay와 별개로, RoomAccessController(ConnectionApprovalCallback)가 그대로 검증한다.
///
/// 씬 분리를 전제로 한 사용법은 기존과 동일:
///  1) 이 스크립트, NetworkManager, DraftSessionBootstrap, RoomAccessController, RelayRoomService는
///     모두 "접속" 씬(예: Title)에 둔다.
///  2) 호스트가 방을 만들면 DraftSessionBootstrap이 대기실 씬(예: MainLobby)으로 자동 전환한다.
///  3) 클라이언트는 접속 성공 시 Netcode 씬 동기화로 같은 씬을 자동으로 따라간다.
/// </summary>
public class NetworkConnectionUI : MonoBehaviour
{
    [Header("Room 생성 (Host)")]
    [SerializeField] private TMP_InputField hostPasswordInput; // 비워두면 비밀번호 없이 오픈
    [SerializeField] private Button createRoomButton;
    [SerializeField] private TMP_Text roomCodeDisplayText;     // 생성된 방 코드를 보여주는 텍스트 (참가자에게 공유용)
    [SerializeField] private int maxConnections = 8;           // 호스트 본인을 제외한 최대 접속자 수

    [Header("Room 참가 (Client)")]
    [SerializeField] private TMP_InputField joinCodeInput;     // 호스트에게 전달받은 방 코드
    [SerializeField] private TMP_InputField joinPasswordInput;
    [SerializeField] private Button joinRoomButton;

    [Header("Status")]
    [SerializeField] private TMP_Text statusText;

    private NetworkManager networkManager;
    private RoomAccessController roomAccess;
    private RelayRoomService relayService;

    private void Awake()
    {
        createRoomButton.onClick.AddListener(HandleCreateRoom);
        joinRoomButton.onClick.AddListener(HandleJoinRoom);
    }

    private bool subscribed;
    private Coroutine waitForSingletonRoutine;

    // NetworkManager.Awake()가 이 스크립트의 Start()보다 늦게 도는 경우가 실제로 존재한다
    // (DontDestroyOnLoad로 살아남은 오브젝트의 파괴/재생성 타이밍, Domain Reload 비활성화 설정 등).
    // 그래서 "한 번만 찾고 실패하면 끝"이 아니라, 나타날 때까지 짧게 재시도한다.
    private const float SingletonPollInterval = 0.1f;
    private const float SingletonPollTimeout = 5f;

    private void Start()
    {
        TryBindNetworkManager();
    }

    private void OnEnable()
    {
        // 이미 한 번 바인딩된 상태에서 다시 켜졌을 수 있으니(예: 씬 재진입) 재시도.
        if (!subscribed) TryBindNetworkManager();
    }

    private void TryBindNetworkManager()
    {
        if (subscribed) return;

        networkManager = NetworkManager.Singleton;
        if (networkManager == null)
        {
            // 곧바로 에러를 찍지 않고, 잠깐 폴링하며 기다린다. 그래도 안 나타나면 그때 에러 처리.
            if (waitForSingletonRoutine == null)
            {
                waitForSingletonRoutine = StartCoroutine(WaitForSingletonThenBind());
            }
            return;
        }

        BindTo(networkManager);
    }

    private IEnumerator WaitForSingletonThenBind()
    {
        float elapsed = 0f;
        while (NetworkManager.Singleton == null)
        {
            if (elapsed >= SingletonPollTimeout)
            {
                Debug.LogError($"[{nameof(NetworkConnectionUI)}] {SingletonPollTimeout}초 동안 NetworkManager.Singleton이 없습니다. " +
                                "씬에 배치되어 있는지, Build Settings에 해당 씬이 포함되어 있는지, " +
                                "NetworkManager 오브젝트가 활성화 상태인지 확인하세요.");
                waitForSingletonRoutine = null;
                yield break;
            }

            yield return new WaitForSeconds(SingletonPollInterval);
            elapsed += SingletonPollInterval;
        }

        waitForSingletonRoutine = null;
        BindTo(NetworkManager.Singleton);
    }

    private void BindTo(NetworkManager manager)
    {
        if (subscribed) return;

        networkManager = manager;
        roomAccess = networkManager.GetComponent<RoomAccessController>();
        relayService = networkManager.GetComponent<RelayRoomService>();

        if (roomAccess == null)
        {
            Debug.LogError($"[{nameof(NetworkConnectionUI)}] RoomAccessController가 없습니다. 비밀번호 기능을 쓰려면 " +
                            "NetworkManager 오브젝트에 추가하세요.");
        }

        if (relayService == null)
        {
            Debug.LogError($"[{nameof(NetworkConnectionUI)}] RelayRoomService가 없습니다. Relay 접속을 쓰려면 " +
                            "NetworkManager 오브젝트에 추가하세요.");
        }

        networkManager.OnServerStarted += RefreshStatus;
        networkManager.OnClientConnectedCallback += HandleClientConnected;
        networkManager.OnClientDisconnectCallback += HandleClientDisconnected;
        subscribed = true;

        RefreshStatus();
        SetRoomCodeDisplay(string.Empty);
    }

    private void OnDisable()
    {
        if (waitForSingletonRoutine != null)
        {
            StopCoroutine(waitForSingletonRoutine);
            waitForSingletonRoutine = null;
        }

        if (networkManager == null || !subscribed) return;

        networkManager.OnServerStarted -= RefreshStatus;
        networkManager.OnClientConnectedCallback -= HandleClientConnected;
        networkManager.OnClientDisconnectCallback -= HandleClientDisconnected;
        subscribed = false;
    }

    // ==================== 방 만들기 (Host) ====================

    private async void HandleCreateRoom()
    {
        if (roomAccess != null)
        {
            string password = hostPasswordInput != null ? hostPasswordInput.text : string.Empty;
            roomAccess.HostSetPassword(password);
            // 호스트 자신도 ConnectionApproval을 거치므로(clientId=0), 서버에 등록한 것과
            // 동일한 비밀번호를 자기 자신의 ConnectionData(payload)에도 실어야 승인을 통과한다.
            roomAccess.ClientSetJoinPassword(password);
        }
        if (relayService == null) return;

        SetInteractable(false);
        SetStatus("Relay 방 생성 중...");

        string joinCode = await relayService.CreateRoomAsync(maxConnections);
        if (string.IsNullOrEmpty(joinCode))
        {
            SetStatus("방 생성 실패. Unity Services 연동/네트워크 상태를 확인하세요.");
            SetInteractable(true);
            return;
        }

        if (roomAccess != null)
        {
            roomAccess.HostSetPassword(hostPasswordInput != null ? hostPasswordInput.text : string.Empty);
            SetRoomCodeDisplay(joinCode);
            GUIUtility.systemCopyBuffer = joinCode; // 사라지기 전에 자동으로 클립보드에 복사
            SetStatus("방 코드가 클립보드에 복사되었습니다.");
        }

        networkManager.StartHost();
        SetRoomCodeDisplay(joinCode);
        RefreshStatus();
    }

    // ==================== 참가하기 (Client) ====================

    private async void HandleJoinRoom()
    {
        if (relayService == null) return;

        string code = joinCodeInput != null ? joinCodeInput.text : string.Empty;

        SetInteractable(false);
        SetStatus("접속 중...");

        bool joined = await relayService.JoinRoomAsync(code);
        if (!joined)
        {
            SetStatus("방 코드가 올바르지 않거나 Relay 접속에 실패했습니다.");
            SetInteractable(true);
            return;
        }

        if (roomAccess != null)
        {
            roomAccess.ClientSetJoinPassword(joinPasswordInput != null ? joinPasswordInput.text : string.Empty);
        }

        networkManager.StartClient();
        RefreshStatus();
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
            SetInteractable(true);
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

    private void SetRoomCodeDisplay(string joinCode)
    {
        if (!roomCodeDisplayText) return;
        roomCodeDisplayText.text = string.IsNullOrEmpty(joinCode) ? string.Empty : $"방 코드: {joinCode}";
    }
}
