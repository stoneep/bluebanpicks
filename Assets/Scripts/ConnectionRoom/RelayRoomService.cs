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

[RequireComponent(typeof(NetworkManager))]
public class RelayRoomService : MonoBehaviour
{
    private const string ConnectionType = "dtls";
    private NetworkManager networkManager;
    private UnityTransport transport;
    private bool servicesReady;

    public string CurrentJoinCode { get; private set; }

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
