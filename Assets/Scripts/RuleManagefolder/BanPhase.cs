
public sealed class BanPhase : DraftPhaseBase
{
    public BanPhase(int firstBanSlots, int secondBanSlots, ITurnOrderRule turnOrder)
        : base("Ban", firstBanSlots, secondBanSlots, turnOrder)
    {
    }
}