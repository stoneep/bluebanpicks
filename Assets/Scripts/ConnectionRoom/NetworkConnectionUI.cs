using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class NetworkConnectionUI : MonoBehaviour
{
    [Header("Room 생성 (Host)")]
    [SerializeField] private TMP_InputField hostPasswordInput;
    [SerializeField] private Button createRoomButton;
    [SerializeField] private TMP_Text roomCodeDisplayText;    
    [SerializeField] private int maxConnections = 8;          

    [Header("Room 참가 (Client)")]
    [SerializeField] private TMP_InputField joinCodeInput;    
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
    
    private const float SingletonPollInterval = 0.1f;
    private const float SingletonPollTimeout = 5f;

    private void Start()
    {
        TryBindNetworkManager();
    }

    private void OnEnable()
    {
        if (!subscribed) TryBindNetworkManager();
    }

    private void TryBindNetworkManager()
    {
        if (subscribed) return;

        networkManager = NetworkManager.Singleton;
        if (networkManager == null)
        {
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
    

    private async void HandleCreateRoom()
    {
        if (roomAccess != null)
        {
            string password = hostPasswordInput != null ? hostPasswordInput.text : string.Empty;
            roomAccess.HostSetPassword(password);
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
            GUIUtility.systemCopyBuffer = joinCode;
            SetStatus("방 코드가 클립보드에 복사되었습니다.");
        }

        networkManager.StartHost();
        SetRoomCodeDisplay(joinCode);
        RefreshStatus();
    }
    

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
    

    private void HandleClientConnected(ulong clientId) => RefreshStatus();

    private void HandleClientDisconnected(ulong clientId)
    {
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
