using UnityEngine;

public struct FilterStyleData
{
    public Color IconColorDefault;
    public Color IconColorSelected;
    
    public Color BgColorDefault;
    public Color BgColorSelected;
    
    public Color? TextColorDefault;
    public Color? TextColorSelected;
    
    public float TransitionDuration;
    
    public static FilterStyleData GrayToggle(Color selectedBgColor)
    {
        return new FilterStyleData
        {
            IconColorDefault = Color.gray,
            IconColorSelected = Color.white,
            BgColorDefault = Color.clear,
            BgColorSelected = selectedBgColor,
            TransitionDuration = 0.2f
        };
    }
    
    public static FilterStyleData IconColor(Color iconColor, Color selectedBgColor)
    {
        return new FilterStyleData
        {
            IconColorDefault = iconColor,
            IconColorSelected = iconColor,
            BgColorDefault = Color.clear,
            BgColorSelected = selectedBgColor,
            TransitionDuration = 0.2f
        };
    }
    
    public static FilterStyleData FullColor(Color themeColor)
    {
        return new FilterStyleData
        {
            IconColorDefault = themeColor * 0.7f,
            IconColorSelected = Color.white,
            BgColorDefault = Color.clear,
            BgColorSelected = themeColor,
            TransitionDuration = 0.2f
        };
    }
}