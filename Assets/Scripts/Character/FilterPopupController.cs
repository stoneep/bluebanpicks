using UnityEngine;
using System;
using UnityEngine.UI;

public sealed class FilterPopupController : MonoBehaviour
{
    [Header("Filter Bars")]
    [SerializeField] private AffiliationFilterBar affiliationBar;
    [SerializeField] private TacticalRoleFilterBar tacticalRoleBar;
    [SerializeField] private RoleFilterBar roleBar;
    [SerializeField] private AttackTypeFilterBar attackBar;
    [SerializeField] private DefenseTypeFilterBar defenseBar;
    // [SerializeField] private PositionFilterBar positionBar;   // 추가

    [Header("Sort Type Buttons")]
    [SerializeField] private Button raritySortBtn;
    [SerializeField] private Button levelSortBtn;
    [SerializeField] private Button nameSortBtn;
    [SerializeField] private Button affiliationSortBtn;
    [SerializeField] private Button tacticalRoleSortBtn;
    [SerializeField] private Button orderToggleBtn;
    [SerializeField] private RectTransform orderArrowIcon; // 화살표 아이콘 (회전용)

    [Header("Control Buttons")]
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button resetButton;
    
    public event Action<CharacterFilterContext> OnApply;

    private CharacterFilterContext tempContext;

    private void Awake()
    {
        InitializeSortButtons();
        if (affiliationBar) affiliationBar.OnValueChanged += (val) => tempContext.Affiliation = val;
        if (tacticalRoleBar) tacticalRoleBar.OnValueChanged += (val) => tempContext.TacticalRole = val;
        if (roleBar) roleBar.OnValueChanged += (v) => tempContext.Role = v;
        if (attackBar) attackBar.OnValueChanged += (v) => tempContext.AttackType = v;
        if (defenseBar) defenseBar.OnValueChanged += (v) => tempContext.DefenseType = v;

        if (confirmButton) confirmButton.onClick.AddListener(OnClickConfirm);
        if (resetButton) resetButton.onClick.AddListener(OnClickReset);
        if (orderToggleBtn) orderToggleBtn.onClick.AddListener(ToggleOrder);
        
    }
    
    public void Open(CharacterFilterContext currentContext)
    {
        tempContext = currentContext;
        
        SyncAllVisuals();
        
        gameObject.SetActive(true); 
    }
    
    private void SyncAllVisuals()
    {
        if (affiliationBar) affiliationBar.SyncVisual(tempContext.Affiliation);
        if (tacticalRoleBar) tacticalRoleBar.SyncVisual(tempContext.TacticalRole);
        if (roleBar) roleBar.SyncVisual(tempContext.Role);
        if (attackBar) attackBar.SyncVisual(tempContext.AttackType);
        if (defenseBar) defenseBar.SyncVisual(tempContext.DefenseType);
        
        UpdateOrderUI();
        UpdateSortButtonVisuals(); 
    }
    
    // 정렬 타입 변경
    private void SetSort(CharacterSortType type)
    {
        tempContext.SortType = type;
        UpdateSortButtonVisuals();
    }
    
    // 정렬 순서 토글
    private void ToggleOrder()
    {
        tempContext.SortOrder = (tempContext.SortOrder == SortOrder.Ascending) 
            ? SortOrder.Descending 
            : SortOrder.Ascending;
        UpdateOrderUI();
    }

    private void UpdateOrderUI()
    {
        if (orderArrowIcon)
        {
            float angle = (tempContext.SortOrder == SortOrder.Ascending) ? 0f : 180f;
            orderArrowIcon.localRotation = Quaternion.Euler(0, 0, angle);
        }
    }
    
    private void UpdateSortButtonVisuals()
    {
        // UIStylePalette 등을 사용하여 버튼 색상 변경 로직
        // 예: raritySortBtn.image.color = (tempContext.SortType == ByRarity) ? Selected : Normal;
    }

    public void OnClickConfirm()
    {
        // 완성된 박스를 배달
        OnApply?.Invoke(tempContext);
        gameObject.SetActive(false);
    }
    
    public void OnClickReset()
    {
        // 기본값 박스를 배달
        OnApply?.Invoke(CharacterFilterContext.Default);
        gameObject.SetActive(false);
    }
    
    private void InitializeSortButtons()
    {
        // 람다식으로 깔끔하게 연결
        raritySortBtn?.onClick.AddListener(() => SetSort(CharacterSortType.ByRarity));
        levelSortBtn?.onClick.AddListener(() => SetSort(CharacterSortType.ByLevel));
        nameSortBtn?.onClick.AddListener(() => SetSort(CharacterSortType.ByName));
        affiliationSortBtn?.onClick.AddListener(() => SetSort(CharacterSortType.ByAffiliation));
        tacticalRoleSortBtn?.onClick.AddListener(() => SetSort(CharacterSortType.ByTacticalRole));
    }
    
}