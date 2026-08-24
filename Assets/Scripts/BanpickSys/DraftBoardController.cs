using System;
using UnityEngine;

/// <summary>
/// 선공 선택픽(6) / 선공 밴픽(5) / 후공 밴픽(5) / 후공 선택픽(6)
/// 4개의 PickSlotBar를 DraftFormatSO 값으로 초기화하고,
/// RuleManager(순수 C# 로직)가 발생시키는 이벤트를 구독해서
/// "누가 어떤 캐릭터를 밴/픽했는지"를 해당 PickSlotBar에 그대로 반영한다.
///
/// 이 클래스는 뷰(View) 역할만 한다:
/// - 캐릭터 선택 자체(누가 클릭했는지 등)는 CharacterListPanelController 같은
///   곳에서 처리한 뒤 SubmitCharacter(characterId)만 호출해주면 됨.
/// - "지금 누구 차례인가", "이 캐릭터를 골라도 되는가" 같은 규칙 판단은
///   전부 RuleManager에 위임하고, 이 클래스는 결과를 화면에 그리기만 한다.
/// </summary>
public class DraftBoardController : MonoBehaviour
{
    [Header("Format")]
    [SerializeField] private DraftFormatSO format;

    [Header("Bars")]
    [SerializeField] private PickSlotBar firstPickBar;   // 선공 선택픽 1x6
    [SerializeField] private PickSlotBar firstBanBar;    // 선공 밴픽   1x5
    [SerializeField] private PickSlotBar secondBanBar;   // 후공 밴픽   1x5
    [SerializeField] private PickSlotBar secondPickBar;  // 후공 선택픽 1x6

    private RuleManager ruleManager;

    // ==================== 외부 구독용 이벤트 ====================
    // 턴 표시 UI("선공 밴 차례입니다" 등)나 연출 트리거가 필요하면
    // RuleManager를 직접 참조하지 않고 이 이벤트들만 구독하면 됨.
    public event Action<DraftSide> OnTurnChanged;
    public event Action<string> OnPhaseChanged;          // "Ban" / "Pick"
    public event Action<DraftSide, string, DraftResultType> OnActionSubmitted;
    public event Action OnDraftCompleted;

    public bool IsDraftComplete => ruleManager != null && ruleManager.IsDraftComplete;
    public DraftSide? CurrentSide => ruleManager?.CurrentPhase?.CurrentSide;
    public string CurrentPhaseName => ruleManager?.CurrentPhase?.PhaseName;

    private void Awake()
    {
        if (!format)
        {
            Debug.LogError($"[{nameof(DraftBoardController)}] DraftFormatSO가 할당되지 않았습니다.");
            return;
        }

        // firstPickBar.ApplyConfig(PickSlotBarConfig.Of(format.FirstPickSlots));
        // firstBanBar.ApplyConfig(PickSlotBarConfig.Of(format.FirstBanSlots));
        // secondBanBar.ApplyConfig(PickSlotBarConfig.Of(format.SecondBanSlots));
        // secondPickBar.ApplyConfig(PickSlotBarConfig.Of(format.SecondPickSlots));

        ruleManager = new RuleManager(format);
        ruleManager.OnActionSubmitted += HandleActionSubmitted;
        ruleManager.OnPhaseChanged += HandlePhaseChanged;
        ruleManager.OnDraftCompleted += HandleDraftCompleted;
    }

    private void Start()
    {
        StartDraft();
    }

    private void OnDestroy()
    {
        if (ruleManager == null) return;
        ruleManager.OnActionSubmitted -= HandleActionSubmitted;
        ruleManager.OnPhaseChanged -= HandlePhaseChanged;
        ruleManager.OnDraftCompleted -= HandleDraftCompleted;
    }

    // ==================== 진행 API ====================

    /// <summary>드래프트를 (재)시작한다. 보드도 함께 초기화된다.</summary>
    public void StartDraft()
    {
        ResetBoard();
        ruleManager.StartDraft();
    }

    /// <summary>
    /// 현재 차례인 진영이 characterId를 밴/픽한다.
    /// 차례가 아니거나 이미 사용된 캐릭터면 false와 함께 사유가 error로 반환된다.
    /// 캐릭터 선택 UI(리스트 클릭 등)는 이 메서드 하나만 호출하면 된다.
    /// </summary>
    public bool SubmitCharacter(string characterId, out string error)
    {
        error = null;

        if (ruleManager == null || ruleManager.CurrentPhase == null)
        {
            error = "드래프트가 초기화되지 않았습니다.";
            return false;
        }

        var side = ruleManager.CurrentPhase.CurrentSide;
        return ruleManager.SubmitAction(side, characterId, out error);
    }

    /// <summary>이미 밴/픽되어 더 이상 선택할 수 없는 캐릭터인지 (리스트 버튼 비활성화 등에 사용)</summary>
    public bool IsCharacterAvailable(string characterId) =>
        ruleManager != null && ruleManager.IsCharacterAvailable(characterId);

    public void ResetBoard()
    {
        firstPickBar.ClearAll();
        firstBanBar.ClearAll();
        secondBanBar.ClearAll();
        secondPickBar.ClearAll();
    }

    // ==================== RuleManager 이벤트 핸들러 ====================

    private void HandleActionSubmitted(DraftSide side, string characterId, DraftResultType type)
    {
        var bar = ResolveBar(side, type);
        if (!bar)
        {
            Debug.LogError($"[{nameof(DraftBoardController)}] {side}/{type}에 대응하는 PickSlotBar가 없습니다.");
            return;
        }

        // 이벤트는 phase 진행(다음 페이즈 전환) 직전에 발생하므로,
        // 지금 막 선택이 반영된 슬롯 개수(count-1)가 곧 이번에 채울 인덱스다.
        var phase = ruleManager.CurrentPhase;
        int index = phase.GetSelections(side).Count - 1;
        bar.SetCharacter(index, characterId);

        OnActionSubmitted?.Invoke(side, characterId, type);

        // 같은 페이즈 안에서 다음 차례로 넘어간 경우(예: 선공밴 -> 후공밴)는
        // RuleManager.OnPhaseChanged가 따로 발행되지 않으므로 여기서 턴 변경을 알려준다.
        // 페이즈 자체가 끝난 경우엔 뒤이어 HandlePhaseChanged(또는 HandleDraftCompleted)가 처리하므로 생략.
        if (!phase.IsComplete)
        {
            OnTurnChanged?.Invoke(phase.CurrentSide);
        }
    }

    private void HandlePhaseChanged(IDraftPhase phase)
    {
        OnPhaseChanged?.Invoke(phase.PhaseName);
        OnTurnChanged?.Invoke(phase.CurrentSide);
    }

    private void HandleDraftCompleted()
    {
        OnDraftCompleted?.Invoke();
    }

    private PickSlotBar ResolveBar(DraftSide side, DraftResultType type)
    {
        return (side, type) switch
        {
            (DraftSide.First, DraftResultType.Ban) => firstBanBar,
            (DraftSide.First, DraftResultType.Pick) => firstPickBar,
            (DraftSide.Second, DraftResultType.Ban) => secondBanBar,
            (DraftSide.Second, DraftResultType.Pick) => secondPickBar,
            _ => null
        };
    }
}
