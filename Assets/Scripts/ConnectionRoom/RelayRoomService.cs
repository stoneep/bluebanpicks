using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

/// <summary>
/// 서로 완전히 다른 네트워크(인터넷 너머)에 있는 플레이어끼리 접속할 수 있도록,
/// IP 직접 입력 대신 Unity Relay를 통해 연결한다. 호스트는 "방 코드"(짧은 문자열)를 만들고,
/// 클라이언트는 그 코드만 입력하면 된다 - 포트포워딩도, 공인 IP도 필요 없다.
///
/// 사전 준비 (Unity 에디터 / 대시보드에서 1회, 코드로는 대신할 수 없는 부분):
///  1) Package Manager에서 설치: Authentication, Relay
///     (Services Core, Netcode for GameObjects의 Transport 패키지는 보통 같이 따라온다)
///  2) Project Settings > Services 에서 이 프로젝트를 Unity Dashboard의 프로젝트와 연결(Link)한다.
///  3) NetworkManager의 Transport가 UnityTransport(UTP)여야 한다.
///
/// NetworkManager와 같은 GameObject에 배치. RoomAccessController(비밀번호 체크)와는 독립적으로
/// 동작하므로, Relay로 붙어도 비밀번호 검증은 그대로 같이 적용된다.
///
/// 주의: Unity Services/Relay 패키지 버전에 따라 일부 API 시그니처(RelayServerData 생성자 등)가
/// 조금씩 달라질 수 있다. 프로젝트에 실제로 설치된 패키지 버전의 문서/샘플과 대조해서 확인할 것.
/// </summary>
[RequireComponent(typeof(NetworkManager))]
public class RelayRoomService : MonoBehaviour
{
    private const string ConnectionType = "dtls"; // 암호화된 UDP. 특정 플랫폼/네트워크에서 막히면 "udp"로 교체 고려.
    private NetworkManager networkManager;
    private UnityTransport transport;
    private bool servicesReady;

    public string CurrentJoinCode { get; private set; } // 씬 전환 후에도 대기실에서 다시 꺼내 보여줄 수 있게 보관

    private void Awake()
    {
        networkManager = GetComponent<NetworkManager>();
        transport = networkManager.NetworkConfig.NetworkTransport as UnityTransport;

        if (transport == null)
        {
            Debug.LogError($"[{nameof(RelayRoomService)}] UnityTransport가 아닙니다. Relay를 쓰려면 " +
                            "NetworkManager의 Transport를 UnityTransport로 설정하세요.");
        }
    }

    /// <summary>
    /// Unity 서비스 초기화 + 익명 로그인. 별도의 계정 UI 없이 기기별로 자동 로그인된다.
    /// 이미 되어 있으면 아무 것도 하지 않으므로 여러 번 호출해도 안전하다.
    /// </summary>
    public async Task<bool> EnsureSignedInAsync()
    {
        if (servicesReady) return true;

        try
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                await UnityServices.InitializeAsync();
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            servicesReady = true;
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[{nameof(RelayRoomService)}] Unity Services 초기화/로그인 실패: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// 호스트: Relay 할당을 만들고 UnityTransport에 연결 정보를 세팅한 뒤,
    /// 참가자에게 알려줄 "방 코드"를 반환한다. 실패하면 null.
    /// 반환값을 받은 뒤에 NetworkManager.StartHost()를 호출해야 한다.
    /// </summary>
    /// <param name="maxConnections">호스트 본인을 제외한 최대 동시 접속자 수</param>
    public async Task<string> CreateRoomAsync(int maxConnections)
    {
        if (!await EnsureSignedInAsync() || transport == null) return null;

        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            transport.SetRelayServerData(RelayUtils.HostRelayData(allocation, ConnectionType));
            CurrentJoinCode = joinCode;

            Debug.Log($"[{nameof(RelayRoomService)}] Relay 방 생성 완료. 방 코드={joinCode}");
            return joinCode;
        }
        catch (Exception e)
        {
            Debug.LogError($"[{nameof(RelayRoomService)}] Relay 방 생성 실패: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// 클라이언트: 방 코드로 Relay 참가 신청 후 UnityTransport에 연결 정보를 세팅한다.
    /// 성공(true)하면 그 뒤에 NetworkManager.StartClient()를 호출해야 실제 접속이 시작된다.
    /// </summary>
    public async Task<bool> JoinRoomAsync(string joinCode)
    {
        if (!await EnsureSignedInAsync() || transport == null) return false;

        if (string.IsNullOrWhiteSpace(joinCode))
        {
            Debug.LogError($"[{nameof(RelayRoomService)}] 방 코드가 비어 있습니다.");
            return false;
        }

        try
        {
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode.Trim());
            transport.SetRelayServerData(RelayUtils.PlayerRelayData(joinAllocation, ConnectionType));
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[{nameof(RelayRoomService)}] Relay 참가 실패 (방 코드 오타/만료 가능): {e.Message}");
            return false;
        }
    }
}
