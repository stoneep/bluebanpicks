/// <summary>
/// 밴 페이즈: 선공 firstBanSlots개 / 후공 secondBanSlots개를
/// ITurnOrderRule 순서대로 번갈아 밴한다.
/// 로직은 전부 DraftPhaseBase에 있고, 이름만 "Ban"으로 고정.
/// </summary>
public sealed class BanPhase : DraftPhaseBase
{
    public BanPhase(int firstBanSlots, int secondBanSlots, ITurnOrderRule turnOrder)
        : base("Ban", firstBanSlots, secondBanSlots, turnOrder)
    {
    }
}