using System;
using UnityEngine;
using UnityEngine.UI;

public sealed class IconFilterButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image iconImage;
    [SerializeField] private GameObject selectedHighlight;

    // 어떤 데이터가 연결되었는지 저장 (Enum, ID string 등)
    public object DataValue { get; private set; }
    
    // 클릭 시 데이터를 다시 돌려줄 콜백
    private Action<IconFilterButton> onClickCallback;

    public void Setup(object data, Sprite icon, Action<IconFilterButton> onClick, bool isSelected)
    {
        this.DataValue = data;
        this.onClickCallback = onClick;
        
        // 아이콘 설정
        if (iconImage)
        {
            iconImage.sprite = icon;
            iconImage.enabled = (icon != null);
            iconImage.preserveAspect = true;
            // 필요시 Color 초기화: iconImage.color = Color.white;
        }
        
        // 버튼 이벤트 연결
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onClickCallback?.Invoke(this));

        SetSelected(isSelected);
    }

    public void SetSelected(bool isSelected)
    {
        if (selectedHighlight) selectedHighlight.SetActive(isSelected);
    }
}