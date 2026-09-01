using System;
using System.Collections.Generic;
using Unity.Networking.Transport.Relay;
using Unity.Services.Relay.Models;

public static class RelayUtils
{
    public static RelayServerData HostRelayData(Allocation allocation, string connectionType = "dtls")
    {
        RelayServerEndpoint endpoint = GetEndpointForConnectionType(allocation.ServerEndpoints, connectionType);
        if (endpoint == null)
            throw new Exception($"[RelayUtils] '{connectionType}' 타입의 엔드포인트를 찾을 수 없습니다.");
        
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
