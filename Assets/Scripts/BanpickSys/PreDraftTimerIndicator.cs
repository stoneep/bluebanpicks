using UnityEngine;

/// <summary>
/// 1단계 - 밴픽 준비 타이머.
///
/// 밴픽씬 로드 완료 직후, "혹시 모를" UI/에셋 로딩 지연을 대비해 실제 밴/픽 시작 전
/// 대기하는 시간(DraftSessionServer.PreDraftSecondsRemaining, 기본 15초)을 보여준다.
/// State가 Loading일 때만 활성화된다.
/// </summary>
public sealed class PreDraftTimerIndicator : DraftTimerIndicatorBase
{
    [Header("Pre-Draft")]
    [SerializeField] private string format = "잠시 후 밴픽이 시작됩니다 ({0}초)";

    protected override void OnBound(DraftSessionServer boundSession) =>
        boundSession.PreDraftSecondsRemaining.OnValueChanged += HandleSecondsChanged;

    protected override void OnUnbound(DraftSessionServer unboundSession) =>
        unboundSession.PreDraftSecondsRemaining.OnValueChanged -= HandleSecondsChanged;

    private void HandleSecondsChanged(float previous, float current) => Render();

    protected override bool IsActiveState(DraftSessionState state) => state == DraftSessionState.Loading;

    protected override void RenderContent()
    {
        SetVisible(true);
        if (timerText)
            timerText.text = string.Format(format, Mathf.CeilToInt(session.PreDraftSecondsRemaining.Value));
    }
}