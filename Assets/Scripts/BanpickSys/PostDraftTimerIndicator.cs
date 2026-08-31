using UnityEngine;

/// <summary>
/// 3단계 - 밴픽 종료 후 타이머.
///
/// DraftSessionServer.PostDraftDisplaySeconds(대기실에서 설정 가능)가 0보다 크면
/// 서버 권위 카운트다운(PostDraftSecondsRemaining, 모든 클라이언트 동일)을 그대로 표시한다.
///
/// 0 이하로 설정돼 있으면(기본값이 아니라 명시적으로 끈 경우) 기존 방식대로,
/// Completed로 전환된 "로컬 시점"부터 Time.time으로 경과 시간을 직접 세어 보여준다.
/// 이 경우 모든 클라이언트가 각자 계산하므로 프레임 단위까지 완전히 일치하진 않지만,
/// "종료됐다"는 연출/안내용으로는 충분하다.
/// </summary>
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

        // 이미 Completed 상태인 세션에 뒤늦게 바인딩되는 경우(late-join 등)를 대비.
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
        // 카운트다운을 쓰지 않는 설정(PostDraftDisplaySeconds <= 0, 서버가 PostDraftSecondsRemaining을
        // 계속 0으로 둠)일 때만 로컬 경과 시간을 매 프레임 세어 갱신한다. 카운트다운을 쓸 때는
        // 서버 값 변경(OnValueChanged -> Render)만으로 충분하다.
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
    
    /// <summary>
    /// 초 단위 float를 "mm:ss" 문자열로 바꾼다.
    /// showHours가 켜져 있으면 "hh:mm:ss"까지 확장된다.
    /// </summary>
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
