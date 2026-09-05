using UnityEngine;

public sealed class PlayerPanelIndicator : DraftTimerIndicatorBase
{
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

    protected override bool IsActiveState(DraftSessionState state) =>
        System.Array.IndexOf(activeStates, state) >= 0 && session.LocalSide != null;

    protected override void RenderContent() => SetVisible(true);
}