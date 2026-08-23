// CharacterListPanelController.cs

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
    [Tooltip("할당하면 리스트 클릭이 곧바로 밴/픽 제출로 연결됨. 밴픽 화면이 아니면 비워둘 것.")]
    [SerializeField] private DraftBoardController draftBoardController;

    private FilterEngine<CharacterViewData> engine;
    private readonly CharacterFilterState state = new();
    private readonly CharacterFilterRules rules = new();
    
    private readonly List<CharacterViewData> allData = new();

    // ★ 프리로드용
    private readonly AtlasPreloader atlasPreloader = new();
    private CharacterArtProvider preloadArtProvider;

    /// <summary>
    /// draftBoardController.SubmitCharacter가 실패했을 때(차례가 아님, 이미 밴/픽됨 등)
    /// 사유를 그대로 전달. 토스트 UI 등에서 구독해서 사용자에게 보여주면 됨.
    /// </summary>
    public event Action<string> OnDraftSubmitFailed;

    private void Awake()
    {
        InitializeEngine();
        
        if (openPopupBtn) openPopupBtn.onClick.AddListener(OpenFilterPopup);
        if (filterPopup) filterPopup.OnApply += HandlePopupApply;

        // ★ 아틀라스 프리로드 (학원, 공통 아이콘 등)
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
        }
        
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

        // ★ 모든 캐릭터 초상화를 백그라운드 프리로드
        PreloadPortraits(allData);
    }

    // ═══════════════════════════════════════
    //  ★ 밴픽 연결
    // ═══════════════════════════════════════

    /// <summary>
    /// 리스트에서 캐릭터를 클릭했을 때. draftBoardController가 할당돼 있으면
    /// 곧바로 SubmitCharacter로 넘긴다 - "지금 누구 차례인가" 같은 판단은
    /// 전부 RuleManager 쪽 책임이고 여기서는 결과(성공/실패)만 처리한다.
    /// </summary>
    private void HandleCharacterPicked(CharacterViewData data)
    {
        if (!draftBoardController) return; // 밴픽 화면이 아니면 무시

        if (!draftBoardController.SubmitCharacter(data.Id, out var error))
        {
            Debug.LogWarning($"[{nameof(CharacterListPanelController)}] 밴/픽 실패: {error}");
            OnDraftSubmitFailed?.Invoke(error);
        }
    }

    /// <summary>
    /// 밴/픽이 하나 성사될 때마다 방금 선택된 캐릭터를 포함해 리스트 전체의
    /// "선택 가능 여부"가 바뀌므로, 화면에 보이는 슬롯들을 다시 바인딩해서
    /// 락 오버레이(빨간색: 밴/픽됨)를 최신 상태로 반영한다.
    /// 스크롤 위치를 유지해야 하므로 jumpToTop: false로 새로고침.
    /// </summary>
    private void HandleDraftActionSubmitted(DraftSide side, string characterId, DraftResultType type)
    {
        RefreshView(jumpToTop: false);
    }

    // ═══════════════════════════════════════
    //  ★ 프리로드 로직
    // ═══════════════════════════════════════

    /// <summary>
    /// 캐릭터 슬롯에서 사용하는 아틀라스를 미리 로드
    /// Awake에서 1회 호출 — 이후 AtlasImageBinder가 handle.IsDone == true로 동기 바인딩
    /// </summary>
    private void PreloadAtlases()
    {
        var keys = new List<string>
        {
            UIExtensions.ATLAS_AFFILIATION,
            UIExtensions.ATLAS_COMMON
            // 추가 아틀라스가 있으면 여기에
        };

        atlasPreloader.LoadAtlases(keys, () =>
        {
            Debug.Log("[CharacterListPanel] 아틀라스 프리로드 완료");
        });
    }

    /// <summary>
    /// 모든 캐릭터의 Head 초상화를 백그라운드에서 로드 요청
    /// CharacterArtProvider 내부 cache에 handle이 저장되므로
    /// 이후 BindSlot 시점에 IsDone == true → 즉시 적용
    /// </summary>
    private void PreloadPortraits(List<CharacterViewData> characters)
    {
        if (characters == null || characters.Count == 0) return;

        // view의 artProvider를 재사용하면 캐시를 공유할 수 있지만
        // 현재 artProvider는 CharacterGridViewAdapter 내부 private이므로
        // 별도 인스턴스를 만들어도 Addressables 자체가 동일 key면 같은 handle을 반환함
        // → 사실상 캐시 공유됨 (Addressables의 특성)
        preloadArtProvider ??= new CharacterArtProvider();

        for (int i = 0; i < characters.Count; i++)
        {
            // LoadSprite 호출만 하면 됨 — 결과를 당장 사용하지 않아도
            // 내부적으로 Addressables.LoadAssetAsync가 시작되고 캐시에 저장됨
            preloadArtProvider.LoadSprite(characters[i].Id, CharacterCut.Slot);
        }

        Debug.Log($"[CharacterListPanel] 초상화 프리로드 요청: {characters.Count}개");
    }

    private void OnDestroy()
    {
        // 프리로드용 provider도 정리
        preloadArtProvider?.ReleaseAll();
    }
}
