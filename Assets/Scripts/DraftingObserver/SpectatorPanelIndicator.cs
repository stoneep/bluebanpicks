using UnityEngine;

/// <summary>
/// 관전자(LocalSide == null, 호스트 포함) 전용 패널.
/// activeStates에 포함된 세션 상태 + 관전자 조건을 동시에 만족할 때만 root를 켠다.
///
/// 이 컨트롤러 자체는 켜고 끄는 역할만 하고, root 아래에 배치되는 실제 UI 요소들은
/// 각자 필요하면 스스로 DraftSessionServer.Instance / OnSessionReady를 구독해서
/// 그리면 된다 (DraftPauseIndicator, PreDraftTimerIndicator와 동일한 방식).
/// 즉 관전자용 UI를 여러 개 추가하고 싶을 때, 이 스크립트를 건드릴 필요 없이
/// root 하위에 컴포넌트만 추가하면 된다.
/// </summary>
public sealed class SpectatorPanelIndicator : DraftTimerIndicatorBase
{
    [Header("표시할 상태")]
    [Tooltip("이 목록에 포함된 상태 + 관전자일 때만 root가 켜진다.")]
    [SerializeField] private DraftSessionState[] activeStates =
        { DraftSessionState.Loading, DraftSessionState.InProgress };

    protected override bool DefaultVisibleBeforeBind => false;

    protected override void OnBound(DraftSessionServer boundSession)
    {
        boundSession.FirstSideClientId.OnValueChanged += HandleSideChanged;
        boundSession.SecondSideClientId.OnValueChanged += HandleSideChanged;
    }

    protected override void OnUnbound(DraftSessionServer unboundSession)
    {
        unboundSession.FirstSideClientId.OnValueChanged -= HandleSideChanged;
        unboundSession.SecondSideClientId.OnValueChanged -= HandleSideChanged;
    }

    private void HandleSideChanged(ulong previous, ulong current) => Render();

    // state 자체 조건 + 관전자 조건을 여기서 같이 체크.
    // Render()가 호출되는 시점엔 session이 이미 세팅돼 있으므로 안전하게 참조 가능.
    protected override bool IsActiveState(DraftSessionState state) =>
        System.Array.IndexOf(activeStates, state) >= 0 && session.LocalSide == null;

    protected override void RenderContent() => SetVisible(true); // 텍스트 없이 root만 켜면 됨
}