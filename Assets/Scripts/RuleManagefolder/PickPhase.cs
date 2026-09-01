
public sealed class PickPhase : DraftPhaseBase
{
    public PickPhase(int firstPickSlots, int secondPickSlots, ITurnOrderRule turnOrder)
        : base("Pick", firstPickSlots, secondPickSlots, turnOrder)
    {
    }
}