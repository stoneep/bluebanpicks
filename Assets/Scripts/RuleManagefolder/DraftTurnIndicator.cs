using TMPro;
using UnityEngine;

public sealed class DraftTurnIndicator : MonoBehaviour
{
    [SerializeField] private DraftBoardController draftBoardController;
    [SerializeField] private TMP_Text turnText;

    private string currentPhaseName;

    private void OnEnable()
    {
        if (!draftBoardController) return;

        draftBoardController.OnPhaseChanged += HandlePhaseChanged;
        draftBoardController.OnTurnChanged += HandleTurnChanged;
        draftBoardController.OnDraftCompleted += HandleDraftCompleted;
        
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
