using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
















public class DraftPauseIndicator : MonoBehaviour
{
    [Header("Session")]
    [Tooltip("같은 씬에 미리 배치된 DraftSessionServer를 할당하면 Start()에서 자동 바인딩된다. " +
             "씬 전환으로 세션 오브젝트가 나중에 스폰되는 구조라면 Bind()를 직접 호출할 것.")]
    [SerializeField] private DraftSessionServer session;

    [Header("Pause Button")]
    [Tooltip("일시정지만 요청하는 버튼. overlayRoot 바깥(평상시 화면)에 배치되고, " +
             "일시정지 중에는 overlayRoot가 전체를 덮으므로 자연히 클릭이 막힌다. " +
             "관전자 등 권한 없는 클라이언트에게는 interactable=false로 비활성화된다(최종 검증은 서버).")]
    [SerializeField] private Button pauseButton;
    [SerializeField] private TMP_Text pauseButtonLabel;
    [SerializeField] private string pauseLabel = "일시정지";

    [Header("Resume Button")]
    [Tooltip("재개만 요청하는 버튼. overlayRoot의 자식으로 배치해서 오버레이보다 위 레이어에서 " +
             "클릭 가능하게 만든다.")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private TMP_Text resumeButtonLabel;
    [SerializeField] private string resumeLabel = "재개";

    [Header("Overlay")]
    [Tooltip("일시정지 중 밴픽판 위를 덮는 전체화면 반투명 패널. IsPaused.Value로만 켜고 끈다. " +
             "pauseButton과 resumeButton 사이의 레이어 전체를 덮어 pauseButton 클릭을 막는 역할도 겸한다.")]
    [SerializeField] private GameObject overlayRoot;
    [SerializeField] private TMP_Text overlayText;
    [SerializeField] private string overlayFormat = "일시정지됨";

    private void Start()
    {
        if (pauseButton != null) pauseButton.onClick.AddListener(HandlePauseButtonClicked);
        if (resumeButton != null) resumeButton.onClick.AddListener(HandleResumeButtonClicked);

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
        if (pauseButton != null) pauseButton.onClick.RemoveListener(HandlePauseButtonClicked);
        if (resumeButton != null) resumeButton.onClick.RemoveListener(HandleResumeButtonClicked);
        Unbind();
    }

    public void Bind(DraftSessionServer newSession)
    {
        if (newSession == null)
        {
            Debug.LogError($"[{nameof(DraftPauseIndicator)}] Bind에 null 세션이 전달되었습니다.");
            return;
        }

        DraftSessionServer.OnSessionReady -= Bind; 

        if (session != null) Unbind();
        session = newSession;

        session.IsPaused.OnValueChanged += HandlePausedChanged;
        session.State.OnValueChanged += HandleStateChanged;

        Render();
    }

    public void Unbind()
    {
        if (session == null) return;

        session.IsPaused.OnValueChanged -= HandlePausedChanged;
        session.State.OnValueChanged -= HandleStateChanged;

        session = null;
    }

    private void HandlePausedChanged(bool previous, bool current) => Render();
    private void HandleStateChanged(DraftSessionState previous, DraftSessionState current) => Render();

    private void HandlePauseButtonClicked()
    {
        if (session == null) return;

        
        
        session.RequestPauseServerRpc(true);
    }

    private void HandleResumeButtonClicked()
    {
        if (session == null) return;

        
        
        session.RequestPauseServerRpc(false);
    }

    private void Render()
    {
        if (session == null)
        {
            SetPauseButtonVisible(false);
            SetResumeButtonVisible(false);
            SetOverlayVisible(false);
            return;
        }

        bool isPauseUsableState = session.State.Value == DraftSessionState.Loading ||
                                   session.State.Value == DraftSessionState.InProgress;
        bool isPaused = session.IsPaused.Value;
        bool canToggle = IsLocalClientHostOrParticipant();

        
        
        SetPauseButtonVisible(isPauseUsableState && !isPaused);
        SetResumeButtonVisible(isPauseUsableState && isPaused);

        if (pauseButton) pauseButton.interactable = canToggle;
        if (pauseButtonLabel) pauseButtonLabel.text = pauseLabel;

        if (resumeButton) resumeButton.interactable = canToggle;
        if (resumeButtonLabel) resumeButtonLabel.text = resumeLabel;

        SetOverlayVisible(isPauseUsableState && isPaused);
        if (overlayText) overlayText.text = overlayFormat;
    }

    
    private bool IsLocalClientHostOrParticipant()
    {
        var localId = NetworkManager.Singleton != null ? NetworkManager.Singleton.LocalClientId : ulong.MaxValue;
        return localId == NetworkManager.ServerClientId ||
               localId == session.FirstSideClientId.Value ||
               localId == session.SecondSideClientId.Value;
    }

    private void SetPauseButtonVisible(bool visible)
    {
        if (pauseButton) pauseButton.gameObject.SetActive(visible);
    }

    private void SetResumeButtonVisible(bool visible)
    {
        if (resumeButton) resumeButton.gameObject.SetActive(visible);
    }

    private void SetOverlayVisible(bool visible)
    {
        if (overlayRoot) overlayRoot.SetActive(visible);
    }
}
