using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 드래프트 도중 "완전 정지"를 위한 보험용 UI.
///
/// 다른 타이머 뷰들(DraftTimerIndicatorBase 계열)은 "특정 DraftSessionState 하나일 때만
/// 보이는 텍스트"라는 단일 목적이라 그 베이스를 그대로 썼지만, 이 컴포넌트는
///  - Loading/InProgress 두 상태에 걸쳐 버튼이 떠 있어야 하고,
///  - 오버레이 표시는 State가 아니라 IsPaused 값 자체로 토글되고,
///  - 버튼 활성화 여부가 "로컬 클라이언트가 호스트/참가자인가"라는 별도 조건에 달려 있어서
/// 상속 구조를 억지로 맞추기보다 DraftBoardController 등과 같은 Bind/Unbind 관례만
/// 따르는 독립 컴포넌트로 분리했다.
///
/// 권한(호스트 또는 배정된 선공/후공 참가자만 토글 가능)은 최종적으로 서버(DraftSessionServer.
/// RequestPauseServerRpc)가 검증한다 - 여기서 버튼을 막아두는 건 UX상의 편의일 뿐,
/// 신뢰 경계는 항상 서버 쪽에 있다.
/// </summary>
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

        DraftSessionServer.OnSessionReady -= Bind; // Start()의 안전망 구독이었다면 여기서 정리

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

        // 이 버튼은 "일시정지 요청" 전용이다. 실제 반영은 서버 승인
        // (IsPaused.OnValueChanged) 이후에야 일어난다.
        session.RequestPauseServerRpc(true);
    }

    private void HandleResumeButtonClicked()
    {
        if (session == null) return;

        // 이 버튼은 "재개 요청" 전용이다. 실제 반영은 서버 승인
        // (IsPaused.OnValueChanged) 이후에야 일어난다.
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

        // 평상시엔 pauseButton만, 일시정지 중엔 resumeButton만 보인다.
        // resumeButton은 overlayRoot의 자식이라 오버레이가 켜져 있는 동안에만 실제로 노출/클릭 가능하다.
        SetPauseButtonVisible(isPauseUsableState && !isPaused);
        SetResumeButtonVisible(isPauseUsableState && isPaused);

        if (pauseButton) pauseButton.interactable = canToggle;
        if (pauseButtonLabel) pauseButtonLabel.text = pauseLabel;

        if (resumeButton) resumeButton.interactable = canToggle;
        if (resumeButtonLabel) resumeButtonLabel.text = resumeLabel;

        SetOverlayVisible(isPauseUsableState && isPaused);
        if (overlayText) overlayText.text = overlayFormat;
    }

    /// <summary>호스트(ServerClientId) 또는 배정된 선공/후공 참가자인지. 서버 쪽 최종 검증의 UX 미러링용.</summary>
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
