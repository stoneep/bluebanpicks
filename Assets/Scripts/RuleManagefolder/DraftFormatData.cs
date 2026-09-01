using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class DraftFormatData : IDraftFormat
{
    [SerializeField] private List<DraftRoundConfig> rounds = new();

    public IReadOnlyList<DraftRoundConfig> Rounds => rounds;

    public DraftFormatData() { }

    public DraftFormatData(IEnumerable<DraftRoundConfig> source)
    {
        rounds = source != null ? new List<DraftRoundConfig>(source) : new List<DraftRoundConfig>();
    }

    public DraftFormatData AddRound(DraftRoundConfig round)
    {
        if (round == null) throw new ArgumentNullException(nameof(round));
        rounds.Add(round);
        return this;
    }

    public void RemoveRoundAt(int index)
    {
        if (index < 0 || index >= rounds.Count) return;
        rounds.RemoveAt(index);
    }

    public void Clear() => rounds.Clear();
    
    public static DraftFormatData FromPreset(DraftFormatSO preset) =>
        preset != null ? new DraftFormatData(preset.Rounds) : new DraftFormatData();
    
    public string ToJson() => JsonUtility.ToJson(new Wrapper { rounds = rounds });

    public static DraftFormatData FromJson(string json)
    {
        var wrapper = JsonUtility.FromJson<Wrapper>(json);
        return new DraftFormatData(wrapper?.rounds ?? new List<DraftRoundConfig>());
    }

    [Serializable]
    private class Wrapper
    {
        public List<DraftRoundConfig> rounds;
    }
}
