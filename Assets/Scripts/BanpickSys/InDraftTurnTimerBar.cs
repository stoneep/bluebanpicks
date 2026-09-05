using UnityEngine;

/// <summary>
/// InDraftTurnTimerIndicator와 같은 DraftSessionServer(TurnSecondsRemaining / TurnTimeLimitSeconds)를
/// 바라보면서, 턴 제한 시간에 맞춰 "좌우 양쪽에서 중앙으로" 대칭으로 줄어드는 바 UI를 그린다.
///
/// 핵심 아이디어: barRect의 anchorMin/anchorMax/pivot을 전부 (0.5, 0.5)로 고정해두면
/// anchoredPosition(중심 좌표)은 전혀 건드릴 필요가 없다. sizeDelta.x(너비)만 줄여도
/// 좌/우 변이 중심을 기준으로 똑같이 안쪽으로 들어오기 때문이다.
///
/// TurnSecondsRemaining은 NetworkVariable이라 값이 "초 단위로 올림(Ceil)된 값이 바뀔 때만" 갱신된다
/// (NetworkCountdown 참고). 그 값을 그대로 너비에 매핑하면 1초마다 계단식으로 뚝뚝 끊겨 보이므로,
/// 여기서는 Update()에서 매 프레임 로컬로 시간을 흘려보내 부드럽게 줄이고, 서버 값이 실제로 바뀔 때마다
/// 그 값으로 재동기화(스냅)해서 드리프트를 막는다.
/// </summary>
public sealed class InDraftTurnTimerBar : DraftTimerIndicatorBase
{
    [Header("Bar")]
    [Tooltip("좌우 대칭으로 줄어들 바 RectTransform. " +
             "반드시 anchorMin = anchorMax = pivot = (0.5, 0.5)로 설정해서 중앙에 고정할 것.")]
    [SerializeField] private RectTransform barRect;

    [Tooltip("턴이 시작된 시점(=시간이 가득 찼을 때)의 바 너비. " +
             "0 이하로 두면 Awake 시점의 barRect.sizeDelta.x를 그대로 가득 찬 너비로 사용한다.")]
    [SerializeField] private float fullWidth = 0f;

    [Tooltip("바 높이를 강제로 고정하고 싶을 때만 0 이상 값을 넣는다. 음수면 기존 높이를 그대로 둔다.")]
    [SerializeField] private float fixedHeight = -1f;

    // 로컬에서 매 프레임 감소시키는 "표시용" 남은 시간. 서버의 TurnSecondsRemaining과는 별개로,
    // 그 값이 바뀔 때마다 재동기화된다.
    private float displaySecondsRemaining;

    // 이번 턴의 총 제한 시간 스냅샷 (분모). 턴이 새로 시작될 때만 갱신한다.
    private float turnDurationSeconds;

    private bool isRunning;

    protected override void Awake()
    {
        base.Awake();

        if (barRect != null)
        {
            WarnIfNotCenterPivoted(barRect);

            if (fullWidth <= 0f)
                fullWidth = barRect.sizeDelta.x;
        }
    }

    protected override void OnBound(DraftSessionServer boundSession)
    {
        boundSession.TurnSecondsRemaining.OnValueChanged += HandleSecondsChanged;

        // 턴 중간에 바인딩되는 경우(재접속 등)를 대비해 현재 값으로 즉시 한 번 맞춰준다.
        HandleSecondsChanged(boundSession.TurnSecondsRemaining.Value, boundSession.TurnSecondsRemaining.Value);
    }

    protected override void OnUnbound(DraftSessionServer unboundSession)
    {
        unboundSession.TurnSecondsRemaining.OnValueChanged -= HandleSecondsChanged;
        isRunning = false;
    }

    protected override bool IsActiveState(DraftSessionState state) => state == DraftSessionState.InProgress;

    private void HandleSecondsChanged(float previous, float current)
    {
        // 값이 이전보다 "커졌다" = 새 턴이 시작되며 다시 가득 찼다는 뜻이므로 분모(듀레이션)를 다시 잡는다.
        // (일반적인 카운트다운 중에는 current가 계속 previous 이하로만 움직인다.)
        if (!isRunning || current > previous + 0.01f)
            turnDurationSeconds = Mathf.Max(current, session != null ? session.TurnTimeLimitSeconds.Value : current, 0.0001f);

        displaySecondsRemaining = current;
        isRunning = current > 0f;

        Render();
    }

    protected override void RenderContent()
    {
        bool hasTurnTimer = session.TurnSecondsRemaining.Value > 0f;

        SetVisible(hasTurnTimer);
        if (barRect) barRect.gameObject.SetActive(hasTurnTimer);
    }

    private void Update()
    {
        if (session == null || barRect == null || !isRunning) return;
        if (!IsActiveState(session.State.Value)) return;

        // 서버와 동일하게, 일시정지 중에는 이번 프레임 경과 시간을 버린다.
        if (!session.IsPaused.Value)
            displaySecondsRemaining = Mathf.Max(0f, displaySecondsRemaining - Time.deltaTime);

        float ratio = turnDurationSeconds > 0f ? Mathf.Clamp01(displaySecondsRemaining / turnDurationSeconds) : 0f;
        ApplyWidth(ratio);
    }

    private void ApplyWidth(float ratio)
    {
        Vector2 size = barRect.sizeDelta;
        size.x = fullWidth * ratio;
        if (fixedHeight >= 0f) size.y = fixedHeight;
        barRect.sizeDelta = size;

        // anchoredPosition은 절대 건드리지 않는다 - anchor/pivot이 (0.5,0.5)라면
        // sizeDelta.x만 바뀌어도 좌우가 중심 기준으로 대칭으로 줄어든다.
    }

    private static void WarnIfNotCenterPivoted(RectTransform rt)
    {
        bool centered =
            Approximately(rt.anchorMin, new Vector2(0.5f, 0.5f)) &&
            Approximately(rt.anchorMax, new Vector2(0.5f, 0.5f)) &&
            Approximately(rt.pivot, new Vector2(0.5f, 0.5f));

        if (!centered)
        {
            Debug.LogWarning(
                $"[{nameof(InDraftTurnTimerBar)}] '{rt.name}'의 anchorMin/anchorMax/pivot이 " +
                "(0.5, 0.5)로 고정되어 있지 않습니다. 이 상태에서는 sizeDelta.x만 줄여도 " +
                "좌우가 대칭으로 줄어들지 않고 한쪽으로 쏠려 보일 수 있습니다.", rt);
        }
    }

    private static bool Approximately(Vector2 a, Vector2 b) =>
        Mathf.Approximately(a.x, b.x) && Mathf.Approximately(a.y, b.y);
}
