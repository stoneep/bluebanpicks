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
    /// 아틀라스 키 (직접 접근용)
    /// </summary>
    public string AtlasKey
    {
        get => config.AtlasKey;
        set => config.AtlasKey = value;
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
    
    #endregion
    
    // ==================== Services ====================
    
    protected UIIconAtlasService AtlasService => UIIconAtlasService.Instance;
    
    // ==================== Abstract Methods ====================
    
    /// <summary>
    /// All 버튼에 표시할 텍스트 (null이면 AllButtonKey 사용)
    /// </summary>
    protected virtual string GetAllButtonText() => null;
    
    /// <summary>
    /// 특정 Enum 값의 스프라이트 이름 반환
    /// </summary>
    protected abstract string GetSpriteName(T value);
    
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
        
        // 설정 유효성 검증
        if (!config.IsValid())
        {
            Debug.LogError($"[{GetType().Name}] 잘못된 FilterBarConfig: {config}");
            return;
        }
        
        // 완전히 초기화된 경우 - 비주얼만 갱신
        if (_isFullyInitialized)
        {
            RefreshVisuals();
            return;
        }
        
        // 이미 버튼이 생성되어 있으면 - 초기화 완료 표시 후 비주얼 갱신
        if (buttonMap.Count > 0)
        {
            _isFullyInitialized = true;
            RefreshVisuals();
            return;
        }
        
        // 초기화 진행 중이면 건너뜀
        if (_isInitializing)
        {
            Debug.LogWarning($"[{GetType().Name}] 이미 초기화 진행 중입니다.");
            return;
        }
        
        // 아틀라스 준비 여부 확인 후 초기화
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
    
    /// <summary>
    /// 안전한 초기화 (중복 체크 포함)
    /// </summary>
    private void SafeInitialize()
    {
        // 중복 초기화 방지
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
//        Debug.Log($"[{GetType().Name}] Initialize 시작 - Config: {config}");

        // 1. 안전한 청소
        CleanupExisting();
        
        // 2. All 버튼 생성 (Config 확인)
        if (config.IncludeAllButton)
        {
            CreateAllButton();
        }

        // 3. Enum 버튼들 생성
        CreateEnumButtons();

        // 4. 초기화 완료
        MarkAsInitialized();
        RefreshVisuals();
        
 //       Debug.Log($"[{GetType().Name}] Initialize 완료 - 생성된 버튼: {buttonMap.Count}개");
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
            
            // Config에서 텍스트 가져오기
            string allText = GetAllButtonText() ?? config.AllButtonKey;
            
            btn.Setup(allText, null, CurrentValue == null, () => OnItemClicked(null), mediator);
        
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
            
            // 제외할 타입
            if (typeName.Equals("All", StringComparison.OrdinalIgnoreCase) || 
                typeName.Equals("None", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            
            // 중복 체크
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

            // Mediator 주입
            var mediator = CreateButtonMediator(type);
            
            if (icon == null)
            {
                Debug.LogWarning($"[{GetType().Name}] 스프라이트 없음: {config.AtlasKey}/{spriteName}");
            }

            bool isSelected = CurrentValue.HasValue && CurrentValue.Value.Equals(type);
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
            _isFullyInitialized = false;
            SafeInitialize();
        }
    }
    //
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
