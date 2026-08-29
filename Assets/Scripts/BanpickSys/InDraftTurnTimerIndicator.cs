using UnityEngine;

/// <summary>
/// 2단계 - 밴픽 진행 중 턴 타이머.
///
/// 준비 대기가 끝나 드래프트가 자동으로 시작된 뒤, 각 밴/픽 턴마다 주어지는 제한 시간
/// (DraftSessionServer.TurnSecondsRemaining)을 보여준다. 0이 되면 서버가 자동으로 대신 선택한다.
/// State가 InProgress일 때만 활성화되며, 턴 타이머 자체를 쓰지 않는 구성(turnTimeLimitSeconds <= 0)이면
/// TurnSecondsRemaining이 0으로 유지되므로 자동으로 숨는다.
/// </summary>
public sealed class InDraftTurnTimerIndicator : DraftTimerIndicatorBase
{
    [Header("In-Draft")]
    [SerializeField] private string format = "남은 시간 {0}초";

    protected override void OnBound(DraftSessionServer boundSession) =>
        boundSession.TurnSecondsRemaining.OnValueChanged += HandleSecondsChanged;

    protected override void OnUnbound(DraftSessionServer unboundSession) =>
        unboundSession.TurnSecondsRemaining.OnValueChanged -= HandleSecondsChanged;

    private void HandleSecondsChanged(float previous, float current) => Render();

    protected override bool IsActiveState(DraftSessionState state) => state == DraftSessionState.InProgress;

    protected override void RenderContent()
    {
        // turnTimeLimitSeconds가 0 이하로 설정되어 턴 타이머를 안 쓰는 구성이면 0이 유지되므로 숨긴다.
        bool hasTurnTimer = session.TurnSecondsRemaining.Value > 0f;
        SetVisible(hasTurnTimer);
        if (hasTurnTimer && timerText)
            timerText.text = string.Format(format, Mathf.CeilToInt(session.TurnSecondsRemaining.Value));
    }
}