using UnityEngine;

/// <summary>
/// FilterBar가 버튼에게 전달하는 완전한 스타일 명세
/// 색상 결정 권한을 FilterBar에게 집중
/// </summary>
public struct FilterStyleData
{
    // 아이콘 색상
    public Color IconColorDefault;
    public Color IconColorSelected;
    
    // 배경 색상
    public Color BgColorDefault;
    public Color BgColorSelected;
    
    // ⭐ NEW: 텍스트 색상 (선택적 — null이면 변경 안 함)
    public Color? TextColorDefault;
    public Color? TextColorSelected;
    
    // 선택적: 애니메이션 설정
    public float TransitionDuration;
    
    /// <summary>
    /// Gray Toggle 스타일 (아이콘만 회색↔흰색)
    /// </summary>
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
    
    /// <summary>
    /// Icon Color 스타일 (아이콘 색상 변경)
    /// </summary>
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
    
    /// <summary>
    /// Full Color 스타일 (아이콘과 배경 모두 색상 변경)
    /// </summary>
    public static FilterStyleData FullColor(Color themeColor)
    {
        return new FilterStyleData
        {
            IconColorDefault = themeColor * 0.7f, // 약간 어둡게
            IconColorSelected = Color.white,
            BgColorDefault = Color.clear,
            BgColorSelected = themeColor,
            TransitionDuration = 0.2f
        };
    }
}