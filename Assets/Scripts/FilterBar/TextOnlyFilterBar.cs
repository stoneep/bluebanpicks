using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 텍스트만 사용하는 FilterBar (아이콘 없음)
/// 
/// 특징:
/// - 메모리 최적화 (아틀라스 로딩 없음)
/// - Enum 자동 순회로 버튼 생성
/// - 확장 용이 (Enum에 값만 추가하면 자동)
/// - FilterBarConfig를 사용하여 All 버튼 유무 제어 가능
/// 
/// 사용 예시:
/// - RefreshCycleTab (Daily/Weekly/6Hour/Monthly)
/// - DifficultyFilterBar (Easy/Normal/Hard)
/// </summary>
public abstract class TextOnlyFilterBar<T> : BaseFilterBar<T> where T : struct, Enum
{
    [Header("Filter Configuration")]
    [SerializeField] public FilterBarConfig config = new FilterBarConfig();
    
    private bool _isInitializing = false;
    private bool _isFullyInitialized = false;
    
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
    /// All 버튼 텍스트 키 (직접 접근용)
    /// </summary>
    public string AllButtonKey
    {
        get => config.AllButtonKey;
        set => config.AllButtonKey = value;
    }
    
    /// <summary>
    /// 자동 선택 설정 (직접 접근용)
    /// </summary>
    public bool AutoSelectFirst
    {
        get => config.AutoSelectFirst;
        set => config.AutoSelectFirst = value;
    }
    
    // ⭐ AllowToggleOff를 IncludeAllButton 기반으로 자동 결정
    /// <summary>
    /// All 버튼이 있으면 토글 해제 허용, 없으면 필수 선택
    /// </summary>
    protected override bool AllowToggle => config.IncludeAllButton;
    
    // ==================== Abstract Methods ====================
    
    /// <summary>
    /// Enum 값의 표시 텍스트 반환 (자식 클래스에서 구현)
    /// </summary>
    protected abstract string GetDisplayText(T value);
    
    /// <summary>
    /// All 버튼에 표시할 텍스트 (null이면 AllButtonKey 사용)
    /// </summary>
    protected virtual string GetAllButtonText() => null;
    
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
        // 이미 초기화되었으면 비주얼만 갱신
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
        // 기존 버튼 정리
        CleanupExisting();
        
        // All 버튼 생성 (설정에 따라)
        if (config.IncludeAllButton)
        {
            CreateAllButton();
        }
        
        // Enum 자동 순회하여 버튼 생성
        var enumValues = Enum.GetValues(typeof(T));
        T? firstValue = null;
        
        foreach (T value in enumValues)
        {
            // All, None 같은 특수 값 제외
            string name = value.ToString();
            if (ShouldSkipValue(name))
                continue;
            
            CreateButton(value);
            
            // 첫 번째 값 기억
            if (firstValue == null)
                firstValue = value;
        }
        
        // 자동 선택 처리
        if (config.AutoSelectFirst)
        {
            if (config.IncludeAllButton)
            {
                // All 버튼이 있으면 All을 선택
                CurrentValue = null;
            }
            else if (firstValue.HasValue)
            {
                // All 버튼이 없으면 첫 번째 값 선택
                CurrentValue = firstValue.Value;
            }
        }
        
        MarkAsInitialized();
        RefreshVisuals();
    }

    /// <summary>
    /// 건너뛸 Enum 값 판단 (기본: All, None)
    /// </summary>
    protected virtual bool ShouldSkipValue(string enumName)
    {
        return enumName.Equals("All", StringComparison.OrdinalIgnoreCase) || 
               enumName.Equals("None", StringComparison.OrdinalIgnoreCase);
    }
    
    /// <summary>
    /// 기존 버튼들 정리
    /// </summary>
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
    
    /// <summary>
    /// All 버튼 생성
    /// </summary>
    private void CreateAllButton()
    {
        try
        {
            var btn = Instantiate(buttonPrefab, contentRoot);
            btn.name = "Filter_All";

            // All 버튼 텍스트 (커스텀 또는 로컬라이제이션 키)
            string allText = GetAllButtonText();
            // if (string.IsNullOrEmpty(allText))
            // {
            //     allText = LocalizationManager.Instance?.GetText(config.AllButtonKey) 
            //               ?? config.AllButtonKey;
            // }

            // Mediator 주입
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
    
    /// <summary>
    /// 개별 버튼 생성
    /// </summary>
    private void CreateButton(T value)
    {
        try
        {
            var btn = Instantiate(buttonPrefab, contentRoot);
            btn.name = $"Filter_{value}";

            string text = GetDisplayText(value);
            bool isSelected = CurrentValue.HasValue && EqualityComparer<T>.Default.Equals(CurrentValue.Value, value);

            // Mediator 주입
            var mediator = CreateButtonMediator(value);

            btn.Setup(text, null, isSelected, () => OnItemClicked(value), mediator);

            // 자식 클래스에서 추가 설정 가능
            OnButtonCreated(btn, value);

            buttonMap[value] = btn;
        }
        catch (Exception e)
        {
            Debug.LogError($"[{GetType().Name}] 버튼 생성 실패 ({value}): {e.Message}");
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
            _isFullyInitialized = false;
            SafeInitialize();
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
            _isFullyInitialized = false;
            SafeInitialize();
        }
    }
}
