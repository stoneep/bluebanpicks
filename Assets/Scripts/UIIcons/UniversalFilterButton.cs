using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 통합된 필터 버튼 (Mediator 패턴 전용)
/// 
/// IFilterButtonMediator만 사용하도록 단순화
/// </summary>
public class UniversalFilterButton : MonoBehaviour
{
    #region UI References
    
    [Header("Core UI Refs")]
    [SerializeField] private Image iconImage;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TMP_Text labelText;
    
    [Header("Control Type")]
    [SerializeField] private Button button;
    
    #endregion
    
    #region Mediator Pattern (IFilterButtonMediator)
    
    private IFilterButtonMediator _styleMediator;
    private AtlasImageBinder _iconBinder;
    private Action _onClickAction;
    
    /// <summary>
    /// Mediator Pattern: 간단한 데이터 전달 방식
    /// </summary>
    public void Initialize(string iconName, FilterStyleData styleData)
    {
        var mediator = new SimpleDataMediator(styleData);
        Initialize(iconName, mediator);
    }
    
    /// <summary>
    /// Mediator Pattern: 중재자 패턴 방식
    /// </summary>
    public void Initialize(string iconName, IFilterButtonMediator mediator)
    {
        _styleMediator = mediator;
        
        // 아이콘 바인더 초기화 (필요시)
        if (_iconBinder == null && !string.IsNullOrEmpty(iconName))
        {
            _iconBinder = new AtlasImageBinder();
            // _iconBinder.Bind(iconImage, atlasKey, iconName);
        }
        
        // 초기 상태 적용
        ApplyMediatorVisualState(false);
    }
    
    /// <summary>
    /// Button 모드로 설정 (아이콘, 텍스트, 클릭 핸들러)
    /// </summary>
    public void Setup(string text, Sprite icon, bool isSelected, Action onClick, IFilterButtonMediator mediator = null)
    {
        _onClickAction = onClick;
        
        // 중재자 설정
        if (mediator != null)
        {
            _styleMediator = mediator;
        }

        // 아이콘 설정
        if (iconImage && icon != null)
        {
            iconImage.sprite = icon;
            iconImage.enabled = true;
        }
        else if (iconImage)
        {
            iconImage.enabled = false;
        }

        // 텍스트 설정
        if (labelText)
        {
            if (!string.IsNullOrEmpty(text))
            {
                labelText.text = text;
                labelText.gameObject.SetActive(true);
            }
            else
            {
                labelText.gameObject.SetActive(false);
            }
        }

        // Button 모드 활성화
        EnableButtonMode();
        SetSelected(isSelected);
    }
    
    private void ApplyMediatorVisualState(bool isSelected)
    {
        _styleMediator?.ApplyStyle(iconImage, backgroundImage, isSelected);
    }
    
    #endregion
    
    #region Selection Interface
    
    /// <summary>
    /// 선택 상태 설정
    /// </summary>
    public void SetSelected(bool isSelected)
    {
        if (_styleMediator != null)
        {
            ApplyMediatorVisualState(isSelected);
        }
    }
    
    #endregion
    
    #region Mode Management
    
    private void EnableButtonMode()
    {
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => _onClickAction?.Invoke());
        }
    }
    
    #endregion
    
    #region Lifecycle
    
    private void Awake()
    {
        // Button이 없으면 자동으로 가져오기
        if (!button) 
            button = GetComponent<Button>();
    }
    
    private void OnDestroy()
    {
        // AtlasImageBinder 정리
        _iconBinder?.Release(iconImage);
    }
    
    #endregion
    
    #region Public Accessors
    
    public Image IconImage => iconImage;
    public Image BackgroundImage => backgroundImage;
    public TMP_Text LabelText => labelText;
    
    #endregion
}
