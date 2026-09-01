using System;
using Unity.Collections;
using Unity.Netcode;

public struct NetworkDraftRoundConfig : INetworkSerializable, IEquatable<NetworkDraftRoundConfig>
{
    public FixedString32Bytes roundName;
    public int firstBanSlots;
    public int secondBanSlots;
    public int firstPickSlots;
    public int secondPickSlots;
    public DraftSide startingSide;
    public FixedString64Bytes banOrderPattern;
    public FixedString64Bytes pickOrderPattern;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref roundName);
        serializer.SerializeValue(ref firstBanSlots);
        serializer.SerializeValue(ref secondBanSlots);
        serializer.SerializeValue(ref firstPickSlots);
        serializer.SerializeValue(ref secondPickSlots);
        serializer.SerializeValue(ref startingSide);
        serializer.SerializeValue(ref banOrderPattern);
        serializer.SerializeValue(ref pickOrderPattern);
    }

    public bool Equals(NetworkDraftRoundConfig other) =>
        roundName.Equals(other.roundName) &&
        firstBanSlots == other.firstBanSlots &&
        secondBanSlots == other.secondBanSlots &&
        firstPickSlots == other.firstPickSlots &&
        secondPickSlots == other.secondPickSlots &&
        startingSide == other.startingSide &&
        banOrderPattern.Equals(other.banOrderPattern) &&
        pickOrderPattern.Equals(other.pickOrderPattern);

    public override bool Equals(object obj) => obj is NetworkDraftRoundConfig other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(roundName);
        hash.Add(firstBanSlots);
        hash.Add(secondBanSlots);
        hash.Add(firstPickSlots);
        hash.Add(secondPickSlots);
        hash.Add(startingSide);
        hash.Add(banOrderPattern);
        hash.Add(pickOrderPattern);
        return hash.ToHashCode();
    }

    public override string ToString() =>
        $"[{roundName}] Ban(F{firstBanSlots}/S{secondBanSlots}) Pick(F{firstPickSlots}/S{secondPickSlots}) Start={startingSide}";
}
