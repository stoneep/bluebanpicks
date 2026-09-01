using System;
using Unity.Collections;
using Unity.Netcode;

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