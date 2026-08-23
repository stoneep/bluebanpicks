using System;
using System.Collections.Generic;

/// <summary>
/// BanPhase와 PickPhase는 "진영별 슬롯 개수만큼 채울 때까지
/// ITurnOrderRule이 정해주는 순서대로 진영을 번갈아 받는다"는
/// 로직이 완전히 동일하고, 다른 점은 이름(PhaseName)뿐이다.
/// 그래서 공통 로직을 이 base 클래스에 두고,
/// BanPhase/PickPhase는 생성자만 다르게 넘겨주는 얇은 서브클래스로 둔다.
///
/// 선공/후공의 슬롯 개수가 서로 달라도(예: 5 vs 6) 동작하도록,
/// 이미 슬롯이 다 찬 진영은 자동으로 건너뛰고 다음 진영에게 턴을 준다.
/// </summary>
public abstract class DraftPhaseBase : IDraftPhase
{
    private readonly Dictionary<DraftSide, int> slotCounts;
    private readonly Dictionary<DraftSide, List<string>> selections;
    private readonly ITurnOrderRule turnOrder;

    private int turnIndex;
    private bool isEntered;

    public string PhaseName { get; }
    public DraftSide CurrentSide { get; private set; }

    protected DraftPhaseBase(string phaseName, int firstSlotCount, int secondSlotCount, ITurnOrderRule turnOrder)
    {
        if (firstSlotCount < 0 || secondSlotCount < 0)
            throw new ArgumentOutOfRangeException(nameof(firstSlotCount), "슬롯 개수는 0 이상이어야 합니다.");

        PhaseName = phaseName;
        this.turnOrder = turnOrder ?? throw new ArgumentNullException(nameof(turnOrder));

        slotCounts = new Dictionary<DraftSide, int>
        {
            [DraftSide.First] = firstSlotCount,
            [DraftSide.Second] = secondSlotCount
        };

        selections = new Dictionary<DraftSide, List<string>>
        {
            [DraftSide.First] = new List<string>(firstSlotCount),
            [DraftSide.Second] = new List<string>(secondSlotCount)
        };
    }

    public bool IsComplete =>
        selections[DraftSide.First].Count >= slotCounts[DraftSide.First] &&
        selections[DraftSide.Second].Count >= slotCounts[DraftSide.Second];

    public void Enter()
    {
        turnIndex = 0;
        isEntered = true;
        AdvanceToNextAvailableSide();
    }

    public bool SubmitAction(DraftSide side, string characterId, out string error)
    {
        error = null;

        if (!isEntered)
        {
            error = $"[{PhaseName}] 페이즈가 아직 시작되지 않았습니다 (Enter() 먼저 호출).";
            return false;
        }

        if (IsComplete)
        {
            error = $"[{PhaseName}] 페이즈가 이미 종료되었습니다.";
            return false;
        }

        if (string.IsNullOrEmpty(characterId))
        {
            error = $"[{PhaseName}] characterId가 비어 있습니다.";
            return false;
        }

        if (side != CurrentSide)
        {
            error = $"[{PhaseName}] {side}의 차례가 아닙니다. 현재 차례: {CurrentSide}";
            return false;
        }

        if (selections[side].Count >= slotCounts[side])
        {
            error = $"[{PhaseName}] {side} 슬롯이 이미 가득 찼습니다.";
            return false;
        }

        selections[side].Add(characterId);
        turnIndex++;
        AdvanceToNextAvailableSide();
        return true;
    }

    public IReadOnlyList<string> GetSelections(DraftSide side) => selections[side];

    private void AdvanceToNextAvailableSide()
    {
        if (IsComplete) return;

        // 한쪽 슬롯이 먼저 다 차는 경우(개수가 다를 때) 그 진영은 건너뛴다.
        // (First+Second 슬롯 합만큼만 순회하면 항상 답이 나오므로 safety guard로 총합+여유를 둔다)
        int maxIterations = slotCounts[DraftSide.First] + slotCounts[DraftSide.Second] + 2;
        for (int i = 0; i < maxIterations; i++)
        {
            var candidate = turnOrder.GetSideForTurn(turnIndex);
            if (selections[candidate].Count < slotCounts[candidate])
            {
                CurrentSide = candidate;
                return;
            }
            turnIndex++;
        }
    }
}
