using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class DraftBoardController : MonoBehaviour
{
    [Header("Session")]
    [Tooltip("같은 씬에 미리 배치된 DraftSessionServer를 할당하면 Start()에서 자동 바인딩된다. " +
             "씬 전환으로 세션 오브젝트가 나중에 스폰되는 구조라면 Bind()를 직접 호출할 것.")]
    [SerializeField] private DraftSessionServer session;

    [Header("Bars")]
    [SerializeField] private PickSlotBar firstPickBar;   
    [SerializeField] private PickSlotBar firstBanBar;    
    [SerializeField] private PickSlotBar secondBanBar;   
    [SerializeField] private PickSlotBar secondPickBar;  

    private readonly Dictionary<(DraftSide side, DraftResultType type), int> barCursor = new();
    private readonly HashSet<string> usedCharacterIds = new(); 
    private string lastAnnouncedPhaseName;
    
    public event Action<DraftSide> OnTurnChanged;
    public event Action<string> OnPhaseChanged;          
    public event Action<DraftSide, string, DraftResultType> OnActionSubmitted;
    public event Action OnDraftCompleted;
    public event Action<string> OnActionRejected;

    public bool IsDraftComplete => session != null && session.State.Value == DraftSessionState.Completed;

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
            Debug.LogError($"[{nameof(DraftBoardController)}] Bind에 null 세션이 전달되었습니다.");
            return;
        }
        Debug.Log($"[{nameof(DraftBoardController)}] Bind() session={newSession.GetEntityId()}, " +
                  $"scene={newSession.gameObject.scene.name}, " +
                  $"same as Instance? {ReferenceEquals(newSession, DraftSessionServer.Instance)}");
        DraftSessionServer.OnSessionReady -= Bind; 

        if (session != null) Unbind();
        session = newSession;

        session.Format.OnListChanged += HandleFormatChanged;
        session.ActionLog.OnListChanged += HandleActionLogChanged;
        session.State.OnValueChanged += HandleStateChanged;
        session.OnActionRejected += HandleActionRejected;
        session.CurrentSide.OnValueChanged += HandleCurrentSideChanged;
        session.CurrentPhaseName.OnValueChanged += HandleCurrentPhaseNameChanged;
        
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
        session.CurrentSide.OnValueChanged -= HandleCurrentSideChanged;
        session.CurrentPhaseName.OnValueChanged -= HandleCurrentPhaseNameChanged;
        
        session = null;
        lastAnnouncedPhaseName = null;
    }
    
    public void SubmitCharacter(string characterId)
    {
        if (session == null)
        {
            Debug.LogWarning($"[{nameof(DraftBoardController)}] 세션이 바인딩되지 않아 요청을 보낼 수 없습니다.");
            return;
        }

        if (session.State.Value != DraftSessionState.InProgress)
        {
            
            
            
            Debug.Log($"[{nameof(DraftBoardController)}] 드래프트가 진행 중이 아니라 요청을 보내지 않았습니다. (State={session.State.Value})");
            return;
        }

        session.SubmitActionServerRpc(characterId);
    }
    
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
    
    private void ClearAllNextSlotHighlights()
    {
        firstPickBar.ClearNextSlotHighlight();
        firstBanBar.ClearNextSlotHighlight();
        secondBanBar.ClearNextSlotHighlight();
        secondPickBar.ClearNextSlotHighlight();
    }
    
    private void UpdateNextSlotIndicator()
    {
        ClearAllNextSlotHighlights();

        if (session.State.Value != DraftSessionState.InProgress) return;
        if (!Enum.TryParse(session.CurrentPhaseName.Value.ToString(), out DraftResultType type)) return;

        var side = session.CurrentSide.Value;
        var bar = ResolveBar(side, type);
        if (!bar) return;

        var key = (side, type);
        int nextIndex = barCursor.TryGetValue(key, out var cursor) ? cursor : 0;
        bar.HighlightNextSlot(nextIndex);
    }
    
    private void HandleFormatChanged(NetworkListEvent<NetworkDraftRoundConfig> _) => RebuildBars();

    private void RebuildBars()
    {
        var format = session.Format.ToDraftFormatData();

        firstPickBar.ApplyConfig(PickSlotBarConfig.Of(SumSlots(format, DraftSide.First, DraftResultType.Pick)));
        firstBanBar.ApplyConfig(PickSlotBarConfig.Of(SumSlots(format, DraftSide.First, DraftResultType.Ban)));
        secondBanBar.ApplyConfig(PickSlotBarConfig.Of(SumSlots(format, DraftSide.Second, DraftResultType.Ban)));
        secondPickBar.ApplyConfig(PickSlotBarConfig.Of(SumSlots(format, DraftSide.Second, DraftResultType.Pick)));
    }
    
    private void ReplayExistingActions()
    {
        ClearBoardLocal();
        lastAnnouncedPhaseName = null;

        foreach (var action in session.ActionLog)
            ApplyAction(action.side, action.characterId.ToString(), action.resultType, notify: false);

        
        AnnounceCurrentTurnIfInProgress();
    }

    private void HandleActionLogChanged(NetworkListEvent<NetworkDraftAction> change)
    {
        if (change.Type != NetworkListEvent<NetworkDraftAction>.EventType.Add) return;

        var action = change.Value;
        ApplyAction(action.side, action.characterId.ToString(), action.resultType, notify: true);
    }
    
    private void HandleCurrentPhaseNameChanged(FixedString32Bytes previous, FixedString32Bytes current)
    {
        if (session == null || session.State.Value != DraftSessionState.InProgress) return;
        UpdateNextSlotIndicator();
    }
    
    private void HandleCurrentSideChanged(DraftSide previous, DraftSide current)
    {
        if (session == null || session.State.Value != DraftSessionState.InProgress) return;
        UpdateNextSlotIndicator();
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
    
    private void AnnounceCurrentTurnIfInProgress()
    {
        if (session.State.Value != DraftSessionState.InProgress) return;

        string phaseName = session.CurrentPhaseName.Value.ToString();
        if (phaseName != lastAnnouncedPhaseName)
        {
            lastAnnouncedPhaseName = phaseName;
            OnPhaseChanged?.Invoke(phaseName);
        }

        UpdateNextSlotIndicator();
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
            
            
            AnnounceCurrentTurnIfInProgress();
        }
        else if (current == DraftSessionState.Completed)
        {
            ClearAllNextSlotHighlights();
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
