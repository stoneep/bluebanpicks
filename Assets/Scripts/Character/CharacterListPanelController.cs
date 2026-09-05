

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class CharacterListPanelController : MonoBehaviour
{
    [Header("Views")]
    [SerializeField] private CharacterGridViewAdapter view;
    [SerializeField] private AffiliationFilterBar affiliationBar;
    [SerializeField] private TacticalRoleFilterBar tacticalRoleBar;
    [SerializeField] private CharacterSearchBar searchBar;
    
    [Header("Popup")]
    [SerializeField] private FilterPopupController filterPopup;
    [SerializeField] private Button openPopupBtn;

    [Header("Draft")]
    [Tooltip("할당하면 리스트 클릭이 밴/픽 '선택'으로 연결됨(제출은 확인 버튼에서). 밴픽 화면이 아니면 비워둘 것.")]
    [SerializeField] private DraftBoardController draftBoardController;
    [Tooltip("선택된 캐릭터의 밴/픽을 확정 제출하는 버튼. draftBoardController가 할당된 화면에서만 사용됨.")]
    [SerializeField] private Button confirmActionButton;

    private FilterEngine<CharacterViewData> engine;
    private readonly CharacterFilterState state = new();
    private readonly CharacterFilterRules rules = new();
    
    private readonly List<CharacterViewData> allData = new();
    private string pendingCharacterId;
    
    private readonly AtlasPreloader atlasPreloader = new();
    private CharacterArtProvider preloadArtProvider;
    
    public event Action<string> OnDraftSubmitFailed;

    private void Awake()
    {
        InitializeEngine();
        
        if (openPopupBtn) openPopupBtn.onClick.AddListener(OpenFilterPopup);
        if (filterPopup) filterPopup.OnApply += HandlePopupApply;
        if (confirmActionButton) confirmActionButton.onClick.AddListener(OnClickConfirmAction);

        
        PreloadAtlases();
    }

    private void InitializeEngine()
    {
        rules.RegisterTo(state);
        
        engine = new FilterEngine<CharacterViewData>(view, state.Pass);
        
        state.OnStateChanged += RefreshView;
    }

    private void RefreshView() => RefreshView(jumpToTop: true);

    private void RefreshView(bool jumpToTop)
    {
        engine.SetSort(state.GetComparison());
        engine.Rebuild(jumpToTop);
    }

    private void OnEnable()
    {
        if (affiliationBar) affiliationBar.OnValueChanged += OnQuickAffiliationChanged;
        if (tacticalRoleBar) tacticalRoleBar.OnValueChanged += OnQuickRoleChanged;
        if (searchBar) searchBar.OnValueChanged += OnSearchTextChanged;
        if (view) view.OnCharacterPicked += HandleCharacterPicked;

        if (draftBoardController)
        {
            view.SetAvailabilityPredicate(draftBoardController.IsCharacterAvailable);
            draftBoardController.OnActionSubmitted += HandleDraftActionSubmitted;
            draftBoardController.OnActionRejected += HandleDraftActionRejected;
        }
        
        ClearPendingSelection();
        RefreshView();
    }

    private void OnDisable()
    {
        if (affiliationBar) affiliationBar.OnValueChanged -= OnQuickAffiliationChanged;
        if (tacticalRoleBar) tacticalRoleBar.OnValueChanged -= OnQuickRoleChanged;
        if (searchBar) searchBar.OnValueChanged -= OnSearchTextChanged;
        if (view) view.OnCharacterPicked -= HandleCharacterPicked;

        if (draftBoardController)
        {
            draftBoardController.OnActionSubmitted -= HandleDraftActionSubmitted;
            draftBoardController.OnActionRejected -= HandleDraftActionRejected;
        }
    }
    
    private void ApplyContext(CharacterFilterContext context)
    {
        rules.Apply(context);
        
        state.SortType = context.SortType;
        state.Order = context.SortOrder;

        if (affiliationBar) affiliationBar.SyncVisual(context.Affiliation);
        if (tacticalRoleBar) tacticalRoleBar.SyncVisual(context.TacticalRole);
        if (searchBar) searchBar.SyncVisual(context.SearchText);

        state.NotifyChanged();
    }
    
    private void HandlePopupApply(CharacterFilterContext context) => ApplyContext(context);

    private void OnQuickAffiliationChanged(Affiliation? aff) {
        var ctx = GetCurrentContext();
        ctx.Affiliation = aff;
        ApplyContext(ctx);
    }

    private void OnQuickRoleChanged(TacticalRole? role) {
        var ctx = GetCurrentContext();
        ctx.TacticalRole = role;
        ApplyContext(ctx);
    }

    private void OnSearchTextChanged(string text) {
        var ctx = GetCurrentContext();
        ctx.SearchText = text;
        ApplyContext(ctx);
    }

    private void OpenFilterPopup() => filterPopup.Open(GetCurrentContext());
    
    private CharacterFilterContext GetCurrentContext() 
    {
        var ctx = new CharacterFilterContext {
            SortType = state.SortType,
            SortOrder = state.Order
        };
        rules.WriteTo(ref ctx);
        
        return ctx;
    }

    public void SetAllCharacters(List<CharacterViewData> characters)
    {
        allData.Clear();
        if (characters != null) allData.AddRange(characters);
        engine.SetAll(allData);
        state.NotifyChanged();

        
        PreloadPortraits(allData);
    }
    
    private void HandleCharacterPicked(CharacterViewData data)
    {
        if (!draftBoardController) return;
        if (!draftBoardController.IsSessionActive) return;
        if (!draftBoardController.IsCharacterAvailable(data.Id)) return;

        if (pendingCharacterId == data.Id)
        {
            ClearPendingSelection();
            return;
        }

        pendingCharacterId = data.Id;
        if (view) view.SetSelectedCharacter(pendingCharacterId);
        UpdateConfirmButtonInteractable();
        
        // 내 턴일 때만 서버로 전송 - 남의 턴에 구경하듯 클릭하는 것까지 네트워크를 태우지 않는다.
        if (draftBoardController.CurrentSide.HasValue &&
            draftBoardController.CurrentSide == draftBoardController.LocalSide)
        {
            draftBoardController.RequestPreview(pendingCharacterId);
        }
    }
    
    private void OnClickConfirmAction()
    {
        if (!draftBoardController || string.IsNullOrEmpty(pendingCharacterId)) return;

        draftBoardController.SubmitCharacter(pendingCharacterId);
        ClearPendingSelection();
    }
    
    private void ClearPendingSelection()
    {
        pendingCharacterId = null;
        if (view) view.SetSelectedCharacter(null);
        UpdateConfirmButtonInteractable();
        
        if (draftBoardController != null &&
            draftBoardController.CurrentSide.HasValue &&
            draftBoardController.CurrentSide == draftBoardController.LocalSide)
        {
            draftBoardController.ClearPreview();
        }
    }
    
    private void UpdateConfirmButtonInteractable()
    {
        if (confirmActionButton) confirmActionButton.interactable = !string.IsNullOrEmpty(pendingCharacterId);
    }
    
    private void HandleDraftActionRejected(string reason)
    {
        Debug.LogWarning($"[{nameof(CharacterListPanelController)}] 밴/픽 거부: {reason}");
        OnDraftSubmitFailed?.Invoke(reason);
    }
    
    private void HandleDraftActionSubmitted(DraftSide side, string characterId, DraftResultType type)
    {
        if (pendingCharacterId == characterId) ClearPendingSelection();
        RefreshView(jumpToTop: false);
    }
    
    private void PreloadAtlases()
    {
        var keys = new List<string>
        {
            UIExtensions.ATLAS_AFFILIATION,
            UIExtensions.ATLAS_COMMON
            
        };

        atlasPreloader.LoadAtlases(keys, () =>
        {
            Debug.Log("[CharacterListPanel] 아틀라스 프리로드 완료");
        });
    }

    
    
    
    
    
    private void PreloadPortraits(List<CharacterViewData> characters)
    {
        if (characters == null || characters.Count == 0) return;

        
        
        
        
        preloadArtProvider ??= new CharacterArtProvider();

        for (int i = 0; i < characters.Count; i++)
        {
            
            
            preloadArtProvider.LoadSprite(characters[i].Id, CharacterCut.Slot);
        }

        Debug.Log($"[CharacterListPanel] 초상화 프리로드 요청: {characters.Count}개");
    }

    private void OnDestroy()
    {
        
        preloadArtProvider?.ReleaseAll();
    }
}
