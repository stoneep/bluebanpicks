using System;
using UnityEngine;

public abstract class DynamicFilterBar<T> : BaseFilterBar<T> where T : struct, Enum
{
    [Header("Filter Configuration")]
    public FilterBarConfig config = new FilterBarConfig();
    
    private bool _isInitializing = false;
    private bool _isFullyInitialized = false;
        
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
    
    
    protected UIIconAtlasService AtlasService => UIIconAtlasService.Instance;
    
    
    protected virtual string GetAllButtonText() => null;
    
    protected virtual string GetAllButtonSpriteName() => null;
    
    protected abstract string GetSpriteName(T value);
    
    protected virtual string GetDisplayText(T value) => null;
    
    protected virtual IFilterButtonMediator CreateButtonMediator(T? value)
    {
        return FilterButtonMediatorFactory.CreateGrayToggle(Color.white);
    }
    
    protected virtual void OnButtonCreated(UniversalFilterButton btn, T value) { }
    
    
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
            
            var mediator = CreateButtonMediator(null);
            
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
            btn.Setup(displayText, icon, isSelected, () => OnItemClicked(type), mediator);
            OnButtonCreated(btn, type);

            buttonMap[type] = btn;
        }
        catch (Exception e)
        {
            Debug.LogError($"[{GetType().Name}] 버튼 생성 실패 ({type}): {e.Message}");
        }
    }
    
    
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