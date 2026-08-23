using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 개선된 MultiAtlasDynamicFilterBar - FilterBarConfig를 사용
/// - 여러 아틀라스를 지원하는 동적 필터 바
/// - Getter/Setter 패턴으로 설정 관리
/// </summary>
public abstract class MultiAtlasDynamicFilterBar<T> : BaseFilterBar<T> where T : struct, Enum
{
    [Header("Filter Configuration")]
    [SerializeField] private FilterBarConfig config = new FilterBarConfig();
    
    // AtlasPreloader 사용
    protected AtlasPreloader atlasPreloader = new AtlasPreloader();
    
    // ==================== Properties (Getter/Setter) ====================
    
    /// <summary>
    /// 필터 바 설정 접근자
    /// </summary>
    public FilterBarConfig Config
    {
        get => config;
        set => config = value ?? FilterBarConfig.Default;
    }
    
    /// <summary>
    /// All 버튼 포함 여부 (직접 접근용)
    /// </summary>
    public bool IncludeAllButton
    {
        get => config.IncludeAllButton;
        set => config.IncludeAllButton = value;
    }
    
    /// <summary>
    /// 폴백 아틀라스 키 (직접 접근용)
    /// </summary>
    public string FallbackAtlasKey
    {
        get => config.FallbackAtlasKey;
        set => config.FallbackAtlasKey = value;
    }
    
    /// <summary>
    /// All 버튼 텍스트 키 (직접 접근용)
    /// </summary>
    public string AllButtonKey
    {
        get => config.AllButtonKey;
        set => config.AllButtonKey = value;
    }
    
    // ⭐ AllowToggleOff를 IncludeAllButton 기반으로 자동 결정
    /// <summary>
    /// All 버튼이 있으면 토글 해제 허용, 없으면 필수 선택
    /// </summary>
    protected override bool AllowToggle => config.IncludeAllButton;
    
    // ==================== Services ====================
    
    protected UIIconAtlasService AtlasService => UIIconAtlasService.Instance;
    
    // ==================== Abstract Methods ====================
    
    /// <summary>
    /// All 버튼에 표시할 텍스트 (null이면 AllButtonKey 사용)
    /// </summary>
    protected virtual string GetAllButtonText() => null;
    
    /// <summary>
    /// 특정 Enum 값에 사용할 아틀라스 키 반환 (오버라이드 필수)
    /// </summary>
    protected abstract string GetAtlasKeyForValue(T value);
    
    /// <summary>
    /// 특정 Enum 값에 사용할 스프라이트 이름 반환 (오버라이드 필수)
    /// </summary>
    protected abstract string GetSpriteName(T value);
    
    /// <summary>
    /// 버튼별 Mediator 생성 (선택사항)
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
        
        // 설정 유효성 검증
        if (!config.IsValid())
        {
            Debug.LogError($"[{GetType().Name}] 잘못된 FilterBarConfig: {config}");
            return;
        }
        
        // 이미 버튼이 생성되어 있으면 비주얼만 갱신
        if (buttonMap.Count > 0)
        {
            RefreshVisuals();
            return;
        }
        
        // AtlasPreloader를 사용하여 필요한 아틀라스 로드
        var requiredAtlases = CollectRequiredAtlases();
        atlasPreloader.LoadAtlases(requiredAtlases, () => 
        {
            if (this != null && gameObject.activeInHierarchy) 
                Initialize();
        });
    }
    
    // ==================== Initialization ====================
    
    /// <summary>
    /// 필요한 모든 아틀라스 키를 수집
    /// </summary>
    private List<string> CollectRequiredAtlases()
    {
        HashSet<string> uniqueAtlases = new HashSet<string>();
        
        // 폴백 아틀라스 추가
        if (!string.IsNullOrEmpty(config.FallbackAtlasKey))
        {
            uniqueAtlases.Add(config.FallbackAtlasKey);
        }
        
        // 각 타입별 아틀라스 수집
        foreach (T type in Enum.GetValues(typeof(T)))
        {
            string typeName = type.ToString();
            
            // "None" 같은 특수 타입은 스킵
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
        
        // 중복 초기화 방지
        if (buttonMap.Count > 0)
        {
            Debug.LogWarning($"[{GetType().Name}] 이미 초기화됨 - 건너뜀");
            return;
        }
        
        // 청소
        CleanupExisting();

        // All 버튼 생성
        if (config.IncludeAllButton)
        {
            CreateAllButton();
        }

        // 각 타입별 버튼 생성
        CreateEnumButtons();

        // 초기화 완료
        MarkAsInitialized();
        RefreshVisuals();
        
        Debug.Log($"[{GetType().Name}] Initialize 완료 - 생성된 버튼: {buttonMap.Count}개");
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

            // Config에서 텍스트 가져오기
            string allText = GetAllButtonText() ?? config.AllButtonKey;
            bool isSelected = !CurrentValue.HasValue;
            
            // Mediator 주입
            var mediator = CreateButtonMediator(null);
            
            // All 버튼은 아이콘 없이 텍스트만 사용
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
            
            // "None" 같은 특수 타입은 스킵
            if (typeName.Equals("None", StringComparison.OrdinalIgnoreCase) ||
                typeName.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            
            // 중복 체크
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
            
            // 아틀라스 키가 없으면 폴백 사용
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
            
            // Mediator 주입
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
    
    // ==================== Public API ====================
    
    /// <summary>
    /// 런타임에 설정 변경 후 재초기화
    /// </summary>
    public void ApplyConfig(FilterBarConfig newConfig)
    {
        config = newConfig ?? FilterBarConfig.Default;
        
        if (gameObject.activeInHierarchy)
        {
            // 버튼 클리어 후 재초기화
            CleanupExisting();
            
            // 새로운 아틀라스 로드
            var requiredAtlases = CollectRequiredAtlases();
            atlasPreloader.LoadAtlases(requiredAtlases, () => 
            {
                if (this != null && gameObject.activeInHierarchy) 
                    Initialize();
            });
        }
    }
    
    /// <summary>
    /// All 버튼 표시/숨김 토글
    /// </summary>
    public void SetIncludeAllButton(bool include, bool reinitialize = false)
    {
        config.IncludeAllButton = include;
        
        if (reinitialize && gameObject.activeInHierarchy)
        {
            ApplyConfig(config);
        }
    }
}
