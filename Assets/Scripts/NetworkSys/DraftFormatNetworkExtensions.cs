using System.Collections.Generic;
using Unity.Netcode;

/// <summary>
/// "게임 로직/에디터용 데이터(DraftRoundConfig, DraftFormatData)"와
/// "네트워크 전송용 데이터(NetworkDraftRoundConfig, NetworkList&lt;NetworkDraftRoundConfig&gt;)" 사이의
/// 변환만 담당하는 계층. 이 파일 밖에서는 어느 쪽도 서로의 존재를 몰라도 되게 하는 게 목적:
///  - RuleManager/DraftPhaseBase 등 규칙 엔진은 DraftRoundConfig/IDraftFormat만 알면 됨
///  - NGO 세션(다음 단계에서 만들 DraftSessionServer)은 이 확장 메서드로만 경계를 넘나듦
/// </summary>
public static class DraftFormatNetworkExtensions
{
    // ---------- DraftRoundConfig <-> NetworkDraftRoundConfig ----------

    public static NetworkDraftRoundConfig ToNetwork(this DraftRoundConfig round)
    {
        if (round == null) return default;

        return new NetworkDraftRoundConfig
        {
            roundName = round.RoundName ?? string.Empty,
            firstBanSlots = round.FirstBanSlots,
            secondBanSlots = round.SecondBanSlots,
            firstPickSlots = round.FirstPickSlots,
            secondPickSlots = round.SecondPickSlots,
            startingSide = round.StartingSide,
            banOrderPattern = round.BanOrderPattern ?? string.Empty,
            pickOrderPattern = round.PickOrderPattern ?? string.Empty,
        };
    }

    public static DraftRoundConfig ToRoundConfig(this NetworkDraftRoundConfig net) => new DraftRoundConfig(
        net.firstBanSlots, net.secondBanSlots,
        net.firstPickSlots, net.secondPickSlots,
        net.startingSide,
        net.roundName.ToString(),
        net.banOrderPattern.ToString(),
        net.pickOrderPattern.ToString());

    // ---------- DraftFormatData <-> NetworkList<NetworkDraftRoundConfig> ----------

    /// <summary>
    /// 호스트가 로컬에서 편집한 DraftFormatData를 NetworkList에 그대로 반영한다.
    /// NetworkList는 요소 단위로 변경 이벤트(OnListChanged)를 쏘므로,
    /// 매번 전체를 Clear+재추가하지 않고 "달라진 부분만" 갱신하고 싶다면
    /// 라운드 개수가 같을 때는 SetAt으로, 개수가 다를 때만 Add/RemoveAt으로 처리하는 게 좋다.
    /// 지금은 구현을 단순하게 가져가려고 항상 전체를 다시 씀 (라운드는 몇 개 안 되므로 비용 미미).
    /// </summary>
    public static void CopyTo(this DraftFormatData data, NetworkList<NetworkDraftRoundConfig> target)
    {
        target.Clear();
        foreach (var round in data.Rounds)
            target.Add(round.ToNetwork());
    }

    public static DraftFormatData ToDraftFormatData(this NetworkList<NetworkDraftRoundConfig> networkRounds)
    {
        var rounds = new List<DraftRoundConfig>(networkRounds.Count);
        foreach (var net in networkRounds)
            rounds.Add(net.ToRoundConfig());
        return new DraftFormatData(rounds);
    }

    // ---------- 단건 편집 helper (대기실 UI에서 라운드 하나만 고칠 때) ----------

    /// <summary>인덱스의 라운드 하나만 교체. 라운드 개수 변경 없이 값만 바뀔 때(슬롯 수 조절 등) 사용.</summary>
    public static void SetRound(this NetworkList<NetworkDraftRoundConfig> target, int index, DraftRoundConfig round)
    {
        if (index < 0 || index >= target.Count) return;
        target[index] = round.ToNetwork();
    }

    public static void AddRound(this NetworkList<NetworkDraftRoundConfig> target, DraftRoundConfig round) =>
        target.Add(round.ToNetwork());

    public static void RemoveRoundAt(this NetworkList<NetworkDraftRoundConfig> target, int index)
    {
        if (index < 0 || index >= target.Count) return;
        target.RemoveAt(index);
    }
}
