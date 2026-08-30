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

    [Tooltip("씬 전환 직후 ~ State가 Loading으로 동기화되기 전(Lobby로 stale하게 남아있는 짧은 구간)에 " +
             "보여줄 문구. 이 구간엔 PreDraftSecondsRemaining이 아직 유효한 값이 아니므로 초를 표시하지 않는다.")]
    [SerializeField] private string waitingFormat = "잠시 후 밴픽이 시작됩니다...";

    // 커튼은 "확인되기 전까지는 가리고 있어야" 하므로, Bind 이전 첫 프레임부터 이미 닫혀 있게 한다.
    protected override bool DefaultVisibleBeforeBind => true;

    protected override void OnBound(DraftSessionServer boundSession) =>
        boundSession.PreDraftSecondsRemaining.OnValueChanged += HandleSecondsChanged;

    protected override void OnUnbound(DraftSessionServer unboundSession) =>
        unboundSession.PreDraftSecondsRemaining.OnValueChanged -= HandleSecondsChanged;

    private void HandleSecondsChanged(float previous, float current) => Render();

    // Loading뿐 아니라 Lobby도 "아직 Loading으로 동기화되기 전"인 stale 구간으로 보고 계속 덮는다.
    // (이 컴포넌트는 드래프트 씬에서만 쓰이므로, 여기서 관측되는 Lobby는 실제 대기실이 아니라
    //  전환 직후의 동기화 지연일 뿐이다.)
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