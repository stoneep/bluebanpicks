using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 대기실(Lobby)에서 호스트가 조립/수정하는 드래프트 포맷.
///
/// DraftFormatSO(에셋)와 달리 ScriptableObject가 아닌 순수 데이터라
/// - 대기실 UI에서 런타임에 라운드를 추가/삭제/수정할 수 있고
/// - JsonUtility.ToJson으로 문자열화해서 네트워크로 보내거나
///   (다음 단계에서) INetworkSerializable 구조체로 변환하기도 쉽다.
///
/// 절대 DraftFormatSO 에셋 자체를 런타임에 수정하지 말 것 - 에셋은 항상
/// "프리셋 템플릿"으로만 쓰고, 실제로 진행할 드래프트는 이 클래스의 인스턴스로 다룬다.
/// </summary>
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

    /// <summary>
    /// 프리셋 SO를 불러와서 대기실 편집용 복사본을 만들 때 사용.
    /// (원본 SO는 건드리지 않음)
    /// </summary>
    public static DraftFormatData FromPreset(DraftFormatSO preset) =>
        preset != null ? new DraftFormatData(preset.Rounds) : new DraftFormatData();

    /// <summary>
    /// 이후 NGO 단계에서 NetworkVariable/RPC로 보낼 때 쓸 JSON 스냅샷.
    /// 드래프트 설정은 진행 중에 바뀌지 않으므로 1회성 전송으로 충분하다.
    /// </summary>
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
