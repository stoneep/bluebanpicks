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

    [Header("Sort Type Buttons")]
    [SerializeField] private Button raritySortBtn;
    [SerializeField] private Button levelSortBtn;
    [SerializeField] private Button nameSortBtn;
    [SerializeField] private Button affiliationSortBtn;
    [SerializeField] private Button tacticalRoleSortBtn;
    [SerializeField] private Button orderToggleBtn;
    [SerializeField] private RectTransform orderArrowIcon;

    [Header("Control Buttons")]
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private Button backgroundOverlayButton;
    
    public event Action<CharacterFilterContext> OnApply;

    private CharacterFilterContext tempContext;
    private CharacterFilterContext originalContext;

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
        if (cancelButton) cancelButton.onClick.AddListener(OnClickCancel);
        if (backgroundOverlayButton) backgroundOverlayButton.onClick.AddListener(OnClickCancel);
        if (orderToggleBtn) orderToggleBtn.onClick.AddListener(ToggleOrder);
        
    }
    
    public void Open(CharacterFilterContext currentContext)
    {
        tempContext = currentContext;
        originalContext = currentContext;
        
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
    
    private void SetSort(CharacterSortType type)
    {
        tempContext.SortType = type;
        UpdateSortButtonVisuals();
    }
    
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
    }

    public void OnClickConfirm()
    {
        OnApply?.Invoke(tempContext);
        gameObject.SetActive(false);
    }
    
    public void OnClickCancel()
    {
        tempContext = originalContext;
        SyncAllVisuals();
        gameObject.SetActive(false);
    }
    
    public void OnClickReset()
    {
        OnApply?.Invoke(CharacterFilterContext.Default);
        gameObject.SetActive(false);
    }
    
    private void InitializeSortButtons()
    {
        raritySortBtn?.onClick.AddListener(() => SetSort(CharacterSortType.ByRarity));
        levelSortBtn?.onClick.AddListener(() => SetSort(CharacterSortType.ByLevel));
        nameSortBtn?.onClick.AddListener(() => SetSort(CharacterSortType.ByName));
        affiliationSortBtn?.onClick.AddListener(() => SetSort(CharacterSortType.ByAffiliation));
        tacticalRoleSortBtn?.onClick.AddListener(() => SetSort(CharacterSortType.ByTacticalRole));
    }
    
}