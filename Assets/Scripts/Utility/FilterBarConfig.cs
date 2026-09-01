using System;
using UnityEngine;

[Serializable]
public class FilterBarConfig
{
    [Header("Button Settings")]
    [SerializeField] private bool includeAllButton = true;
    [SerializeField] private string allButtonKey = "UI_FILTER_ALL";
    [SerializeField] private bool autoSelectFirst = true;
    
    [Header("Atlas Settings")]
    [SerializeField] private string atlasKey = "atlas/icon_combat";
    [SerializeField] private string fallbackAtlasKey = "atlas/icon_combat";
    
    public bool IncludeAllButton
    {
        get => includeAllButton;
        set => includeAllButton = value;
    }
    
    public string AllButtonKey
    {
        get => allButtonKey;
        set => allButtonKey = value ?? "UI_FILTER_ALL";
    }
    
    public bool AutoSelectFirst
    {
        get => autoSelectFirst;
        set => autoSelectFirst = value;
    }
    
    public string AtlasKey
    {
        get => atlasKey;
        set => atlasKey = value ?? "atlas/icon_combat";
    }
    
    public string FallbackAtlasKey
    {
        get => fallbackAtlasKey;
        set => fallbackAtlasKey = value ?? "atlas/icon_combat";
    }
    
    
    public static FilterBarConfig Default => new FilterBarConfig
    {
        includeAllButton = true,
        allButtonKey = "UI_FILTER_ALL",
        autoSelectFirst = true,
        atlasKey = AtlasAddressConfig.COMBAT
    };
    
    public static FilterBarConfig WithoutAll => new FilterBarConfig
    {
        includeAllButton = false,
        allButtonKey = "UI_FILTER_ALL",
        autoSelectFirst = true,
        atlasKey = "atlas/icon_combat"
    };
    
    public static FilterBarConfig WithCustomAllText(string customKey) => new FilterBarConfig
    {
        includeAllButton = true,
        allButtonKey = customKey,
        autoSelectFirst = true,
        atlasKey = "atlas/icon_combat"
    };

    public static FilterBarConfig WithoutAtlas() => new FilterBarConfig
    {
        includeAllButton =  true,
        autoSelectFirst = true
    };
    
    
    public FilterBarConfig SetIncludeAllButton(bool include)
    {
        includeAllButton = include;
        return this;
    }
    
    public FilterBarConfig SetAllButtonKey(string key)
    {
        allButtonKey = key ?? "UI_FILTER_ALL";
        return this;
    }
    
    public FilterBarConfig SetAtlasKey(string key)
    {
        atlasKey = key ?? "atlas/icon_combat";
        return this;
    }
    
    public FilterBarConfig SetAutoSelectFirst(bool autoSelect)
    {
        autoSelectFirst = autoSelect;
        return this;
    }
    
    public bool IsValid()
    {
        if (includeAllButton && string.IsNullOrEmpty(allButtonKey))
            return false;
            
        if (string.IsNullOrEmpty(atlasKey))
            return false;
            
        return true;
    }
    
    public override string ToString()
    {
        return $"FilterBarConfig [IncludeAll:{includeAllButton}, AllKey:{allButtonKey}, Atlas:{atlasKey}]";
    }
}
