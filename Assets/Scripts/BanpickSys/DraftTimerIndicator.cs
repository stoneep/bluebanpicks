using TMPro;
using UnityEngine;

/// <summary>
/// DraftSessionServer의 두 가지 타이머를 하나의 텍스트로 보여주는 뷰.
///
///  1) Loading 상태: PreDraftSecondsRemaining - 밴픽씬 로드 완료 직후 "혹시 모를" UI/에셋
///     로딩 지연을 대비해 실제 밴/픽 시작 전 대기하는 시간(기본 15초).
///  2) InProgress 상태: TurnSecondsRemaining - 위 대기가 끝나 드래프트가 자동으로 시작된 뒤,
///     각 밴/픽 턴마다 주어지는 제한 시간. 0이 되면 서버가 자동으로 대신 선택한다.
///
/// DraftBoardController와 달리 RuleManager 진행 상태(누구 차례인지 등)는 전혀 다루지 않고,
/// DraftSessionServer의 NetworkVariable만 직접 구독하는 얇은 View라서 DraftBoardController
/// 유무와 무관하게 동작한다 (DraftTurnIndicator와 나란히 붙여 써도 된다).
/// </summary>
public sealed class DraftTimerIndicator : MonoBehaviour
{
    [Header("Session")]
    [Tooltip("같은 씬에 미리 배치된 DraftSessionServer를 할당하면 Start()에서 자동 바인딩된다. " +
             "씬 전환으로 세션 오브젝트가 나중에 스폰되는 구조라면 Bind()를 직접 호출할 것.")]
    [SerializeField] private DraftSessionServer session;

    [Header("View")]
    [Tooltip("타이머를 아예 감출 때 통째로 꺼둘 루트. 비워두면 timerText 오브젝트 자체를 껐다 켠다.")]
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private string preDraftFormat = "잠시 후 밴픽이 시작됩니다 ({0}초)";
    [SerializeField] private string turnFormat = "남은 시간 {0}초";

    private void Start()
    {
        if (session != null)
        {
            Bind(session);
        }
        else if (DraftSessionServer.Instance != null)
        {
            // 씬 전환 이전에 이미 스폰되어 살아있는 세션을 그대로 찾아 바인딩.
            Bind(DraftSessionServer.Instance);
        }
        else
        {
            // 극히 드문 타이밍(이 오브젝트의 Start가 세션 스폰보다 먼저 실행되는 경우)에 대한 안전망.
            DraftSessionServer.OnSessionReady += Bind;
        }
    }

    private void OnDestroy()
    {
        DraftSessionServer.OnSessionReady -= Bind;
        Unbind();
    }

    public void Bind(DraftSessionServer newSession)
    {
        if (newSession == null)
        {
            Debug.LogError($"[{nameof(DraftTimerIndicator)}] Bind에 null 세션이 전달되었습니다.");
            return;
        }

        DraftSessionServer.OnSessionReady -= Bind; // Start()의 안전망 구독이었다면 여기서 정리

        if (session != null) Unbind();
        session = newSession;

        session.State.OnValueChanged += HandleStateChanged;
        session.PreDraftSecondsRemaining.OnValueChanged += HandlePreDraftSecondsChanged;
        session.TurnSecondsRemaining.OnValueChanged += HandleTurnSecondsChanged;

        Render();
    }

    public void Unbind()
    {
        if (session == null) return;

        session.State.OnValueChanged -= HandleStateChanged;
        session.PreDraftSecondsRemaining.OnValueChanged -= HandlePreDraftSecondsChanged;
        session.TurnSecondsRemaining.OnValueChanged -= HandleTurnSecondsChanged;

        session = null;
    }

    // ==================== 세션 이벤트 핸들러 ====================
    // NetworkVariable.OnValueChanged는 "값이 실제로 바뀔 때만" 오므로, 상태 전환/매 초 갱신
    // 어느 쪽이 오든 항상 Render()로 현재 상태를 다시 계산해서 그린다.

    private void HandleStateChanged(DraftSessionState previous, DraftSessionState current) => Render();
    private void HandlePreDraftSecondsChanged(float previous, float current) => Render();
    private void HandleTurnSecondsChanged(float previous, float current) => Render();

    private void Render()
    {
        if (session == null)
        {
            SetVisible(false);
            return;
        }

        switch (session.State.Value)
        {
            case DraftSessionState.Loading:
                SetVisible(true);
                if (timerText)
                    timerText.text = string.Format(preDraftFormat, Mathf.CeilToInt(session.PreDraftSecondsRemaining.Value));
                break;

            case DraftSessionState.InProgress:
                // turnTimeLimitSeconds가 0 이하로 설정되어 턴 타이머를 안 쓰는 구성이면 0이 유지되므로 숨긴다.
                bool hasTurnTimer = session.TurnSecondsRemaining.Value > 0f;
                SetVisible(hasTurnTimer);
                if (hasTurnTimer && timerText)
                    timerText.text = string.Format(turnFormat, Mathf.CeilToInt(session.TurnSecondsRemaining.Value));
                break;

            default: // Lobby, Completed
                SetVisible(false);
                break;
        }
    }

    private void SetVisible(bool visible)
    {
        if (root) root.SetActive(visible);
        else if (timerText) timerText.gameObject.SetActive(visible);
    }
}
