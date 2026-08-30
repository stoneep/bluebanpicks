using System;
using UnityEngine;

/// <summary>
/// 개선된 DynamicFilterBar - FilterBarConfig를 사용
/// - Getter/Setter 패턴으로 설정 관리
/// - 명시적이고 확장 가능한 구조
/// </summary>
public abstract class DynamicFilterBar<T> : BaseFilterBar<T> where T : struct, Enum
{
    [Header("Filter Configuration")]
    public FilterBarConfig config = new FilterBarConfig();
    
    private bool _isInitializing = false;
    private bool _isFullyInitialized = false;
    
    // ==================== Properties (Getter/Setter) ====================
    #region Properties
    
    public FilterBarConfig Config
    {
        get => config;
        set => config = value ?? FilterBarConfig.Default;
    }
    
    public bool IncludeAllButton
    {
        get => config.IncludeAllButton;
        set => config.IncludeAllButton = value;
    }
    
    public string AtlasKey
    {
        get => config.AtlasKey;
        set => config.AtlasKey = value;
    }
    
    public string AllButtonKey
    {
        get => config.AllButtonKey;
        set => config.AllButtonKey = value;
    }
    
    protected override bool AllowToggle => config.IncludeAllButton;
    
    #endregion
    
    // ==================== Services ====================
    
    protected UIIconAtlasService AtlasService => UIIconAtlasService.Instance;
    
    // ==================== Abstract Methods ====================
    
    /// <summary>
    /// All 버튼에 표시할 텍스트 (null이면 AllButtonKey 사용)
    /// </summary>
    protected virtual string GetAllButtonText() => null;
    
    /// <summary>
    /// All 버튼을 이미지로 표시하고 싶을 때 오버라이드.
    /// null이 아니면 config.AtlasKey에서 해당 이름의 스프라이트를 찾아 아이콘으로 사용하고,
    /// 이 경우 텍스트는 자동으로 숨김 처리됩니다.
    /// </summary>
    protected virtual string GetAllButtonSpriteName() => null;
    
    /// <summary>
    /// 특정 Enum 값의 스프라이트 이름 반환
    /// </summary>
    protected abstract string GetSpriteName(T value);
    
    /// <summary>
    /// 개별 버튼에 표시할 텍스트 (선택사항)
    /// null이면 기존처럼 아이콘만 표시. 값을 반환하면 아이콘+텍스트 동시 표시.
    /// </summary>
    protected virtual string GetDisplayText(T value) => null;
    
    /// <summary>
    /// 버튼별 Mediator 생성 (자식 클래스에서 오버라이드)
    /// </summary>
    protected virtual IFilterButtonMediator CreateButtonMediator(T? value)
    {
        return FilterButtonMediatorFactory.CreateGrayToggle(Color.white);
    }
    
    /// <summary>
    /// 버튼 생성 후 추가 설정 (선택사항)
    /// </summary>
    protected virtual void OnButtonCreated(UniversalFilterButton btn, T value) { }
    
    // ==================== Lifecycle ====================
    
    private void OnEnable()
    {
        if (AtlasService == null) 
        {
            Debug.LogError($"[{GetType().Name}] UIIconAtlasService가 없습니다!");
            return;
        }
        
        if (!config.IsValid())
        {
            Debug.LogError($"[{GetType().Name}] 잘못된 FilterBarConfig: {config}");
            return;
        }
        
        if (_isFullyInitialized)
        {
            RefreshVisuals();
            return;
        }
        
        if (buttonMap.Count > 0)
        {
            _isFullyInitialized = true;
            RefreshVisuals();
            return;
        }
        
        if (_isInitializing)
        {
            Debug.LogWarning($"[{GetType().Name}] 이미 초기화 진행 중입니다.");
            return;
        }
        
        if (AtlasService.IsAtlasReady(config.AtlasKey))
        {
            SafeInitialize();
        }
        else
        {
            _isInitializing = true;
            var handle = AtlasService.LoadAtlas(config.AtlasKey);
            handle.Completed += _ => 
            {
                _isInitializing = false;
                if (this != null && gameObject.activeInHierarchy)
                {
                    SafeInitialize();
                }
            };
        }
    }
    
    // ==================== Initialization ====================
    
