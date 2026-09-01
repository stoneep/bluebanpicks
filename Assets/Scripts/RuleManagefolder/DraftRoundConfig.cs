using System;
using UnityEngine;

[Serializable]
public class DraftRoundConfig
{
    [Header("라운드 이름 (UI/로그용, 선택)")]
    [SerializeField] private string roundName = "";

    [Header("밴 슬롯")]
    [SerializeField] private int firstBanSlots;
    [SerializeField] private int secondBanSlots;

    [Header("픽 슬롯")]
    [SerializeField] private int firstPickSlots;
    [SerializeField] private int secondPickSlots;

    [Header("이 라운드에서 먼저 시작하는 진영")]
    [Tooltip("밴/픽 모두 이 진영부터 시작한다. 예: 전반=First, 후반=Second로 두면 이니셔티브가 반전된다.")]
    [SerializeField] private DraftSide startingSide = DraftSide.First;

    [Header("커스텀 순서 패턴 (비워두면 startingSide 기준 단순 교대)")]
    [Tooltip("A=선공, B=후공. 예: ABABAB / BABABA. 채워져 있으면 startingSide보다 우선한다.")]
    [SerializeField] private string banOrderPattern = "";
    [SerializeField] private string pickOrderPattern = "";

    public string RoundName => roundName;
    public int FirstBanSlots => firstBanSlots;
    public int SecondBanSlots => secondBanSlots;
    public int FirstPickSlots => firstPickSlots;
    public int SecondPickSlots => secondPickSlots;
    public DraftSide StartingSide => startingSide;
    public string BanOrderPattern => banOrderPattern;
    public string PickOrderPattern => pickOrderPattern;

    public DraftRoundConfig() { }

    public DraftRoundConfig(
        int firstBanSlots, int secondBanSlots,
        int firstPickSlots, int secondPickSlots,
        DraftSide startingSide = DraftSide.First,
        string roundName = "",
        string banOrderPattern = "",
        string pickOrderPattern = "")
    {
        this.roundName = roundName;
        this.firstBanSlots = firstBanSlots;
        this.secondBanSlots = secondBanSlots;
        this.firstPickSlots = firstPickSlots;
        this.secondPickSlots = secondPickSlots;
        this.startingSide = startingSide;
        this.banOrderPattern = banOrderPattern;
        this.pickOrderPattern = pickOrderPattern;
    }
    
    public DraftRoundConfig WithFlippedInitiative(string newRoundName = null) => new DraftRoundConfig(
        firstBanSlots, secondBanSlots,
        firstPickSlots, secondPickSlots,
        startingSide == DraftSide.First ? DraftSide.Second : DraftSide.First,
        newRoundName ?? roundName);

    public override string ToString() =>
        $"[{roundName}] Ban(F{firstBanSlots}/S{secondBanSlots}) Pick(F{firstPickSlots}/S{secondPickSlots}) Start={startingSide}";
}
