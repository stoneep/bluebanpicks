using TMPro;
using UnityEngine;

/// <summary>
/// DraftSessionServer의 상태/타이머를 구독해 텍스트로 보여주는 "타이머 뷰"들의 공통 베이스.
///
/// 기존 DraftTimerIndicator 하나가 Loading/InProgress를 switch로 다 처리하던 것을,
/// 준비(Pre) / 진행(In) / 종료(Post) 3단계로 나눠 각자 독립된 컴포넌트로 쓸 수 있게 분리했다.
/// 바인딩/구독 해제 같은 보일러플레이트는 여기서 한 번만 처리하고,
/// "어떤 상태에서 자신을 보여줄지"(IsActiveState)와 "그 상태에서 무엇을 보여줄지"(RenderContent)만
/// 자식 클래스가 결정한다.
///
/// 여러 자식(Pre/In/Post)을 같은 씬(혹은 같은 오브젝트 계층)에 나란히 붙여두면,
/// 각자 자기가 담당하는 State일 때만 자동으로 보이고 나머지는 스스로 숨는다.
/// </summary>
public abstract class DraftTimerIndicatorBase : MonoBehaviour
{
    [Header("Session")]
    [Tooltip("같은 씬에 미리 배치된 DraftSessionServer를 할당하면 Start()에서 자동 바인딩된다. " +
             "씬 전환으로 세션 오브젝트가 나중에 스폰되는 구조라면 Bind()를 직접 호출할 것.")]
    [SerializeField] protected DraftSessionServer session;

    [Header("View")]
    [Tooltip("타이머를 아예 감출 때 통째로 꺼둘 루트. 비워두면 timerText 오브젝트 자체를 껐다 켠다.")]
    [SerializeField] protected GameObject root;
    [SerializeField] protected TMP_Text timerText;

    /// <summary>
    /// 세션에 바인딩되기 "전"(Start/Bind가 실행되기 전 첫 프레임 포함) 기본으로 보여야 하는지 여부.
    ///
    /// 배경: 씬 전환 직후엔 이 클라이언트의 그리드/캐릭터 리스트가 로컬에서 곧바로 그려지는 반면,
    /// DraftSessionServer.State는 "전원이 로드를 마쳐야" 서버에서 Loading으로 바뀌고 그게 다시
    /// 네트워크로 동기화되어 온다. 그 사이 Bind()의 첫 Render()가 실행되면 session.State.Value는
    /// 아직 이전 값(Lobby)이라 IsActiveState가 false를 반환 → 커튼(Pre)이 "확인되기도 전에" 잠깐
    /// 열렸다가, 잠시 뒤 State가 Loading으로 동기화되면 다시 닫히는 깜빡임이 생긴다.
    ///
    /// 커튼처럼 "확인되기 전까지는 가리고 있어야" 하는 자식(Pre)은 true로 오버라이드해서
    /// Bind 이전부터 이미 닫혀 있게 하고, 확인되기 전까진 안 보여도 무방한 자식(In/Post)은
    /// 기본값(false)을 그대로 쓴다.
    /// </summary>
    protected virtual bool DefaultVisibleBeforeBind => false;

    protected virtual void Awake()
    {
        // Start()/Bind()보다 먼저 실행되어, 바인딩 전 첫 프레임의 상태를 확정해둔다.
        SetVisible(DefaultVisibleBeforeBind);
    }

    protected virtual void Start()
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

    protected virtual void OnDestroy()
    {
        DraftSessionServer.OnSessionReady -= Bind;
        Unbind();
    }

    public void Bind(DraftSessionServer newSession)
    {
        if (newSession == null)
        {
            Debug.LogError($"[{GetType().Name}] Bind에 null 세션이 전달되었습니다.");
            return;
        }

        DraftSessionServer.OnSessionReady -= Bind; // Start()의 안전망 구독이었다면 여기서 정리

        if (session != null) Unbind();
        session = newSession;

        session.State.OnValueChanged += HandleStateChanged;
        OnBound(session);

        Render();
    }

    public void Unbind()
    {
        if (session == null) return;

        session.State.OnValueChanged -= HandleStateChanged;
        OnUnbound(session);

        session = null;
    }

    /// <summary>
    /// 바인딩 시점에 자식이 자기 몫의 NetworkVariable(PreDraftSecondsRemaining 등)을
    /// 추가로 구독하고 싶을 때 오버라이드. Unbind 시 대칭으로 OnUnbound에서 해제할 것.
    /// </summary>
    protected virtual void OnBound(DraftSessionServer boundSession) { }
    protected virtual void OnUnbound(DraftSessionServer unboundSession) { }

    /// <summary>
    /// State가 바뀔 때마다 호출된다. 예: 종료 시점을 기록해야 하는 자식(PostDraft)이 사용.
    /// </summary>
    protected virtual void OnStateChanged(DraftSessionState previous, DraftSessionState current) { }

    // NetworkVariable.OnValueChanged는 "값이 실제로 바뀔 때만" 오므로, 상태 전환/매 초 갱신
    // 어느 쪽이 오든 항상 Render()로 현재 상태를 다시 계산해서 그린다.
    private void HandleStateChanged(DraftSessionState previous, DraftSessionState current)
    {
        OnStateChanged(previous, current);
        Render();
    }

    protected void Render()
    {
        if (session == null)
        {
            SetVisible(false);
            return;
        }

        if (!IsActiveState(session.State.Value))
        {
            SetVisible(false);
            return;
        }

        RenderContent();
    }

    /// <summary>이 컴포넌트가 담당하는(=화면에 나타나야 하는) DraftSessionState인지.</summary>
    protected abstract bool IsActiveState(DraftSessionState state);

    /// <summary>
    /// IsActiveState가 true일 때 호출된다. 여기서 SetVisible(true)와 텍스트 채우기를 직접 처리한다.
    /// (턴 타이머 미사용처럼) 조건에 따라 스스로 다시 숨기고 싶다면 SetVisible(false)를 호출해도 된다.
    /// </summary>
    protected abstract void RenderContent();

    protected void SetVisible(bool visible)
    {
        if (root) root.SetActive(visible);
        else if (timerText) timerText.gameObject.SetActive(visible);
    }
}
