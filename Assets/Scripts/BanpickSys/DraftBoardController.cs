using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 선공 픽바 / 선공 밴바 / 후공 밴바 / 후공 픽바, 4개의 PickSlotBar를
/// DraftSessionServer가 동기화하는 상태(Format/ActionLog/CurrentSide/State)만 구독해서
/// 화면에 반영하는 순수 View.
///
/// - RuleManager를 직접 만들거나 참조하지 않는다 (서버 전용).
/// - "지금 누구 차례인가", "이 캐릭터를 골라도 되는가" 판단은 전부 서버(RuleManager)가 하고,
///   이 클래스는 SubmitCharacter로 요청만 보낸 뒤 결과(ActionLog 추가 / OnActionRejected)를
///   수동적으로 반영한다. 요청이 성공했는지 실패했는지는 즉시 알 수 없다 - 네트워크 특성상
///   서버 왕복 후에야 반영되므로, 예전처럼 SubmitCharacter가 bool을 즉시 반환하지 않는다.
/// - ActionLog는 NetworkList라 스폰/바인딩 시점에 이미 쌓여있는 기록까지 자동으로 동기화되므로
///   드래프트 도중 접속한 클라이언트(late-join)도 ReplayExistingActions()로 상태를 그대로 복원한다.
///
/// 참고: 지금은 라운드 구분 없이 한 줄 바에 전부 이어붙이는 방식이다.
/// 라운드별로 바를 나누거나 탭으로 구분하는 건 별도 UI 작업이 필요하다.
/// </summary>
public class DraftBoardController : MonoBehaviour
{
    [Header("Session")]
    [Tooltip("같은 씬에 미리 배치된 DraftSessionServer를 할당하면 Start()에서 자동 바인딩된다. " +
             "씬 전환으로 세션 오브젝트가 나중에 스폰되는 구조라면 Bind()를 직접 호출할 것.")]
    [SerializeField] private DraftSessionServer session;

    [Header("Bars")]
    [SerializeField] private PickSlotBar firstPickBar;   // 선공 선택픽 (전체 라운드 합산)
    [SerializeField] private PickSlotBar firstBanBar;    // 선공 밴픽   (전체 라운드 합산)
    [SerializeField] private PickSlotBar secondBanBar;   // 후공 밴픽   (전체 라운드 합산)
    [SerializeField] private PickSlotBar secondPickBar;  // 후공 선택픽 (전체 라운드 합산)

    private readonly Dictionary<(DraftSide side, DraftResultType type), int> barCursor = new();
    private readonly HashSet<string> usedCharacterIds = new(); // 서버 ActionLog를 미러링한 UX 캐시 (진실은 서버)

    // NetworkVariable.OnValueChanged는 "값이 실제로 바뀔 때만" 발화한다.
    // 예를 들어 첫 페이즈의 시작 진영이 CurrentSide의 기본값(First)과 같거나,
    // 마지막에 한 진영이 연속으로 턴을 갖는 경우 값이 안 바뀌어 이벤트가 안 올 수 있다.
    // 그래서 CurrentSide.OnValueChanged에 의존하지 않고, ActionLog 반영/상태 전이 시점에
    // 직접 캐시와 비교해서 OnPhaseChanged/OnTurnChanged를 발행한다.
    private string lastAnnouncedPhaseName;

    // ==================== 외부 구독용 이벤트 ====================
    // DraftTurnIndicator 등은 세션/네트워크 타입을 몰라도 되도록 이 이벤트들만 구독하면 됨.
    public event Action<DraftSide> OnTurnChanged;
    public event Action<string> OnPhaseChanged;          // "Ban" / "Pick"
    public event Action<DraftSide, string, DraftResultType> OnActionSubmitted;
    public event Action OnDraftCompleted;

    /// <summary>SubmitCharacter 요청이 서버에서 거부됐을 때(차례 아님, 이미 사용됨 등) 사유 전달.</summary>
    public event Action<string> OnActionRejected;

    public bool IsDraftComplete => session != null && session.State.Value == DraftSessionState.Completed;

