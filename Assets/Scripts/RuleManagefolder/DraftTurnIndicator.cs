using TMPro;
using UnityEngine;

/// <summary>
/// DraftBoardController가 발행하는 이벤트(OnPhaseChanged/OnTurnChanged/OnDraftCompleted)를
/// 구독해서 "선공 밴 차례입니다" 같은 안내 텍스트를 갱신하는 얇은 뷰.
///
/// RuleManager나 페이즈 내부 구조는 전혀 모르고, DraftBoardController가 이미
/// 정리해서 내보내는 이벤트만 받는다 - 로직/뷰 분리를 유지하기 위함.
/// </summary>
public sealed class DraftTurnIndicator : MonoBehaviour
{
    [SerializeField] private DraftBoardController draftBoardController;
    [SerializeField] private TMP_Text turnText;

    private string currentPhaseName; // "Ban" / "Pick" - OnTurnChanged가 먼저 와도 최신 페이즈명을 유지하기 위해 캐시

    private void OnEnable()
    {
        if (!draftBoardController) return;

        draftBoardController.OnPhaseChanged += HandlePhaseChanged;
        draftBoardController.OnTurnChanged += HandleTurnChanged;
        draftBoardController.OnDraftCompleted += HandleDraftCompleted;

        // 이미 진행 중인 드래프트에 뒤늦게 붙는 경우(씬 재진입 등) 현재 상태로 즉시 갱신
        if (draftBoardController.CurrentPhaseName != null)
        {
            currentPhaseName = draftBoardController.CurrentPhaseName;
            Render(currentPhaseName, draftBoardController.CurrentSide);
        }
    }

    private void OnDisable()
    {
        if (!draftBoardController) return;

        draftBoardController.OnPhaseChanged -= HandlePhaseChanged;
        draftBoardController.OnTurnChanged -= HandleTurnChanged;
        draftBoardController.OnDraftCompleted -= HandleDraftCompleted;
    }

    private void HandlePhaseChanged(string phaseName)
    {
        currentPhaseName = phaseName;
    }

    private void HandleTurnChanged(DraftSide side)
    {
        // OnPhaseChanged -> OnTurnChanged 순서로 DraftBoardController가 발행하므로
        // 이 시점엔 currentPhaseName이 이미 최신 값으로 갱신돼 있다.
        Render(currentPhaseName, side);
    }

    private void HandleDraftCompleted()
    {
        if (turnText) turnText.text = "드래프트 종료";
    }

    private void Render(string phaseName, DraftSide? side)
    {
        if (!turnText || !side.HasValue) return;

        string sideLabel = side.Value == DraftSide.First ? "선공" : "후공";
        string phaseLabel = phaseName == "Ban" ? "밴" : "픽";

        turnText.text = $"{sideLabel} {phaseLabel} 차례";
    }
}
