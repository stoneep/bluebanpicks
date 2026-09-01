using TMPro;
using UnityEngine;













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

    
    
    
    
    
    
    
    
    
    
    
    
    
    protected virtual bool DefaultVisibleBeforeBind => false;

    protected virtual void Awake()
    {
        
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
            
            Bind(DraftSessionServer.Instance);
        }
        else
        {
            
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

        DraftSessionServer.OnSessionReady -= Bind; 

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

    
    
    
    
    protected virtual void OnBound(DraftSessionServer boundSession) { }
    protected virtual void OnUnbound(DraftSessionServer unboundSession) { }

    
    
    
    protected virtual void OnStateChanged(DraftSessionState previous, DraftSessionState current) { }

    
    
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

    
    protected abstract bool IsActiveState(DraftSessionState state);

    
    
    
    
    protected abstract void RenderContent();

    protected void SetVisible(bool visible)
    {
        if (root) root.SetActive(visible);
        else if (timerText) timerText.gameObject.SetActive(visible);
    }
}
