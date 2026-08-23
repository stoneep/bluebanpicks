using System;
using Unity.Collections;
using Unity.Netcode;

/// <summary>
/// DraftRoundConfig(class, string 필드 포함)의 네트워크 전송용 대응 구조체.
///
/// NGO의 NetworkList&lt;T&gt;/NetworkVariable&lt;T&gt;(단순 값 래핑 경로)는
/// unmanaged 타입만 담을 수 있어서 System.string을 가진 클래스를 그대로 실을 수 없다.
/// 그래서:
///  - 에디터 인스펙터, JSON, 게임 로직(RuleManager 등)은 계속 DraftRoundConfig(class)를 쓰고
///  - "네트워크 경계를 넘는 순간"에만 이 struct로 변환한다.
///
/// FixedString32/64Bytes를 쓰므로 라운드 이름은 대략 29자, 패턴 문자열은 대략 61자까지
/// 담을 수 있다(둘 다 이 프로젝트의 실제 사용 범위보다 넉넉함). 이 길이를 넘는 값은
/// ToNetwork()에서 잘려 들어가므로, 대기실 UI에서 입력 길이를 미리 제한해두는 걸 권장.
/// </summary>
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
