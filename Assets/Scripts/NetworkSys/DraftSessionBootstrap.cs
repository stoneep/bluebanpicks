using Unity.Netcode;
using UnityEngine;

/// <summary>
/// NetworkManager와 같은 GameObject(또는 같은 씬)에 배치.
/// 호스트/서버가 리슨을 시작하는 시점에 DraftSessionServer를 스폰해서,
/// 이후 접속하는 모든 클라이언트가 "이미 세션이 존재하는 상태"로 들어오게 만든다.
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

    private NetworkManager networkManager;

    private void Awake()
    {
        networkManager = GetComponent<NetworkManager>();
    }

    private void OnEnable()
    {
        Debug.Log($"[{nameof(DraftSessionBootstrap)}] OnEnable, this={GetEntityId()} @ frame {Time.frameCount}");
        Debug.Log($"[{nameof(DraftSessionBootstrap)}] HandleServerStarted called, this={GetEntityId()} @ frame {Time.frameCount}", this);
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

        // 소유권을 지정하지 않으면 서버가 기본 소유자가 된다 (호스트 권위형 설계와 일치).
        networkObject.Spawn();
        Debug.Log($"[{nameof(DraftSessionBootstrap)}] DraftSessionServer 스폰 완료.");
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
