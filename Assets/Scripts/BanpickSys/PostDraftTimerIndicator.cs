using UnityEngine;

/// <summary>
/// 3단계 - 밴픽 종료 후 타이머.
///
/// 주의: DraftSessionServer에는 Completed 상태를 위한 서버 권위 카운트다운 NetworkVariable이
/// 아직 없다(PreDraftSecondsRemaining/TurnSecondsRemaining과 달리). 그래서 이 컴포넌트는
/// Completed로 전환된 "로컬 시점"부터 Time.time으로 경과 시간을 직접 세어 보여준다.
/// 모든 클라이언트가 각자 계산하므로 프레임 단위까지 완전히 일치하진 않지만,
/// "종료됐다"는 연출/안내용으로는 충분하다.
///
/// 만약 "종료 후 N초 뒤 자동으로 로비로 복귀" 같은 서버 동기화 카운트다운(모두에게 정확히 같은 값)이
/// 필요해지면, DraftSessionServer 쪽에 PreDraftSecondsRemaining과 동일한 패턴으로
/// PostDraftSecondsRemaining NetworkVariable + 카운트다운 코루틴을 추가하는 것을 권장한다.
/// </summary>
public sealed class PostDraftTimerIndicator : DraftTimerIndicatorBase
{
    [Header("Post-Draft")]
    [SerializeField] private string format = "밴픽 종료 ({0}초 경과)";

    private float completedAtTime = -1f;

    protected override void OnBound(DraftSessionServer boundSession)
    {
        // 이미 Completed 상태인 세션에 뒤늦게 바인딩되는 경우(late-join 등)를 대비.
        if (boundSession.State.Value == DraftSessionState.Completed)
            completedAtTime = Time.time;
    }

    protected override void OnStateChanged(DraftSessionState previous, DraftSessionState current)
    {
        if (current == DraftSessionState.Completed)
            completedAtTime = Time.time;
    }

    protected override bool IsActiveState(DraftSessionState state) => state == DraftSessionState.Completed;

    protected override void RenderContent()
    {
        if (completedAtTime < 0f) completedAtTime = Time.time;
        SetVisible(true);
        UpdateText();
    }

    private void Update()
    {
        // 매 프레임 갱신이 필요한 유일한 값(로컬 경과 시간)이라 Update에서 직접 텍스트를 갱신한다.
        if (session != null && session.State.Value == DraftSessionState.Completed)
            UpdateText();
    }

    private void UpdateText()
    {
        if (!timerText || completedAtTime < 0f) return;
        int elapsed = Mathf.Max(0, Mathf.FloorToInt(Time.time - completedAtTime));
        timerText.text = string.Format(format, elapsed);
    }
}