    /// <summary>
    /// 지금 이 컨트롤러가 실제로 "진행 중"인 드래프트 세션에 연결되어 있는지.
    /// 세션이 아직 대기실(Lobby)이거나, 종료(Completed)됐거나, 아직 Bind() 전이면 false.
    /// SubmitCharacter를 부르기 전에 이걸로 먼저 걸러내면, 진행 중이 아닌 화면에서 클릭했을 때
    /// 불필요한 서버 왕복과 혼란스러운 OnActionRejected를 피할 수 있다.
    /// </summary>
    public bool IsSessionActive
    {
        get
        {
            bool result = session != null && session.State.Value == DraftSessionState.InProgress;
            Debug.Log($"[{nameof(DraftBoardController)}] (IsServer={session?.IsServer}, IsClient={session?.IsClient}, " +
                      $"LocalClientId={NetworkManager.Singleton?.LocalClientId}) IsSessionActive={result} " +
                      $"(session={(session ? session.GetEntityId().ToString() : "null")}, State={(session != null ? session.State.Value.ToString() : "N/A")}) @ frame {Time.frameCount}");
            return result;
        }
    }
    public DraftSide? CurrentSide => (session != null && session.State.Value == DraftSessionState.InProgress) ? session.CurrentSide.Value : null;
    public string CurrentPhaseName => (session != null && session.State.Value == DraftSessionState.InProgress) ? session.CurrentPhaseName.Value.ToString() : null;

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
            Debug.LogError($"[{nameof(DraftBoardController)}] Bind에 null 세션이 전달되었습니다.");
            return;
        }
        Debug.Log($"[{nameof(DraftBoardController)}] Bind() session={newSession.GetEntityId()}, " +
                  $"scene={newSession.gameObject.scene.name}, " +
                  $"same as Instance? {ReferenceEquals(newSession, DraftSessionServer.Instance)}");
        DraftSessionServer.OnSessionReady -= Bind; // Start()의 안전망 구독이었다면 여기서 정리

        if (session != null) Unbind();
        session = newSession;

        session.Format.OnListChanged += HandleFormatChanged;
        session.ActionLog.OnListChanged += HandleActionLogChanged;
        session.State.OnValueChanged += HandleStateChanged;
        session.OnActionRejected += HandleActionRejected;

        if (session.Format.Count > 0) RebuildBars();
        ReplayExistingActions();
    }

    public void Unbind()
    {
        if (session == null) return;

        session.Format.OnListChanged -= HandleFormatChanged;
        session.ActionLog.OnListChanged -= HandleActionLogChanged;
        session.State.OnValueChanged -= HandleStateChanged;
        session.OnActionRejected -= HandleActionRejected;

        session = null;
        lastAnnouncedPhaseName = null;
    }

    // ==================== 진행 API ====================

    /// <summary>
    /// characterId를 밴/픽 요청으로 서버에 제출한다.
    /// 결과(성공: ActionLog 반영 / 실패: OnActionRejected)는 비동기로 온다 -
    /// 예전처럼 이 호출 시점에 성공 여부를 알 수 없다는 점에 주의.
    /// </summary>
    public void SubmitCharacter(string characterId)
    {
        if (session == null)
        {
            Debug.LogWarning($"[{nameof(DraftBoardController)}] 세션이 바인딩되지 않아 요청을 보낼 수 없습니다.");
            return;
        }

        if (session.State.Value != DraftSessionState.InProgress)
        {
            // 대기실/종료 상태에서의 클릭은 서버에 물어볼 필요도 없이 이미 결과를 알 수 있다.
            // (같은 화면/프리팹이 "밴픽 화면"과 "일반 캐릭터 목록 화면" 양쪽에 재사용되는 경우
            //  대기실 상태에서 클릭이 들어오면 여기서 조용히 걸러진다)
            Debug.Log($"[{nameof(DraftBoardController)}] 드래프트가 진행 중이 아니라 요청을 보내지 않았습니다. (State={session.State.Value})");
            return;
        }

        session.SubmitActionServerRpc(characterId);
    }

    /// <summary>이미 밴/픽되어 더 이상 선택할 수 없는 캐릭터인지 (리스트 버튼 비활성화 등에 사용)</summary>
    public bool IsCharacterAvailable(string characterId) => !usedCharacterIds.Contains(characterId);

    private void ClearBoardLocal()
    {
        firstPickBar.ClearAll();
        firstBanBar.ClearAll();
        secondBanBar.ClearAll();
        secondPickBar.ClearAll();
        barCursor.Clear();
        usedCharacterIds.Clear();
    }

    // ==================== 세션 이벤트 핸들러 ====================

    private void HandleFormatChanged(NetworkListEvent<NetworkDraftRoundConfig> _) => RebuildBars();

    private void RebuildBars()
    {
        var format = session.Format.ToDraftFormatData();

        firstPickBar.ApplyConfig(PickSlotBarConfig.Of(SumSlots(format, DraftSide.First, DraftResultType.Pick)));
        firstBanBar.ApplyConfig(PickSlotBarConfig.Of(SumSlots(format, DraftSide.First, DraftResultType.Ban)));
        secondBanBar.ApplyConfig(PickSlotBarConfig.Of(SumSlots(format, DraftSide.Second, DraftResultType.Ban)));
        secondPickBar.ApplyConfig(PickSlotBarConfig.Of(SumSlots(format, DraftSide.Second, DraftResultType.Pick)));
    }

    /// <summary>바인딩 시점에 이미 ActionLog에 쌓여있는 기록을 그대로 재생 (late-join 대응).</summary>
    private void ReplayExistingActions()
    {
        ClearBoardLocal();
        lastAnnouncedPhaseName = null;

        foreach (var action in session.ActionLog)
            ApplyAction(action.side, action.characterId.ToString(), action.resultType, notify: false);

        // 과거 기록은 조용히 반영만 하고, "지금 상태"는 마지막에 한 번만 알려준다.
        AnnounceCurrentTurnIfInProgress();
    }

    private void HandleActionLogChanged(NetworkListEvent<NetworkDraftAction> change)
    {
        // 이 컨트롤러는 항상 처음부터 구독해서 Add 이벤트만 순서대로 받는다고 가정한다.
        // (재바인딩 시엔 ReplayExistingActions가 전체를 이미 처리하므로 여기선 Add만 다룸)
        if (change.Type != NetworkListEvent<NetworkDraftAction>.EventType.Add) return;

        var action = change.Value;
        ApplyAction(action.side, action.characterId.ToString(), action.resultType, notify: true);
    }

    private void ApplyAction(DraftSide side, string characterId, DraftResultType type, bool notify)
    {
        var bar = ResolveBar(side, type);
        if (!bar)
        {
            Debug.LogError($"[{nameof(DraftBoardController)}] {side}/{type}에 대응하는 PickSlotBar가 없습니다.");
            return;
        }

        var key = (side, type);
        int index = barCursor.TryGetValue(key, out var cursor) ? cursor : 0;
        bar.SetCharacter(index, characterId);
        barCursor[key] = index + 1;
        usedCharacterIds.Add(characterId);

        if (!notify) return;

        OnActionSubmitted?.Invoke(side, characterId, type);
        AnnounceCurrentTurnIfInProgress();
    }

    /// <summary>
    /// 서버가 동기화한 현재 페이즈명/진영을 기준으로, 필요할 때만 OnPhaseChanged를 (캐시와 다를 때)
    /// 그리고 항상 OnTurnChanged를 발행한다. NetworkVariable.OnValueChanged 대신 이 경로로
    /// 직접 비교하는 이유는 클래스 상단 주석 참고.
    /// </summary>
    private void AnnounceCurrentTurnIfInProgress()
    {
        if (session.State.Value != DraftSessionState.InProgress) return;

        string phaseName = session.CurrentPhaseName.Value.ToString();
        if (phaseName != lastAnnouncedPhaseName)
        {
            lastAnnouncedPhaseName = phaseName;
            OnPhaseChanged?.Invoke(phaseName);
        }

        OnTurnChanged?.Invoke(session.CurrentSide.Value);
    }

    private void HandleStateChanged(DraftSessionState previous, DraftSessionState current)
    {
        Debug.Log($"[{nameof(DraftBoardController)}] State changed: {previous} -> {current} @ frame {Time.frameCount}");
        
        if (current == DraftSessionState.Lobby)
        {
            ClearBoardLocal();
            lastAnnouncedPhaseName = null;
        }
        else if (current == DraftSessionState.InProgress)
        {
            // HostStartDraft()가 첫 페이즈까지 동기 진행시키므로, State가 InProgress로 바뀐
            // 이 시점엔 CurrentPhaseName/CurrentSide가 이미 첫 페이즈 값으로 세팅돼 있다.
            AnnounceCurrentTurnIfInProgress();
        }
        else if (current == DraftSessionState.Completed)
        {
            OnDraftCompleted?.Invoke();
        }
    }

    private void HandleActionRejected(string reason) => OnActionRejected?.Invoke(reason);

    private PickSlotBar ResolveBar(DraftSide side, DraftResultType type)
    {
        return (side, type) switch
        {
            (DraftSide.First, DraftResultType.Ban) => firstBanBar,
            (DraftSide.First, DraftResultType.Pick) => firstPickBar,
            (DraftSide.Second, DraftResultType.Ban) => secondBanBar,
            (DraftSide.Second, DraftResultType.Pick) => secondPickBar,
            _ => null
        };
    }

    /// <summary>모든 라운드를 통틀어 해당 (진영, 밴/픽) 슬롯 수 총합을 구한다.</summary>
    private static int SumSlots(IDraftFormat format, DraftSide side, DraftResultType type)
    {
        int total = 0;
        foreach (var round in format.Rounds)
        {
            total += (side, type) switch
            {
                (DraftSide.First, DraftResultType.Ban) => round.FirstBanSlots,
                (DraftSide.First, DraftResultType.Pick) => round.FirstPickSlots,
                (DraftSide.Second, DraftResultType.Ban) => round.SecondBanSlots,
                (DraftSide.Second, DraftResultType.Pick) => round.SecondPickSlots,
                _ => 0
            };
        }
        return total;
    }
}
