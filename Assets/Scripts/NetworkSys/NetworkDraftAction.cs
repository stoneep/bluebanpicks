using System;
using Unity.Collections;
using Unity.Netcode;

/// <summary>
/// "누가 무엇을 밴/픽했는지" 한 건을 담는 네트워크 전송용 struct.
/// DraftSessionServer의 actionLog(NetworkList)에 순서대로 쌓이며,
/// NetworkList가 서버->클라 자동 동기화를 책임지므로
/// 이 로그 하나만으로 진행 중 접속한 클라이언트(late-join)도
/// 지금까지의 밴/픽 결과를 그대로 복원할 수 있다.
/// </summary>
public struct NetworkDraftAction : INetworkSerializable, IEquatable<NetworkDraftAction>
{
    public DraftSide side;
    public FixedString64Bytes characterId;
    public DraftResultType resultType;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref side);
        serializer.SerializeValue(ref characterId);
        serializer.SerializeValue(ref resultType);
    }

    public bool Equals(NetworkDraftAction other) =>
        side == other.side &&
        characterId.Equals(other.characterId) &&
        resultType == other.resultType;

    public override bool Equals(object obj) => obj is NetworkDraftAction other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(side, characterId, resultType);

    public override string ToString() => $"{side} {resultType} {characterId}";
}