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

    private readonly IDraftFormat format;
    private readonly HashSet<string> usedCharacterIds = new();

    private List<IDraftPhase> phases;
    private int currentPhaseIndex = -1;

    public RuleManager(IDraftFormat format)
    {
        if (format == null)
            throw new ArgumentNullException(nameof(format));
        if (format.Rounds == null || format.Rounds.Count == 0)
            throw new ArgumentException("최소 1개 이상의 라운드가 필요합니다.", nameof(format));

        this.format = format;
    }

    private static List<IDraftPhase> BuildPhases(IDraftFormat format)
    {
        var phases = new List<IDraftPhase>();

        foreach (var round in format.Rounds)
        {
            var banOrder = BuildTurnOrder(round.BanOrderPattern, round.StartingSide);
            var pickOrder = BuildTurnOrder(round.PickOrderPattern, round.StartingSide);

            phases.Add(new BanPhase(round.FirstBanSlots, round.SecondBanSlots, banOrder));
            phases.Add(new PickPhase(round.FirstPickSlots, round.SecondPickSlots, pickOrder));
        }

        return phases;
    }

    private static ITurnOrderRule BuildTurnOrder(string pattern, DraftSide startingSide) =>
        string.IsNullOrWhiteSpace(pattern)
            ? new AlternatingTurnOrderRule(startingSide)
            : SequenceTurnOrderRule.FromPattern(pattern);

    public IDraftPhase CurrentPhase =>
        (currentPhaseIndex >= 0 && currentPhaseIndex < phases.Count) ? phases[currentPhaseIndex] : null;

    public bool IsDraftComplete => currentPhaseIndex >= phases.Count;

    public bool HasStarted => currentPhaseIndex >= 0;

    public void StartDraft()
    {
        phases = BuildPhases(format);
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
