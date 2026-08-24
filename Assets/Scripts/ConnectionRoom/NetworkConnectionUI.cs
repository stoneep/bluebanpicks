using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 로컬 테스트용 최소 연결 화면. Start Host / Start Client 버튼과 상태 텍스트만 제공한다.
/// Client는 Transport에 설정된 기본 주소(보통 127.0.0.1, 로컬 테스트 기준)로 접속한다.
/// 나중에 Relay를 붙이면 이 화면은 "Relay 코드 입력 후 접속"으로 교체될 자리다 - 지금은
/// 순수 로컬 루프백으로 드래프트 로직/동기화가 맞게 도는지 확인하는 용도.
/// </summary>
public class NetworkConnectionUI : MonoBehaviour
{
    [SerializeField] private Button startHostButton;
    [SerializeField] private Button startClientButton;
    [SerializeField] private TMP_Text statusText;

    private void Awake()
    {
        startHostButton.onClick.AddListener(HandleStartHost);
        startClientButton.onClick.AddListener(HandleStartClient);
    }

    private void OnEnable()
    {
        var nm = NetworkManager.Singleton;
        if (nm == null)
        {
            Debug.LogError($"[{nameof(NetworkConnectionUI)}] NetworkManager.Singleton이 없습니다. 씬에 배치되어 있는지 확인하세요.");
            return;
        }

        nm.OnServerStarted += RefreshStatus;
        nm.OnClientConnectedCallback += HandleClientConnected;
        nm.OnClientDisconnectCallback += HandleClientDisconnected;
        RefreshStatus();
    }

    private void OnDisable()
    {
        var nm = NetworkManager.Singleton;
        if (nm == null) return;

        nm.OnServerStarted -= RefreshStatus;
        nm.OnClientConnectedCallback -= HandleClientConnected;
        nm.OnClientDisconnectCallback -= HandleClientDisconnected;
    }

    private void HandleStartHost()
    {
        NetworkManager.Singleton.StartHost();
        RefreshStatus();
    }

    private void HandleStartClient()
    {
        NetworkManager.Singleton.StartClient();
        RefreshStatus();
    }

    private void HandleClientConnected(ulong clientId) => RefreshStatus();
    private void HandleClientDisconnected(ulong clientId) => RefreshStatus();

    private void RefreshStatus()
    {
        var nm = NetworkManager.Singleton;

        if (!nm.IsClient && !nm.IsServer)
        {
            statusText.text = "연결 안 됨";
            startHostButton.interactable = true;
            startClientButton.interactable = true;
            return;
        }

        startHostButton.interactable = false;
        startClientButton.interactable = false;

        string role = nm.IsHost ? "호스트" : (nm.IsServer ? "서버" : "클라이언트");
        statusText.text = $"{role} (내 clientId={nm.LocalClientId}, 접속자 {nm.ConnectedClientsIds.Count}명)";
    }
}
