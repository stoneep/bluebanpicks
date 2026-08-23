using System;
using System.Collections.Generic;

/// <summary>
/// 밴/픽 결과 구분 (UI에서 로그/연출 분기용)
/// </summary>
public enum DraftResultType
{
    Ban,
    Pick
}

/// <summary>
/// 선공밴 → 후공밴 → 선공픽 → 후공픽 순서로 페이즈를 진행시키는 상태 머신.
///
/// - 페이즈 내부의 "누구 차례인가"는 IDraftPhase(BanPhase/PickPhase)가 책임진다.
/// - RuleManager는 (1) 페이즈를 순서대로 갈아끼우는 것과
///   (2) 이미 밴/픽된 캐릭터를 다시 고를 수 없게 막는 것(교차 페이즈 검증)을 책임진다.
/// - MonoBehaviour가 아니므로 유닛 테스트가 가능하고,
///   DraftBoardController 같은 뷰는 이 클래스의 이벤트를 구독해서 화면만 갱신하면 된다.
/// </summary>
public sealed class RuleManager
{
    public event Action<IDraftPhase> OnPhaseChanged;
    public event Action<DraftSide, string, DraftResultType> OnActionSubmitted;
    public event Action OnDraftCompleted;

    private readonly DraftFormatSO format;
    private readonly ITurnOrderRule turnOrder;
    private readonly HashSet<string> usedCharacterIds = new();

    private List<IDraftPhase> phases;
    private int currentPhaseIndex = -1;

    public RuleManager(DraftFormatSO format, ITurnOrderRule turnOrder = null)
    {
        if (!format)
            throw new ArgumentNullException(nameof(format));

        this.format = format;
        this.turnOrder = turnOrder ?? new AlternatingTurnOrderRule();
    }

    /// <summary>
    /// 선공밴→후공밴→선공픽→후공픽 순서로 진행할 페이즈들을 새로 만든다.
    /// StartDraft()마다 새로 호출하므로, 재대국(ResetBoard 등) 시에도
    /// 이전 게임의 선택 데이터가 남아있는 phase 인스턴스를 재사용하는 버그가 없다.
    /// </summary>
    private static List<IDraftPhase> BuildPhases(DraftFormatSO format, ITurnOrderRule turnOrder) => new()
    {
        new BanPhase(format.FirstBanSlots, format.SecondBanSlots, turnOrder),
        new PickPhase(format.FirstPickSlots, format.SecondPickSlots, turnOrder)
    };

    public IDraftPhase CurrentPhase =>
        (currentPhaseIndex >= 0 && currentPhaseIndex < phases.Count) ? phases[currentPhaseIndex] : null;

    public bool IsDraftComplete => currentPhaseIndex >= phases.Count;

    public bool HasStarted => currentPhaseIndex >= 0;

    /// <summary>
    /// 드래프트 시작(또는 재시작). 매번 페이즈를 새로 만들기 때문에
    /// 같은 RuleManager 인스턴스로 여러 판을 이어서 진행해도 이전 판의
    /// 밴/픽 기록이 남지 않는다.
    /// </summary>
    public void StartDraft()
    {
        phases = BuildPhases(format, turnOrder);
        usedCharacterIds.Clear();
        currentPhaseIndex = 0;
        CurrentPhase.Enter();
        OnPhaseChanged?.Invoke(CurrentPhase);
    }

    /// <summary>
    /// 현재 페이즈, 현재 차례인 진영에게 characterId로 행동(밴/픽)을 제출한다.
    /// 실패 시 error에 사유가 담기고 false를 반환한다 (예외를 던지지 않음 - 예: 잘못된 클릭 UI 대응).
    /// </summary>
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

    /// <summary>characterId가 아직 밴/픽되지 않아 선택 가능한지 (UI에서 버튼 비활성화 등에 사용)</summary>
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
