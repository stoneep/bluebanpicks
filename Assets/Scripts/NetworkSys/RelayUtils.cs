using System;
using System.Collections.Generic;
using Unity.Networking.Transport.Relay;
using Unity.Services.Relay.Models;

/// <summary>
/// Unity Relay의 Allocation / JoinAllocation을 UnityTransport가 요구하는
/// RelayServerData 구조체로 변환한다.
///
/// 이 프로젝트에 설치된 com.unity.transport 버전에는
/// RelayServerData(Allocation, string) 생성자나 ToRelayServerData() 확장 메서드가
/// 없고, 대신 RelayServerData(string host, ushort port, byte[] allocationId,
/// byte[] connectionData, byte[] hostConnectionData, byte[] key, bool isSecure)
/// 생성자만 존재하므로, 이를 직접 채워서 만들어준다.
/// </summary>
public static class RelayUtils
{
    /// <summary>호스트용 RelayServerData 생성 (CreateAllocationAsync 결과 사용)</summary>
    public static RelayServerData HostRelayData(Allocation allocation, string connectionType = "dtls")
    {
        RelayServerEndpoint endpoint = GetEndpointForConnectionType(allocation.ServerEndpoints, connectionType);
        if (endpoint == null)
            throw new Exception($"[RelayUtils] '{connectionType}' 타입의 엔드포인트를 찾을 수 없습니다.");

        // 호스트는 자기 자신에게 연결하는 것이므로 connectionData와 hostConnectionData가 동일하다.
        return new RelayServerData(
            endpoint.Host,
            (ushort)endpoint.Port,
            allocation.AllocationIdBytes,
            allocation.ConnectionData,
            allocation.ConnectionData,
            allocation.Key,
            endpoint.Secure
        );
    }

    /// <summary>클라이언트용 RelayServerData 생성 (JoinAllocationAsync 결과 사용)</summary>
    public static RelayServerData PlayerRelayData(JoinAllocation allocation, string connectionType = "dtls")
    {
        RelayServerEndpoint endpoint = GetEndpointForConnectionType(allocation.ServerEndpoints, connectionType);
        if (endpoint == null)
            throw new Exception($"[RelayUtils] '{connectionType}' 타입의 엔드포인트를 찾을 수 없습니다.");

        return new RelayServerData(
            endpoint.Host,
            (ushort)endpoint.Port,
            allocation.AllocationIdBytes,
            allocation.ConnectionData,
            allocation.HostConnectionData,
            allocation.Key,
            endpoint.Secure
        );
    }

    private static RelayServerEndpoint GetEndpointForConnectionType(
        List<RelayServerEndpoint> endpoints, string connectionType)
    {
        foreach (RelayServerEndpoint endpoint in endpoints)
        {
            if (endpoint.ConnectionType == connectionType)
                return endpoint;
        }
        return null;
    }
}
