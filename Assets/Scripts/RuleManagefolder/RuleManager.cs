using System;
using System.Collections.Generic;

public enum DraftResultType
{
    Ban,
    Pick
}

public sealed class RuleManager
{
    public event Action<IDraftPhase> OnPhaseChanged;
    public event Action<DraftSide, string, DraftResultType> OnActionSubmitted;
    public event Action OnDraftCompleted;

    private readonly DraftFormatSO format;
    private readonly ITurnOrderRule banTurnOrder;
    private readonly ITurnOrderRule pickTurnOrder;
    private readonly HashSet<string> usedCharacterIds = new();

    private List<IDraftPhase> phases;
    private int currentPhaseIndex = -1;

    /// <summary>
    /// banTurnOrder/pickTurnOrder를 명시적으로 넘기면 그걸 우선 사용하고,
    /// null로 두면 format(DraftFormatSO)에 설정된 패턴(banOrderPattern/pickOrderPattern)을
    /// 그대로 따른다. 그 패턴마저 비어 있으면 AlternatingTurnOrderRule로 폴백한다.
    /// 즉 "코드로 강제 지정" > "SO 인스펙터 값" > "기본 교대" 순의 우선순위.
    /// </summary>
    public RuleManager(DraftFormatSO format, ITurnOrderRule banTurnOrder = null, ITurnOrderRule pickTurnOrder = null)
    {
        if (!format)
            throw new ArgumentNullException(nameof(format));

        this.format = format;
        this.banTurnOrder = banTurnOrder ?? format.BuildBanTurnOrder();
        this.pickTurnOrder = pickTurnOrder ?? format.BuildPickTurnOrder();
    }

    private static List<IDraftPhase> BuildPhases(DraftFormatSO format, ITurnOrderRule banTurnOrder, ITurnOrderRule pickTurnOrder) => new()
    {
        new BanPhase(format.FirstBanSlots, format.SecondBanSlots, banTurnOrder),
        new PickPhase(format.FirstPickSlots, format.SecondPickSlots, pickTurnOrder)
    };

    public IDraftPhase CurrentPhase =>
        (currentPhaseIndex >= 0 && currentPhaseIndex < phases.Count) ? phases[currentPhaseIndex] : null;

    public bool IsDraftComplete => currentPhaseIndex >= phases.Count;

    public bool HasStarted => currentPhaseIndex >= 0;

    public void StartDraft()
    {
        phases = BuildPhases(format, banTurnOrder, pickTurnOrder);
        usedCharacterIds.Clear();
        currentPhaseIndex = 0;
        CurrentPhase.Enter();
        OnPhaseChanged?.Invoke(CurrentPhase);
    }

    public bool SubmitAction(DraftSide side, string characterId, out string error)
    {
        error = null;

        if (!HasStarted)
        {
            error = "드래프트가 아직 시작되지 않았습니다. StartDraft()를 먼저 호출하세요.";
            return false;
        }

        if (IsDraftComplete)
        {
            error = "드래프트가 이미 종료되었습니다.";
            return false;
        }

        if (usedCharacterIds.Contains(characterId))
        {
            error = $"'{characterId}'는 이미 밴/픽되어 다시 선택할 수 없습니다.";
            return false;
        }

        var phase = CurrentPhase;
        if (!phase.SubmitAction(side, characterId, out error))
            return false;

        usedCharacterIds.Add(characterId);

        var resultType = phase is BanPhase ? DraftResultType.Ban : DraftResultType.Pick;
        OnActionSubmitted?.Invoke(side, characterId, resultType);

        if (phase.IsComplete)
            AdvancePhase();

        return true;
    }

    public bool IsCharacterAvailable(string characterId) => !usedCharacterIds.Contains(characterId);

    private void AdvancePhase()
    {
        currentPhaseIndex++;

        if (IsDraftComplete)
        {
            OnDraftCompleted?.Invoke();
            return;
        }

        CurrentPhase.Enter();
        OnPhaseChanged?.Invoke(CurrentPhase);
    }
}