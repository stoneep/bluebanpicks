using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 밴픽이 완전히 종료(State == Completed)되면, 서버가 이미 동기화해둔 ActionLog를
/// 처음부터 순서대로 재생해 "누가 몇 번째로 어떤 밴/픽을 했는지" 보여주는 결과창.
///
/// ActionLog(NetworkList)는 진행 중 계속 쌓여서 이미 전원에게 복제되어 있으므로
/// (late-join 대응으로 원래 그렇게 설계됨), 종료 시점에 새로 서버에 뭘 요청할 필요 없이
/// 그 로그를 그대로 순회하기만 하면 된다.
///
/// DraftTimerIndicatorBase류와 동일한 바인딩 패턴(OnSessionReady 안전망, late-join 시
/// 이미 Completed인 세션에 뒤늦게 붙는 경우 처리)을 따른다.
/// </summary>
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
            // 씬 전환(ConnectionLobby -> MainLobby) 이전에 이미 스폰되어 살아있는 세션을 그대로 찾아 바인딩.
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

    // ==================== 바인딩 ====================

    public void Bind(DraftSessionServer newSession)
    {
        if (newSession == null)
        {
            Debug.LogError($"[{nameof(DraftResultPanelController)}] Bind에 null 세션이 전달되었습니다.");
            return;
        }

        DraftSessionServer.OnSessionReady -= Bind; // Start()의 안전망 구독이었다면 여기서 정리

        if (session != null) Unbind();
        session = newSession;

        session.State.OnValueChanged += HandleStateChanged;
        session.ActionLog.OnListChanged += HandleActionLogChanged;

        // late-join 등으로 이미 종료된 세션에 뒤늦게 바인딩되는 경우 즉시 결과를 보여준다.
        TryShowResultIfReady();
    }

    public void Unbind()
    {
        if (session == null) return;

        session.State.OnValueChanged -= HandleStateChanged;
        session.ActionLog.OnListChanged -= HandleActionLogChanged;
        session = null;
    }

    // ==================== 외부 API ====================

    /// <summary>닫기 버튼 등에서 호출. 다시 열면(root 재활성화) 이미 만들어둔 행을 그대로 보여준다.</summary>
    public void Close() => SetVisible(false);

    // ==================== 세션 이벤트 ====================

    private void HandleStateChanged(DraftSessionState previous, DraftSessionState current)
    {
        if (current == DraftSessionState.Completed)
        {
            TryShowResultIfReady();
        }
        else if (current == DraftSessionState.Lobby)
        {
            // 같은 세션으로 새 드래프트를 다시 시작하는 경우(재대국 등) 결과창도 초기화해서
            // 다음 종료 시점에 새 로그로 다시 빌드되게 한다.
            isBuilt = false;
            ClearRows();
            SetVisible(false);
        }
    }

    /// <summary>
    /// ActionLog(NetworkList)에 새 항목이 추가될 때마다 호출됨.
    /// State==Completed 판정과 ActionLog의 "마지막 Add"는 같은 네트워크 틱에 실려도
    /// 클라이언트에 적용되는 순서가 보장되지 않는다 (NGO에서 같은 오브젝트의 여러
    /// NetworkVariable/List가 같은 틱에 바뀌면 콜백 발화 순서가 비결정적).
    /// State.OnValueChanged가 먼저 와버리면 ActionLog의 마지막 밴/픽 1건이
    /// 아직 반영되기 전에 결과창을 만들어 1명이 빠진 채로 굳어버리는 문제가 있었다
    /// (특히 그 마지막 액션을 직접 제출한 클라이언트에서 재현됨).
    /// 그래서 두 이벤트 중 어느 쪽이 먼저 오든, "State==Completed"와 "ActionLog 개수가
    /// 포맷상 기대하는 전체 액션 수에 도달"을 모두 만족할 때만 빌드하도록 이중으로 체크한다.
    /// </summary>
    private void HandleActionLogChanged(NetworkListEvent<NetworkDraftAction> change) => TryShowResultIfReady();

    private void TryShowResultIfReady()
    {
        if (isBuilt || session == null) return;
        if (session.State.Value != DraftSessionState.Completed) return;
        if (session.ActionLog.Count < ExpectedTotalActionCount()) return; // 마지막 액션이 아직 도착 전

        BuildRows();
        isBuilt = true;
        SetVisible(true);
    }

    /// <summary>Format(NetworkList)로부터 이번 세션에서 총 몇 건의 밴/픽이 나와야 끝나는지 계산.</summary>
    private int ExpectedTotalActionCount()
    {
        int total = 0;
        foreach (var round in session.Format)
            total += round.firstBanSlots + round.secondBanSlots + round.firstPickSlots + round.secondPickSlots;
        return total;
    }

    // ==================== 행 구성 ====================

    private void BuildRows()
    {
        ClearRows();

        if (!rowPrefab || !rowContainer)
        {
            Debug.LogError($"[{nameof(DraftResultPanelController)}] rowPrefab/rowContainer가 할당되지 않았습니다.");
            return;
        }

        var localSide = session.LocalSide;
        int order = 1;

        // ActionLog는 서버가 밴/픽이 확정될 때마다 순서대로 Add해온 기록이라,
        // 여기서 그냥 처음부터 순회하는 것만으로 "실제 진행 순서"가 그대로 보장된다.
        foreach (var action in session.ActionLog)
        {
            var row = Instantiate(rowPrefab, rowContainer);
            row.name = $"ResultRow_{order:00}";
            row.Bind(order, action.side, action.resultType, action.characterId.ToString(),
                      ResolveSideLabel(action.side, localSide));
            rows.Add(row);
            order++;
        }
    }

    /// <summary>
    /// 참가자(선공/후공 배정된 클라이언트)에게는 "나"/"상대"로, 관전자(호스트 등)에게는
    /// "선공"/"후공"으로 보여준다. InDraftTurnTimerIndicator의 라벨 규칙과 동일하게 맞췄다.
    /// </summary>
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
    }

    private void SetVisible(bool visible)
    {
        if (root) root.SetActive(visible);
    }
}
