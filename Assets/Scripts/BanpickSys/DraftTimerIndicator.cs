using TMPro;
using UnityEngine;
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
            Bind(DraftSessionServer.Instance);
        }
        else
        {
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

        DraftSessionServer.OnSessionReady -= Bind; 

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
                
                bool hasTurnTimer = session.TurnSecondsRemaining.Value > 0f;
                SetVisible(hasTurnTimer);
                if (hasTurnTimer && timerText)
                    timerText.text = string.Format(turnFormat, Mathf.CeilToInt(session.TurnSecondsRemaining.Value));
                break;

            default: 
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
