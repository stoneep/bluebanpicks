using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(NetworkManager))]
public class DraftSessionBootstrap : MonoBehaviour
{
    [Tooltip("DraftSessionServer + NetworkObject가 붙어있는 프리팹")]
    [SerializeField] private DraftSessionServer sessionPrefab;

    [Header("Scene Transition")]
    [Tooltip("접속/방 생성 화면(예: Title) 씬에서 이 스크립트가 실행된다는 전제 하에, " +
             "세션 스폰 직후 이동할 대기실 씬 이름. Build Settings에 등록되어 있어야 하고, " +
             "NetworkManager 인스펙터에서 Enable Scene Management가 켜져 있어야 한다. " +
             "빈 문자열로 두면 씬 전환 없이 지금 씬에 그대로 머문다(기존 동작과 동일).")]
    [SerializeField] private string lobbySceneName = "MainLobby";

    private NetworkManager networkManager;

    private void Awake()
    {
        networkManager = GetComponent<NetworkManager>();
    }

    private void OnEnable()
    {
        Debug.Log($"[{nameof(DraftSessionBootstrap)}] OnEnable, this={GetEntityId()} @ frame {Time.frameCount}");
        networkManager.OnServerStarted += HandleServerStarted;
        networkManager.OnServerStopped += HandleServerStopped;
    }

    private void OnDisable()
    {
        networkManager.OnServerStarted -= HandleServerStarted;
        networkManager.OnServerStopped -= HandleServerStopped;
    }
    
    private void HandleServerStarted()
    {
        Debug.Log($"[{nameof(DraftSessionBootstrap)}] HandleServerStarted called, " +
                  $"this={GetEntityId()} @ frame {Time.frameCount}");
        if (!networkManager.IsServer) return;
        SpawnSession();
    }

    private void SpawnSession()
    {
        if (DraftSessionServer.Instance != null)
        {
            Debug.LogWarning($"[{nameof(DraftSessionBootstrap)}] 세션이 이미 스폰되어 있습니다. 중복 스폰을 건너뜁니다.");
            return;
        }

        if (sessionPrefab == null)
        {
            Debug.LogError($"[{nameof(DraftSessionBootstrap)}] sessionPrefab이 할당되지 않았습니다. 인스펙터에서 연결하세요.");
            return;
        }

        EnsurePrefabRegistered();

        var instance = Instantiate(sessionPrefab);
        var networkObject = instance.GetComponent<NetworkObject>();

        if (networkObject == null)
        {
            Debug.LogError($"[{nameof(DraftSessionBootstrap)}] sessionPrefab에 NetworkObject 컴포넌트가 없습니다.");
            Destroy(instance.gameObject);
            return;
        }
        
        networkObject.Spawn();
        Debug.Log($"[{nameof(DraftSessionBootstrap)}] DraftSessionServer 스폰 완료.");

        LoadLobbySceneIfConfigured();
    }
    
    private void LoadLobbySceneIfConfigured()
    {
        if (string.IsNullOrEmpty(lobbySceneName))
        {
            return;
        }
        
        StartCoroutine(LoadLobbySceneNextFrame());
    }

    private IEnumerator LoadLobbySceneNextFrame()
    {
        yield return null;

        var sceneManager = networkManager.SceneManager;
        if (sceneManager == null)
        {
            Debug.LogError($"[{nameof(DraftSessionBootstrap)}] NetworkManager의 Scene Management가 꺼져 있어 " +
                            "대기실 씬으로 전환할 수 없습니다. 인스펙터에서 Enable Scene Management를 켜주세요.");
            yield break;
        }
        
        sceneManager.OnLoadEventCompleted += HandleLobbyLoadEventCompleted;

        var status = sceneManager.LoadScene(lobbySceneName, LoadSceneMode.Single);
        Debug.Log($"[{nameof(DraftSessionBootstrap)}] LoadScene('{lobbySceneName}') 요청, status={status} @ frame {Time.frameCount}");

        if (status != SceneEventProgressStatus.Started)
        {
            sceneManager.OnLoadEventCompleted -= HandleLobbyLoadEventCompleted;
            Debug.LogError($"[{nameof(DraftSessionBootstrap)}] 대기실 씬 전환을 시작하지 못했습니다: {status}. " +
                            $"씬 '{lobbySceneName}'이 Build Settings에 등록되어 있는지 확인하세요.");
        }
    }

    private void HandleLobbyLoadEventCompleted(string sceneName, LoadSceneMode mode,
        System.Collections.Generic.List<ulong> clientsCompleted, System.Collections.Generic.List<ulong> clientsTimedOut)
    {
        networkManager.SceneManager.OnLoadEventCompleted -= HandleLobbyLoadEventCompleted;

        if (clientsTimedOut != null && clientsTimedOut.Count > 0)
        {
            Debug.LogError($"[{nameof(DraftSessionBootstrap)}] '{sceneName}' 씬 로드가 일부 클라이언트에서 " +
                            $"타임아웃됐습니다. timedOut=[{string.Join(",", clientsTimedOut)}], " +
                            $"completed=[{string.Join(",", clientsCompleted)}]");
        }
        else
        {
            Debug.Log($"[{nameof(DraftSessionBootstrap)}] '{sceneName}' 씬 로드 완료. " +
                       $"completed=[{string.Join(",", clientsCompleted)}]");
        }
    }
    
    private void EnsurePrefabRegistered()
    {
        try
        {
            networkManager.AddNetworkPrefab(sessionPrefab.gameObject);
        }
        catch (System.Exception)
        {
        }
    }

    private void HandleServerStopped(bool _)
    {
    }
}
