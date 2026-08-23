/// <summary>
/// 픽 페이즈: 선공 firstPickSlots개 / 후공 secondPickSlots개를
/// ITurnOrderRule 순서대로 번갈아 픽한다.
/// 로직은 전부 DraftPhaseBase에 있고, 이름만 "Pick"으로 고정.
/// </summary>
public sealed class PickPhase : DraftPhaseBase
{
    public PickPhase(int firstPickSlots, int secondPickSlots, ITurnOrderRule turnOrder)
        : base("Pick", firstPickSlots, secondPickSlots, turnOrder)
    {
    }
}