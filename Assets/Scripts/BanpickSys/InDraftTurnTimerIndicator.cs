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
        // turnTimeLimitSeconds가 0 이하로 설정되어 턴 타이머를 안 쓰는 구성이면 0이 유지되므로 둘 다 숨긴다.
        bool hasTurnTimer = session.TurnSecondsRemaining.Value > 0f;

        SetVisible(hasTurnTimer);                          // timerText(또는 root) 담당
        if (turnText) turnText.gameObject.SetActive(hasTurnTimer); // turnText는 별도로 직접 토글

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

        // 관전자(호스트) 등 배정 안 된 클라이언트
        return currentSide == DraftSide.First ? "선공 턴" : "후공 턴";
    }
}