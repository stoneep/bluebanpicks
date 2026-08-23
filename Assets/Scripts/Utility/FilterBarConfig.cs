using System;
using UnityEngine;

/// <summary>
/// 필터 바 설정을 관리하는 설정 클래스
/// - Getter/Setter 패턴으로 캡슐화
/// - 명시적인 설정 관리
/// </summary>
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
    
    // ==================== Getters/Setters ====================
    
    /// <summary>
    /// "All" 버튼 포함 여부
    /// </summary>
    public bool IncludeAllButton
    {
        get => includeAllButton;
        set => includeAllButton = value;
    }
    
    /// <summary>
    /// "All" 버튼에 표시할 로컬라이제이션 키
    /// </summary>
    public string AllButtonKey
    {
        get => allButtonKey;
        set => allButtonKey = value ?? "UI_FILTER_ALL";
    }
    
    /// <summary>
    /// 초기화 시 첫 번째 항목 자동 선택 여부
    /// </summary>
    public bool AutoSelectFirst
    {
        get => autoSelectFirst;
        set => autoSelectFirst = value;
    }
    
    /// <summary>
    /// 아틀라스 키 (단일 아틀라스 사용 시)
    /// </summary>
    public string AtlasKey
    {
        get => atlasKey;
        set => atlasKey = value ?? "atlas/icon_combat";
    }
    
    /// <summary>
    /// 폴백 아틀라스 키 (멀티 아틀라스 사용 시)
    /// </summary>
    public string FallbackAtlasKey
    {
        get => fallbackAtlasKey;
        set => fallbackAtlasKey = value ?? "atlas/icon_combat";
    }
    
    // ==================== 프리셋 ====================
    
    /// <summary>
    /// 기본 설정 (All 버튼 포함)
    /// </summary>
    public static FilterBarConfig Default => new FilterBarConfig
    {
        includeAllButton = true,
        allButtonKey = "UI_FILTER_ALL",
        autoSelectFirst = true,
        atlasKey = AtlasAddressConfig.COMBAT
    };
    
    /// <summary>
    /// All 버튼 제외 설정
    /// </summary>
    public static FilterBarConfig WithoutAll => new FilterBarConfig
    {
        includeAllButton = false,
        allButtonKey = "UI_FILTER_ALL",
        autoSelectFirst = true,
        atlasKey = "atlas/icon_combat"
    };
    
    /// <summary>
    /// 커스텀 All 버튼 텍스트
    /// </summary>
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
    
    // ==================== Fluent API ====================
    
    /// <summary>
    /// All 버튼 활성화/비활성화
    /// </summary>
    public FilterBarConfig SetIncludeAllButton(bool include)
    {
        includeAllButton = include;
        return this;
    }
    
    /// <summary>
    /// All 버튼 텍스트 설정
    /// </summary>
    public FilterBarConfig SetAllButtonKey(string key)
    {
        allButtonKey = key ?? "UI_FILTER_ALL";
        return this;
    }
    
    /// <summary>
    /// 아틀라스 키 설정
    /// </summary>
    public FilterBarConfig SetAtlasKey(string key)
    {
        atlasKey = key ?? "atlas/icon_combat";
        return this;
    }
    
    /// <summary>
    /// 자동 선택 설정
    /// </summary>
    public FilterBarConfig SetAutoSelectFirst(bool autoSelect)
    {
        autoSelectFirst = autoSelect;
        return this;
    }
    
    // ==================== 유효성 검증 ====================
    
    /// <summary>
    /// 설정이 유효한지 검증
    /// </summary>
    public bool IsValid()
    {
        if (includeAllButton && string.IsNullOrEmpty(allButtonKey))
            return false;
            
        if (string.IsNullOrEmpty(atlasKey))
            return false;
            
        return true;
    }
    
    /// <summary>
    /// 디버그용 문자열
    /// </summary>
    public override string ToString()
    {
        return $"FilterBarConfig [IncludeAll:{includeAllButton}, AllKey:{allButtonKey}, Atlas:{atlasKey}]";
    }
}