    private void SafeInitialize()
    {
        if (_isFullyInitialized || buttonMap.Count > 0)
        {
            Debug.Log($"[{GetType().Name}] 이미 초기화됨 - 건너뜀");
            _isFullyInitialized = true;
            return;
        }
        
        if (_isInitializing)
        {
            Debug.LogWarning($"[{GetType().Name}] 초기화 진행 중 - 건너뜀");
            return;
        }
        
        try
        {
            _isInitializing = true;
            Initialize();
            _isFullyInitialized = true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[{GetType().Name}] 초기화 실패: {e.Message}\n{e.StackTrace}");
        }
        finally
        {
            _isInitializing = false;
        }
    }
    
    protected override void Initialize()
    {
        CleanupExisting();
        
        if (config.IncludeAllButton)
        {
            CreateAllButton();
        }

        CreateEnumButtons();

        MarkAsInitialized();
        RefreshVisuals();
    }
    
    // ==================== Button Creation ====================
    
    private void CleanupExisting()
    {
        if (contentRoot != null)
        {
            foreach (Transform child in contentRoot)
            {
                if (child != null)
                    Destroy(child.gameObject);
            }
        }
        
        buttonMap.Clear();
        allButton = null;
    }
    
    private void CreateAllButton()
    {
        try
        {
            var btn = Instantiate(buttonPrefab, contentRoot);
            btn.name = "Filter_All";
        
            // Mediator 주입 (All 버튼용)
            var mediator = CreateButtonMediator(null);
            
            // 이미지(아이콘) 우선 확인
            string allSpriteName = GetAllButtonSpriteName();
            Sprite allIcon = null;
            
            if (!string.IsNullOrEmpty(allSpriteName))
            {
                allIcon = AtlasService.GetSprite(config.AtlasKey, allSpriteName);
                if (allIcon == null)
                {
                    Debug.LogWarning($"[{GetType().Name}] All 버튼 스프라이트 없음: {config.AtlasKey}/{allSpriteName}");
                }
            }
            
            // 아이콘이 있으면 텍스트는 숨기고, 없으면 기존처럼 텍스트만 표시
            string allText = allIcon != null ? null : (GetAllButtonText() ?? config.AllButtonKey);
            
            btn.Setup(allText, allIcon, CurrentValue == null, () => OnItemClicked(null), mediator);
        
            allButton = btn;
        }
        catch (Exception e)
        {
            Debug.LogError($"[{GetType().Name}] All 버튼 생성 실패: {e.Message}");
        }
    }
    
    private void CreateEnumButtons()
    {
        var enumValues = Enum.GetValues(typeof(T));
        
        foreach (T type in enumValues)
        {
            string typeName = type.ToString();
            
            if (typeName.Equals("All", StringComparison.OrdinalIgnoreCase) || 
                typeName.Equals("None", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            
            if (buttonMap.ContainsKey(type))
            {
                Debug.LogError($"[{GetType().Name}] 중복된 타입 발견! {type} - 스킵");
                continue;
            }
            
            CreateTypeButton(type);
        }
    }
    
    private void CreateTypeButton(T type)
    {
        try
        {
            var btn = Instantiate(buttonPrefab, contentRoot);
            btn.name = $"Filter_{type}";

            string spriteName = GetSpriteName(type);
            Sprite icon = AtlasService.GetSprite(config.AtlasKey, spriteName);
            string displayText = GetDisplayText(type);
            
            var mediator = CreateButtonMediator(type);
            
            if (icon == null)
            {
                Debug.LogWarning($"[{GetType().Name}] 스프라이트 없음: {config.AtlasKey}/{spriteName}");
            }

            bool isSelected = CurrentValue.HasValue && CurrentValue.Value.Equals(type);
            //btn.Setup(null, icon, isSelected, () => OnItemClicked(type), mediator);
            btn.Setup(displayText, icon, isSelected, () => OnItemClicked(type), mediator);
            OnButtonCreated(btn, type);

            buttonMap[type] = btn;
        }
        catch (Exception e)
        {
            Debug.LogError($"[{GetType().Name}] 버튼 생성 실패 ({type}): {e.Message}");
        }
    }
    
    // ==================== Public API ====================
    
    public void ApplyConfig(FilterBarConfig newConfig)
    {
        config = newConfig ?? FilterBarConfig.Default;
        
        if (gameObject.activeInHierarchy)
        {
            _isFullyInitialized = false;
            SafeInitialize();
        }
    }
    
    public void SetIncludeAllButton(bool include, bool reinitialize = false)
    {
        config.IncludeAllButton = include;
        
        if (reinitialize && gameObject.activeInHierarchy)
        {
            _isFullyInitialized = false;
            SafeInitialize();
        }
    }
}