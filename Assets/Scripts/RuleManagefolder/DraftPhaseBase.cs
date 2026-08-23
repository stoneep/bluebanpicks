using System;
using System.Collections.Generic;

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
        // turnOrder가 SequenceTurnOrderRule이면, 이 페이즈의 슬롯 수와
        // 시퀀스 구성(First/Second 개수)이 일치하는지 시작 시점에 미리 검증한다.
        // 안 맞으면 여기서 바로 예외가 터지므로, 플레이 도중 마지막 값 반복/루프로
        // 조용히 동작이 이상해지는 상황을 막는다.
        if (turnOrder is SequenceTurnOrderRule sequenceRule)
        {
            sequenceRule.Validate(slotCounts[DraftSide.First], slotCounts[DraftSide.Second]);
        }

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