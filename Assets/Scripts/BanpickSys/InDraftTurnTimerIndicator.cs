using TMPro;
using UnityEngine;

public sealed class InDraftTurnTimerIndicator : DraftTimerIndicatorBase
{
    [Header("In-Draft")]
    [Tooltip("턴 안내 텍스트 (내 턴 / 상대 턴 / 선공 턴 / 후공 턴). timerText와 별도 오브젝트.")]
    [SerializeField] private TMP_Text turnText;
    [SerializeField] private string secondsFormat = "남은 시간 {0}초";

    protected override void OnBound(DraftSessionServer boundSession)
    {
        boundSession.TurnSecondsRemaining.OnValueChanged += HandleSecondsChanged;
        boundSession.CurrentSide.OnValueChanged += HandleSideChanged;
    }

    protected override void OnUnbound(DraftSessionServer unboundSession)
    {
        unboundSession.TurnSecondsRemaining.OnValueChanged -= HandleSecondsChanged;
        unboundSession.CurrentSide.OnValueChanged -= HandleSideChanged;
    }

    private void HandleSecondsChanged(float previous, float current) => Render();
    private void HandleSideChanged(DraftSide previous, DraftSide current) => Render();

    protected override bool IsActiveState(DraftSessionState state) => state == DraftSessionState.InProgress;

    protected override void RenderContent()
    {
        
        bool hasTurnTimer = session.TurnSecondsRemaining.Value > 0f;

        SetVisible(hasTurnTimer);                          
        if (turnText) turnText.gameObject.SetActive(hasTurnTimer); 

        if (!hasTurnTimer) return;

        if (timerText) timerText.text = string.Format(secondsFormat, Mathf.CeilToInt(session.TurnSecondsRemaining.Value));
        if (turnText) turnText.text = ResolveTurnLabel();
    }

    private string ResolveTurnLabel()
    {
        var localSide = session.LocalSide;
        var currentSide = session.CurrentSide.Value;

        if (localSide.HasValue)
            return currentSide == localSide.Value ? "내 턴" : "상대 턴";

        
        return currentSide == DraftSide.First ? "선공 턴" : "후공 턴";
    }
}