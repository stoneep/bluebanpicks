using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;












public sealed class DraftResultPanelController : MonoBehaviour
{
    [Header("Session")]
    [Tooltip("같은 씬에 미리 배치된 DraftSessionServer를 할당하면 Start()에서 자동 바인딩된다. " +
             "씬 전환으로 세션 오브젝트가 나중에 스폰되는 구조라면 Bind()를 직접 호출할 것.")]
    [SerializeField] private DraftSessionServer session;

    [Header("View")]
    [Tooltip("결과창 전체를 감출 루트. 평소엔 꺼두고 종료 시점에 켠다.")]
    [SerializeField] private GameObject root;
    [SerializeField] private DraftResultRowView rowPrefab;
    [Tooltip("VerticalLayoutGroup 등이 붙은, 행들이 순서대로 쌓일 컨텐츠 루트 (ScrollRect Content 등).")]
    [SerializeField] private Transform rowContainer;

    [Header("Picked Characters (하단 좌/우 요약)")]
    [Tooltip("왼쪽에 보여줄 진영의 픽 슬롯 바. 참가자 시점엔 '나', 관전자 시점엔 '선공'이 배정된다. " +
             "PickSlotBar의 SlotCount를 6으로 설정해둘 것.")]
    [SerializeField] private PickSlotBar leftPickSlotBar;
    [SerializeField] private TMP_Text leftSideLabel;
    [Tooltip("오른쪽에 보여줄 진영의 픽 슬롯 바. 참가자 시점엔 '상대', 관전자 시점엔 '후공'이 배정된다.")]
    [SerializeField] private PickSlotBar rightPickSlotBar;
    [SerializeField] private TMP_Text rightSideLabel;

    private readonly List<DraftResultRowView> rows = new();
    private bool isBuilt;

    private void Awake()
    {
        SetVisible(false);
    }

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
            Debug.LogError($"[{nameof(DraftResultPanelController)}] Bind에 null 세션이 전달되었습니다.");
            return;
        }

        DraftSessionServer.OnSessionReady -= Bind; 

        if (session != null) Unbind();
        session = newSession;

        session.State.OnValueChanged += HandleStateChanged;
        session.ActionLog.OnListChanged += HandleActionLogChanged;

        
        TryShowResultIfReady();
    }

    public void Unbind()
    {
        if (session == null) return;

        session.State.OnValueChanged -= HandleStateChanged;
        session.ActionLog.OnListChanged -= HandleActionLogChanged;
        session = null;
    }

    

    
    public void Close() => SetVisible(false);

    

    private void HandleStateChanged(DraftSessionState previous, DraftSessionState current)
    {
        if (current == DraftSessionState.Completed)
        {
            TryShowResultIfReady();
        }
        else if (current == DraftSessionState.Lobby)
        {
            
            
            isBuilt = false;
            ClearRows();
            SetVisible(false);
        }
    }

    
    
    
    
    
    
    
    
    
    
    
    private void HandleActionLogChanged(NetworkListEvent<NetworkDraftAction> change) => TryShowResultIfReady();

    private void TryShowResultIfReady()
    {
        if (isBuilt || session == null) return;
        if (session.State.Value != DraftSessionState.Completed) return;
        if (session.ActionLog.Count < ExpectedTotalActionCount()) return; 

        BuildRows();
        isBuilt = true;
        SetVisible(true);
    }

    
    private int ExpectedTotalActionCount()
    {
        int total = 0;
        foreach (var round in session.Format)
            total += round.firstBanSlots + round.secondBanSlots + round.firstPickSlots + round.secondPickSlots;
        return total;
    }

    

    private void BuildRows()
    {
        ClearRows();

        if (!rowPrefab || !rowContainer)
        {
            Debug.LogError($"[{nameof(DraftResultPanelController)}] rowPrefab/rowContainer가 할당되지 않았습니다.");
            return;
        }

        var localSide = session.LocalSide;

        
        var leftSide = localSide ?? DraftSide.First;
        var rightSide = leftSide == DraftSide.First ? DraftSide.Second : DraftSide.First;

        if (leftSideLabel) leftSideLabel.text = ResolveSideLabel(leftSide, localSide);
        if (rightSideLabel) rightSideLabel.text = ResolveSideLabel(rightSide, localSide);

        int order = 1;
        int leftPickIndex = 0;
        int rightPickIndex = 0;

        
        
        foreach (var action in session.ActionLog)
        {
            var row = Instantiate(rowPrefab, rowContainer);
            row.name = $"ResultRow_{order:00}";
            row.Bind(order, action.side, action.resultType, action.characterId.ToString(),
                      ResolveSideLabel(action.side, localSide));
            rows.Add(row);
            order++;

            
            if (action.resultType != DraftResultType.Pick) continue;

            if (action.side == leftSide)
            {
                if (leftPickSlotBar) leftPickSlotBar.SetCharacter(leftPickIndex, action.characterId.ToString());
                leftPickIndex++;
            }
            else
            {
                if (rightPickSlotBar) rightPickSlotBar.SetCharacter(rightPickIndex, action.characterId.ToString());
                rightPickIndex++;
            }
        }
    }

    
    
    
    
    private static string ResolveSideLabel(DraftSide side, DraftSide? localSide)
    {
        if (localSide.HasValue)
            return side == localSide.Value ? "나" : "상대";

        return side == DraftSide.First ? "선공" : "후공";
    }

    private void ClearRows()
    {
        foreach (var row in rows)
        {
            if (row) Destroy(row.gameObject);
        }
        rows.Clear();

        if (leftPickSlotBar) leftPickSlotBar.ClearAll();
        if (rightPickSlotBar) rightPickSlotBar.ClearAll();
    }

    private void SetVisible(bool visible)
    {
        if (root) root.SetActive(visible);
    }
}
