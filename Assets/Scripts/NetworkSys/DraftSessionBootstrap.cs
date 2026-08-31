using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// NetworkManager와 같은 GameObject(또는 같은 씬)에 배치.
/// 호스트/서버가 리슨을 시작하는 시점에 DraftSessionServer를 스폰해서,
/// 이후 접속하는 모든 클라이언트가 "이미 세션이 존재하는 상태"로 들어오게 만든다.
/// lobbySceneName이 설정되어 있으면, 스폰 직후 접속 화면 씬에서 대기실 씬으로 자동 전환한다
/// (접속 화면과 대기실을 서로 다른 씬으로 분리하고 싶을 때 사용).
///
/// ── 사전 준비 (에디터에서 1회) ──────────────────────────────
/// 1) DraftSessionServer 컴포넌트를 붙인 프리팹을 만든다.
///    - GameObject 생성 → Add Component: DraftSessionServer
///    - Add Component: NetworkObject (Netcode) ← 반드시 필요, 없으면 Spawn()에서 에러
///    - 프리팹으로 저장 (예: Assets/Prefabs/Network/DraftSessionServer.prefab)
/// 2) NetworkManager 인스펙터 → Network Prefabs 리스트(또는 연결된
///    Default Network Prefabs List 에셋)에 위 프리팹을 등록한다.
///    (등록 안 해도 아래 EnsurePrefabRegistered()가 런타임에 한 번 더 시도하지만,
///     정식 배포 빌드에서는 에디터에서 등록해두는 걸 권장)
/// 3) 이 스크립트를 NetworkManager 오브젝트에 붙이고, sessionPrefab 필드에 위 프리팹을 할당한다.
/// ──────────────────────────────────────────────────────────
/// </summary>
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
//        Debug.Log($"[{nameof(DraftSessionBootstrap)}] OnEnable, this={GetEntityId()} @ frame {Time.frameCount}");
        networkManager.OnServerStarted += HandleServerStarted;
        networkManager.OnServerStopped += HandleServerStopped;
    }

    private void OnDisable()
    {
        networkManager.OnServerStarted -= HandleServerStarted;
        networkManager.OnServerStopped -= HandleServerStopped;
    }

    /// <summary>
    /// StartHost()/StartServer()가 성공해서 서버가 실제로 리슨을 시작한 시점에 호출됨.
    /// 서버(호스트 포함)에서만 의미가 있으므로 IsServer로 한 번 더 방어한다.
    /// </summary>
    private void HandleServerStarted()
    {
//        Debug.Log($"[{nameof(DraftSessionBootstrap)}] HandleServerStarted called, " +
    //              $"this={GetEntityId()} @ frame {Time.frameCount}");
        if (!networkManager.IsServer) return;
        SpawnSession();
    }

    private void SpawnSession()
    {
        if (DraftSessionServer.Instance != null)
        {
 //           Debug.LogWarning($"[{nameof(DraftSessionBootstrap)}] 세션이 이미 스폰되어 있습니다. 중복 스폰을 건너뜁니다.");
            return;
        }

        if (sessionPrefab == null)
        {
  //          Debug.LogError($"[{nameof(DraftSessionBootstrap)}] sessionPrefab이 할당되지 않았습니다. 인스펙터에서 연결하세요.");
            return;
        }

        EnsurePrefabRegistered();

        var instance = Instantiate(sessionPrefab);
        var networkObject = instance.GetComponent<NetworkObject>();

        if (networkObject == null)
        {
   //         Debug.LogError($"[{nameof(DraftSessionBootstrap)}] sessionPrefab에 NetworkObject 컴포넌트가 없습니다.");
            Destroy(instance.gameObject);
            return;
        }

        // 소유권을 지정하지 않으면 서버가 기본 소유자가 된다 (호스트 권위형 설계와 일치).
        networkObject.Spawn();
 //       Debug.Log($"[{nameof(DraftSessionBootstrap)}] DraftSessionServer 스폰 완료.");

        LoadLobbySceneIfConfigured();
    }

    /// <summary>
    /// 접속 화면(Title 등)과 대기실 화면(MainLobby 등)을 별도 씬으로 나눴을 때,
    /// 호스트가 방을 만든 직후 자동으로 대기실 씬으로 넘어가게 해준다.
    /// 이후 접속하는 클라이언트는 Netcode의 씬 동기화로 자동으로 같은 씬을 따라온다 -
    /// DraftSessionServer는 이미 스폰된 NetworkObject라 씬 전환에도 파괴되지 않고 그대로 유지된다
    /// (HostStartDraft가 Lobby -> Draft 씬으로 넘어갈 때와 동일한 매커니즘).
    /// </summary>
    private void LoadLobbySceneIfConfigured()
    {
        if (string.IsNullOrEmpty(lobbySceneName))
        {
            return; // 씬을 안 나눈 기존 프로젝트 구성과 호환되도록, 비워두면 아무 것도 하지 않는다.
        }

        var sceneManager = networkManager.SceneManager;
        if (sceneManager == null)
        {
  //          Debug.LogError($"[{nameof(DraftSessionBootstrap)}] NetworkManager의 Scene Management가 꺼져 있어 " +
 //                           "대기실 씬으로 전환할 수 없습니다. 인스펙터에서 Enable Scene Management를 켜주세요.");
            return;
        }

        var status = sceneManager.LoadScene(lobbySceneName, LoadSceneMode.Single);
        if (status != SceneEventProgressStatus.Started)
        {
    //        Debug.LogError($"[{nameof(DraftSessionBootstrap)}] 대기실 씬 전환을 시작하지 못했습니다: {status}. " +
    //                        $"씬 '{lobbySceneName}'이 Build Settings에 등록되어 있는지 확인하세요.");
        }
    }

    /// <summary>
    /// 에디터에서 NetworkPrefabs 리스트 등록을 깜빡했을 때를 위한 안전망.
    /// 이미 등록돼 있으면 예외를 잡아 조용히 넘어간다.
    /// (NetworkConfig.Prefabs 내부 구조는 NGO 버전마다 조금씩 달라질 수 있어서,
    ///  공식 공개 API인 NetworkManager.AddNetworkPrefab을 사용한다.)
    /// </summary>
    private void EnsurePrefabRegistered()
    {
        try
        {
            networkManager.AddNetworkPrefab(sessionPrefab.gameObject);
        }
        catch (System.Exception)
        {
            // 이미 등록돼 있는 경우 등 - 정상 동작이므로 무시. 등록 자체가 안 된 진짜 실패라면
            // 뒤이은 networkObject.Spawn()에서 다시 에러 로그가 뜨므로 놓치지 않는다.
        }
    }

    private void HandleServerStopped(bool _)
    {
        // DraftSessionServer.OnNetworkDespawn/OnDestroy가 스스로 Instance를 정리하므로
        // 여기서 별도로 처리할 건 없다. 다음 StartHost() 때 SpawnSession()이 다시 새로 만든다.
    }
}
