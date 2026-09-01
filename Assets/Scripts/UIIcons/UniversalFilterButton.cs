using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
    
    public void Initialize(string iconName, FilterStyleData styleData)
    {
        var mediator = new SimpleDataMediator(styleData);
        Initialize(iconName, mediator);
    }
    
    public void Initialize(string iconName, IFilterButtonMediator mediator)
    {
        _styleMediator = mediator;
        
        if (_iconBinder == null && !string.IsNullOrEmpty(iconName))
        {
            _iconBinder = new AtlasImageBinder();
        }
        
        ApplyMediatorVisualState(false);
    }
    
    public void Setup(string text, Sprite icon, bool isSelected, Action onClick, IFilterButtonMediator mediator = null)
    {
        _onClickAction = onClick;
        
        if (mediator != null)
        {
            _styleMediator = mediator;
        }
        
        if (iconImage && icon != null)
        {
            iconImage.sprite = icon;
            iconImage.enabled = true;
        }
        else if (iconImage)
        {
            iconImage.enabled = false;
        }
        
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
        
        EnableButtonMode();
        SetSelected(isSelected);
    }
    
    private void ApplyMediatorVisualState(bool isSelected)
    {
        _styleMediator?.ApplyStyle(iconImage, backgroundImage, labelText, isSelected);
    }
    
    #endregion
    
    #region Selection Interface
    
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
        if (!button) 
            button = GetComponent<Button>();
        if (button != null)
            button.transition = Selectable.Transition.None;
    }
    
    private void OnDestroy()
    {
        _iconBinder?.Release(iconImage);
    }
    
    #endregion
    
    #region Public Accessors
    
    public Image IconImage => iconImage;
    public Image BackgroundImage => backgroundImage;
    public TMP_Text LabelText => labelText;
    
    #endregion
}
