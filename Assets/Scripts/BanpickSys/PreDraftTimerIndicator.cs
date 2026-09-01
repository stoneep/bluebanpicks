using UnityEngine;








public sealed class PreDraftTimerIndicator : DraftTimerIndicatorBase
{
    [Header("Pre-Draft")]
    [SerializeField] private string format = "잠시 후 밴픽이 시작됩니다 ({0}초)";

    [Tooltip("씬 전환 직후 ~ State가 Loading으로 동기화되기 전(Lobby로 stale하게 남아있는 짧은 구간)에 " +
             "보여줄 문구. 이 구간엔 PreDraftSecondsRemaining이 아직 유효한 값이 아니므로 초를 표시하지 않는다.")]
    [SerializeField] private string waitingFormat = "잠시 후 밴픽이 시작됩니다...";

    
    protected override bool DefaultVisibleBeforeBind => true;

    protected override void OnBound(DraftSessionServer boundSession) =>
        boundSession.PreDraftSecondsRemaining.OnValueChanged += HandleSecondsChanged;

    protected override void OnUnbound(DraftSessionServer unboundSession) =>
        unboundSession.PreDraftSecondsRemaining.OnValueChanged -= HandleSecondsChanged;

    private void HandleSecondsChanged(float previous, float current) => Render();

    
    
    
    protected override bool IsActiveState(DraftSessionState state) =>
        state == DraftSessionState.Loading || state == DraftSessionState.Lobby;

    protected override void RenderContent()
    {
        SetVisible(true);
        if (!timerText) return;

        timerText.text = session.State.Value == DraftSessionState.Loading
            ? string.Format(format, Mathf.CeilToInt(session.PreDraftSecondsRemaining.Value))
            : waitingFormat;
    }
}