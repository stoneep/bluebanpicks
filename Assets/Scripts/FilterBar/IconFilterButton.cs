using System;
using UnityEngine;
using UnityEngine.UI;

public sealed class IconFilterButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image iconImage;
    [SerializeField] private GameObject selectedHighlight;
    
    public object DataValue { get; private set; }
    
    private Action<IconFilterButton> onClickCallback;

    public void Setup(object data, Sprite icon, Action<IconFilterButton> onClick, bool isSelected)
    {
        this.DataValue = data;
        this.onClickCallback = onClick;
        
        if (iconImage)
        {
            iconImage.sprite = icon;
            iconImage.enabled = (icon != null);
            iconImage.preserveAspect = true;
        }
        
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onClickCallback?.Invoke(this));

        SetSelected(isSelected);
    }

    public void SetSelected(bool isSelected)
    {
        if (selectedHighlight) selectedHighlight.SetActive(isSelected);
    }
}