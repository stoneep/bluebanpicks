using System;
using Unity.Collections;
using Unity.Netcode;

/// <summary>
/// "이 clientId를 가진 사람의 닉네임은 무엇인가"를 담는 네트워크 전송용 struct.
/// DraftSessionServer.Nicknames(NetworkList)에 클라이언트 접속/해제 시점에만 추가/제거되므로
/// (진행 중 매 프레임 갱신되는 값이 아님) NetworkDraftAction과 마찬가지로 트래픽 부담이 적다.
/// </summary>
public struct ClientNicknameEntry : INetworkSerializable, IEquatable<ClientNicknameEntry>
{
    public ulong ClientId;

    // 한글은 UTF8로 글자당 3바이트라 16자 닉네임이면 최대 48바이트까지 필요할 수 있어
    // FixedString32Bytes 대신 여유 있게 64Bytes를 사용한다.
    public FixedString64Bytes Nickname;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref ClientId);
        serializer.SerializeValue(ref Nickname);
    }

    public bool Equals(ClientNicknameEntry other) =>
        ClientId == other.ClientId && Nickname.Equals(other.Nickname);

    public override bool Equals(object obj) => obj is ClientNicknameEntry other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(ClientId, Nickname);

    public override string ToString() => $"{ClientId}:{Nickname}";
}