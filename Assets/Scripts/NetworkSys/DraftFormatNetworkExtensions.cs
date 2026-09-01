using System.Collections.Generic;
using Unity.Netcode;

public static class DraftFormatNetworkExtensions
{

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
