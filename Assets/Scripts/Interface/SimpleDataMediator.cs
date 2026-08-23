using UnityEngine.UI;

/// <summary>
/// FilterStyleData를 IFilterButtonMediator로 변환하는 어댑터
/// 방안 1을 방안 2 인터페이스에 맞추는 브릿지
/// </summary>
internal class SimpleDataMediator : IFilterButtonMediator
{
    private readonly FilterStyleData styleData;
    
    public SimpleDataMediator(FilterStyleData data)
    {
        styleData = data;
    }
    
    public void ApplyStyle(Image iconImage, Image bgImage, bool isSelected)
    {
        if (isSelected)
        {
            iconImage.color = styleData.IconColorSelected;
            bgImage.color = styleData.BgColorSelected;
        }
        else
        {
            iconImage.color = styleData.IconColorDefault;
            bgImage.color = styleData.BgColorDefault;
        }
    }
}
