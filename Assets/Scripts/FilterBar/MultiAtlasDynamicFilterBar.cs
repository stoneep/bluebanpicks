using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class MultiAtlasDynamicFilterBar<T> : BaseFilterBar<T> where T : struct, Enum
{
    [Header("Filter Configuration")]
    [SerializeField] private FilterBarConfig config = new FilterBarConfig();
    
    protected AtlasPreloader atlasPreloader = new AtlasPreloader();
    
    
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
    
    public string FallbackAtlasKey
    {
        get => config.FallbackAtlasKey;
        set => config.FallbackAtlasKey = value;
    }
    
    public string AllButtonKey
    {
        get => config.AllButtonKey;
        set => config.AllButtonKey = value;
    }
    
    protected override bool AllowToggle => config.IncludeAllButton;
    
    
    protected UIIconAtlasService AtlasService => UIIconAtlasService.Instance;
    
    
    protected virtual string GetAllButtonText() => null;
    
    protected abstract string GetAtlasKeyForValue(T value);
    
    protected abstract string GetSpriteName(T value);
    
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
        
        if (buttonMap.Count > 0)
        {
            RefreshVisuals();
            return;
        }
        
        var requiredAtlases = CollectRequiredAtlases();
        atlasPreloader.LoadAtlases(requiredAtlases, () => 
        {
            if (this != null && gameObject.activeInHierarchy) 
                Initialize();
        });
    }
    
    
    private List<string> CollectRequiredAtlases()
    {
        HashSet<string> uniqueAtlases = new HashSet<string>();
        
        if (!string.IsNullOrEmpty(config.FallbackAtlasKey))
        {
            uniqueAtlases.Add(config.FallbackAtlasKey);
        }
        
        foreach (T type in Enum.GetValues(typeof(T)))
        {
            string typeName = type.ToString();
            
            if (typeName.Equals("None", StringComparison.OrdinalIgnoreCase))
                continue;
                
            string atlasKey = GetAtlasKeyForValue(type);
            if (!string.IsNullOrEmpty(atlasKey))
            {
                uniqueAtlases.Add(atlasKey);
            }
        }
        
        Debug.Log($"[{GetType().Name}] 로드할 아틀라스: {string.Join(", ", uniqueAtlases)}");
        return new List<string>(uniqueAtlases);
    }

    protected override void Initialize()
    {
        Debug.Log($"[{GetType().Name}] Initialize 시작 - Config: {config}");
        
        if (buttonMap.Count > 0)
        {
            Debug.LogWarning($"[{GetType().Name}] 이미 초기화됨 - 건너뜀");
            return;
        }
        
        CleanupExisting();
        
        if (config.IncludeAllButton)
        {
            CreateAllButton();
        }
        
        CreateEnumButtons();
        
        MarkAsInitialized();
        RefreshVisuals();
        
        Debug.Log($"[{GetType().Name}] Initialize 완료 - 생성된 버튼: {buttonMap.Count}개");
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
            
            string allText = GetAllButtonText() ?? config.AllButtonKey;
            bool isSelected = !CurrentValue.HasValue;
            
            var mediator = CreateButtonMediator(null);
            
            btn.Setup(allText, null, isSelected, () => OnItemClicked(null), mediator);
            
            allButton = btn;
        }
        catch (Exception e)
        {
            Debug.LogError($"[{GetType().Name}] All 버튼 생성 실패: {e.Message}");
        }
    }
    
    private void CreateEnumButtons()
    {
        foreach (T type in Enum.GetValues(typeof(T)))
        {
            string typeName = type.ToString();
            
            if (typeName.Equals("None", StringComparison.OrdinalIgnoreCase) ||
                typeName.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            
            if (buttonMap.ContainsKey(type))
            {
                Debug.LogError($"[{GetType().Name}] 중복된 타입 발견! {type} - 스킵");
                continue;
            }
                
            CreateButton(type);
        }
    }

    private void CreateButton(T type)
    {
        try
        {
            var btn = Instantiate(buttonPrefab, contentRoot);
            btn.name = $"Filter_{type}";

            string atlasKey = GetAtlasKeyForValue(type);
            string spriteName = GetSpriteName(type);
            
            if (string.IsNullOrEmpty(atlasKey))
            {
                atlasKey = config.FallbackAtlasKey;
                Debug.LogWarning($"[{GetType().Name}] {type}에 대한 아틀라스 키가 없어 폴백 사용: {atlasKey}");
            }
            
            Sprite icon = null;
            if (!string.IsNullOrEmpty(atlasKey) && !string.IsNullOrEmpty(spriteName))
            {
                icon = AtlasService.GetSprite(atlasKey, spriteName);
                
                if (icon == null)
                {
                    Debug.LogWarning($"[{GetType().Name}] 스프라이트 로드 실패: {atlasKey}/{spriteName}");
                }
            }

            bool isSelected = CurrentValue.HasValue && CurrentValue.Value.Equals(type);
            
            var mediator = CreateButtonMediator(type);
            
            btn.Setup(null, icon, isSelected, () => OnItemClicked(type), mediator);
            
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
            CleanupExisting();
            
            var requiredAtlases = CollectRequiredAtlases();
            atlasPreloader.LoadAtlases(requiredAtlases, () => 
            {
                if (this != null && gameObject.activeInHierarchy) 
                    Initialize();
            });
        }
    }
    
    public void SetIncludeAllButton(bool include, bool reinitialize = false)
    {
        config.IncludeAllButton = include;
        
        if (reinitialize && gameObject.activeInHierarchy)
        {
            ApplyConfig(config);
        }
    }
}
