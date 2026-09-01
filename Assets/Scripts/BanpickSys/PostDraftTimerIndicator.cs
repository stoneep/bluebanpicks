using UnityEngine;

public sealed class PostDraftTimerIndicator : DraftTimerIndicatorBase
{
    [Header("Post-Draft")] 
    [Tooltip("PostDraftDisplaySeconds(대기실에서 설정한 값)가 0보다 클 때 쓰는 카운트다운 문구. {0}=남은 시간(mm:ss).")] 
    [SerializeField] private string countdownFormat = "밴픽 종료 ({0} 후)";

    [Tooltip("PostDraftDisplaySeconds가 0 이하일 때(카운트다운 미사용) 쓰는 경과 시간 문구. {0}=경과 시간(mm:ss).")]
    [SerializeField] private string elapsedFormat = "밴픽 종료 ({0} 경과)";
    
    [Header("Timer Format")] 
    [Tooltip("시(hh) 단위까지 표시할지 여부. 대부분의 밴픽 종료 타이머는 5분 내외라 꺼두는 게 자연스럽다.")] 
    [SerializeField] private bool showHours = false;
    
    private float completedAtTime = -1f;

    protected override void OnBound(DraftSessionServer boundSession)
    {
        boundSession.PostDraftSecondsRemaining.OnValueChanged += HandleSecondsChanged;

        
        if (boundSession.State.Value == DraftSessionState.Completed)
            completedAtTime = Time.time;
    }

    protected override void OnUnbound(DraftSessionServer unboundSession) =>
        unboundSession.PostDraftSecondsRemaining.OnValueChanged -= HandleSecondsChanged;

    protected override void OnStateChanged(DraftSessionState previous, DraftSessionState current)
    {
        if (current == DraftSessionState.Completed)
            completedAtTime = Time.time;
    }

    private void HandleSecondsChanged(float previous, float current) => Render();

    protected override bool IsActiveState(DraftSessionState state) => state == DraftSessionState.Completed;

    protected override void RenderContent()
    {
        if (completedAtTime < 0f) completedAtTime = Time.time;
        SetVisible(true);
        UpdateText();
    }

    private void Update()
    {
        
        
        
        if (session != null && session.State.Value == DraftSessionState.Completed &&
            session.PostDraftDisplaySeconds.Value <= 0f)
        {
            UpdateText();
        }
    }

    private void UpdateText()
    {
        if (!timerText || session == null) return;

        if (session.PostDraftDisplaySeconds.Value > 0f)
        {
            timerText.text = string.Format(countdownFormat, FormatTime(session.PostDraftSecondsRemaining.Value));
            return;
        }

        if (completedAtTime < 0f) return;
        float elapsed = Mathf.Max(0f, Time.time - completedAtTime);
        timerText.text = string.Format(elapsedFormat, FormatTime(elapsed));
    }
    
    
    
    
    
    private string FormatTime(float seconds)
    {
        seconds = Mathf.Max(0f, seconds);

        int totalWhole = Mathf.FloorToInt(seconds);
        int hours = totalWhole / 3600;
        int minutes = (totalWhole % 3600) / 60;
        int secs = totalWhole % 60;

        return showHours
            ? $"{hours:00}:{minutes:00}:{secs:00}"
            : $"{minutes:00}:{secs:00}";
    }
}
