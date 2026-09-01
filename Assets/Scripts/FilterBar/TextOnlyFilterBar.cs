using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class TextOnlyFilterBar<T> : BaseFilterBar<T> where T : struct, Enum
{
    [Header("Filter Configuration")]
    [SerializeField] public FilterBarConfig config = new FilterBarConfig();
    
    private bool _isInitializing = false;
    private bool _isFullyInitialized = false;
    
    
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
    
    public string AllButtonKey
    {
        get => config.AllButtonKey;
        set => config.AllButtonKey = value;
    }
    
    public bool AutoSelectFirst
    {
        get => config.AutoSelectFirst;
        set => config.AutoSelectFirst = value;
    }
    
    protected override bool AllowToggle => config.IncludeAllButton;
    
    
    protected abstract string GetDisplayText(T value);
    
    protected virtual string GetAllButtonText() => null;
    
    protected virtual IFilterButtonMediator CreateButtonMediator(T? value)
    {
        return FilterButtonMediatorFactory.CreateGrayToggle(Color.white);
    }
    
    protected virtual void OnButtonCreated(UniversalFilterButton btn, T value) { }
    
    
    private void OnEnable()
    {
        if (_isFullyInitialized && buttonMap.Count > 0)
        {
            RefreshVisuals();
            return;
        }
        
        SafeInitialize();
    }
    
    private void SafeInitialize()
    {
        if (_isInitializing)
        {
            Debug.LogWarning($"[{GetType().Name}] 이미 초기화 중입니다");
            return;
        }
        
        _isInitializing = true;
        
        try
        {
            Initialize();
            _isFullyInitialized = true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[{GetType().Name}] 초기화 실패: {e.Message}\n{e.StackTrace}");
            _isFullyInitialized = false;
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
        
        var enumValues = Enum.GetValues(typeof(T));
        T? firstValue = null;
        
        foreach (T value in enumValues)
        {
            string name = value.ToString();
            if (ShouldSkipValue(name))
                continue;
            
            CreateButton(value);
            
            if (firstValue == null)
                firstValue = value;
        }
        
        if (config.AutoSelectFirst)
        {
            if (config.IncludeAllButton)
            {
                CurrentValue = null;
            }
            else if (firstValue.HasValue)
            {
                CurrentValue = firstValue.Value;
            }
        }
        
        MarkAsInitialized();
        RefreshVisuals();
    }
    
    protected virtual bool ShouldSkipValue(string enumName)
    {
        return enumName.Equals("All", StringComparison.OrdinalIgnoreCase) || 
               enumName.Equals("None", StringComparison.OrdinalIgnoreCase);
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
    }
    
    private void CreateAllButton()
    {
        try
        {
            var btn = Instantiate(buttonPrefab, contentRoot);
            btn.name = "Filter_All";
            
            string allText = GetAllButtonText();
            
            var mediator = CreateButtonMediator(null);

            bool isSelected = !CurrentValue.HasValue;
            btn.Setup(allText, null, isSelected, () => OnItemClicked(null), mediator);

            allButton = btn;
        }
        catch (Exception e)
        {
            Debug.LogError($"[{GetType().Name}] All 버튼 생성 실패: {e.Message}");
        }
    }
    
    private void CreateButton(T value)
    {
        try
        {
            var btn = Instantiate(buttonPrefab, contentRoot);
            btn.name = $"Filter_{value}";

            string text = GetDisplayText(value);
            bool isSelected = CurrentValue.HasValue && EqualityComparer<T>.Default.Equals(CurrentValue.Value, value);
            
            var mediator = CreateButtonMediator(value);

            btn.Setup(text, null, isSelected, () => OnItemClicked(value), mediator);
            
            OnButtonCreated(btn, value);

            buttonMap[value] = btn;
        }
        catch (Exception e)
        {
            Debug.LogError($"[{GetType().Name}] 버튼 생성 실패 ({value}): {e.Message}");
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
