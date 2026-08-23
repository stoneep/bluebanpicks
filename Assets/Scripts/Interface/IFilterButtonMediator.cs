using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 방안 2: 진정한 중재자 패턴
/// - FilterBar와 Style 사이의 중재자
/// - 색상 결정 로직을 캡슐화
/// </summary>
public interface IFilterButtonMediator
{
    void ApplyStyle(Image iconImage, Image bgImage, bool isSelected);
}

/// <summary>
/// Gray Toggle 중재자
/// - 아이콘: 회색 ↔ 흰색
/// - 배경: 투명 ↔ 지정 색상
/// </summary>
public class GrayToggleMediator : IFilterButtonMediator
{
    private readonly Color assignedColor;
    
    public GrayToggleMediator(Color assignedColor)
    {
        this.assignedColor = assignedColor;
    }
    
    public void ApplyStyle(Image iconImage, Image bgImage, bool isSelected)
    {
        // 배경: 항상 고정 색상
        if (bgImage != null)
        {
            bgImage.color = assignedColor;
        }
        
        // 아이콘: 투명도로 선택 표현
        if (iconImage != null)
        {
            float alpha = isSelected ? 1.0f : 0.4f;
            iconImage.color = new Color(1f, 1f, 1f, alpha);
        }
    }
}

public class TextToggleMediator : IFilterButtonMediator
{
    private readonly Color defaultColor = Palette.AzureishWhite;
    private readonly Color selectedColor = Palette.SemiBlack;
    
    public void ApplyStyle(Image iconImage, Image bgImage, bool isSelected)
    {
        if (bgImage != null)
        {
            bgImage.color = isSelected ? selectedColor : defaultColor;
        }
    }
}

/// <summary>
/// Icon Color 중재자
/// - 아이콘: 고정 색상 (테마 색상)
/// - 배경: 투명 ↔ 테마 색상
/// </summary>
public class IconColorMediator : IFilterButtonMediator
{
    private readonly Color iconColor;
    private readonly Color selectedBgColor;
    
    public IconColorMediator(Color iconColor, Color selectedBgColor)
    {
        this.iconColor = iconColor;
        this.selectedBgColor = selectedBgColor;
    }
    
    public void ApplyStyle(Image iconImage, Image bgImage, bool isSelected)
    {
        iconImage.color = iconColor;
        bgImage.color = isSelected ? selectedBgColor : Color.clear;
    }
}

/// <summary>
/// Full Color 중재자
/// - 아이콘: 어두운 테마 색상 ↔ 흰색
/// - 배경: 투명 ↔ 테마 색상
/// </summary>
public class FullColorMediator : IFilterButtonMediator
{
    private readonly Color themeColor;
    private readonly Color darkIconColor;
    
    public FullColorMediator(Color themeColor)
    {
        this.themeColor = themeColor;
        this.darkIconColor = themeColor * 0.7f;
    }
    
    public void ApplyStyle(Image iconImage, Image bgImage, bool isSelected)
    {
        iconImage.color = isSelected ? Color.white : darkIconColor;
        bgImage.color = isSelected ? themeColor : Color.clear;
    }
}


/// <summary>
/// ⭐ NEW: Icon Gray Toggle 중재자
/// - 아이콘: 회색 ↔ 흰색
/// - 배경: 변경 없음 (투명 유지)
/// - 사용처: DefenseType, AttackType 필터
/// </summary>
public class IconGrayToggleMediator : IFilterButtonMediator
{
    private readonly Color normalIconColor = Palette.SemiBlack;
    private readonly Color selectedIconColor = Palette.AzureishWhite;
    
    public void ApplyStyle(Image iconImage, Image bgImage, bool isSelected)
    {
        // 아이콘: 선택 여부에 따라 색상 변경
        if (iconImage != null)
        {
            iconImage.color = isSelected ? selectedIconColor : normalIconColor;
        }
        
        // 배경: 변경 없음 (제어하지 않음)
        // if (bgImage != null)
        // {
        //     bgImage.color = Color.clear;
        // }
    }
}

/// <summary>
/// - 아이콘: 변경없음
/// - 배경: 하얀색 - 회색
/// - 사용처: AffiliationFilterBar
/// </summary>

public class WhiteBgGrayMediator : IFilterButtonMediator
{
    private static readonly Color WHITE = Palette.AzureishWhite;
    private static readonly Color GRAY = Palette.MenuDarkBlue;
    
    public void ApplyStyle(Image iconImage, Image bgImage, bool isSelected)
    {
        // if (iconImage != null)
        // {
        //     iconImage.color = isSelected ? WHITE : BLACK;
        // }
        
        if (bgImage != null)
        {
            bgImage.color = isSelected ? GRAY : WHITE;
        }
    }
}

/// <summary>
/// ⭐ NEW: Black Icon Gray Background 중재자
/// - 미선택: 검정 아이콘 + 회색 배경 /아이콘이 카테고리 글자
/// - 선택: 흰색 아이콘 + 검정 배경
/// - 사용처: ItemCategory 필터
/// </summary>
public class BlackIconGrayBgMediator : IFilterButtonMediator
{
    private static readonly Color BLACK = Color.black;
    private static readonly Color WHITE = Palette.AzureishWhite;
    private static readonly Color GRAY = Palette.MenuDarkBlue;
    
    public void ApplyStyle(Image iconImage, Image bgImage, bool isSelected)
    {
        if (iconImage != null)
        {
            iconImage.color = isSelected ? WHITE : BLACK;
        }
        
        if (bgImage != null)
        {
            bgImage.color = isSelected ? BLACK : GRAY;
        }
    }
}

/// <summary>
/// ⭐ NEW: Icon-Background Color Swap 중재자
/// - 미선택: 아이콘=타입 색상, 배경=흰색
/// - 선택: 아이콘=흰색, 배경=타입 색상
/// - 사용처: TacticalRoleFilterBar
/// </summary>
public class IconBgColorSwapMediator : IFilterButtonMediator
{
    private readonly Color themeColor;
    private static readonly Color WHITE = Color.white;
    
    public IconBgColorSwapMediator(Color themeColor)
    {
        this.themeColor = themeColor;
    }
    
    public void ApplyStyle(Image iconImage, Image bgImage, bool isSelected)
    {
        if (isSelected)
        {
            // 선택: 아이콘 흰색, 배경 타입 색상
            if (iconImage != null)
                iconImage.color = WHITE;
            
            if (bgImage != null)
                bgImage.color = themeColor;
        }
        else
        {
            // 미선택: 아이콘 타입 색상, 배경 흰색
            if (iconImage != null)
                iconImage.color = themeColor;
            
            if (bgImage != null)
                bgImage.color = WHITE;
        }
    }
}

/// <summary>
/// ⭐ NEW: Generic Icon-Background Color Swap 중재자
/// - Enum 타입의 GetThemeColor() 확장 메서드를 직접 호출
/// - 미선택: 아이콘=타입 색상, 배경=흰색
/// - 선택: 아이콘=흰색, 배경=타입 색상
/// - 사용처: TacticalRoleFilterBar, RoleFilterBar 등
/// </summary>
public class GenericIconBgSwapMediator<T> : IFilterButtonMediator where T : struct, System.Enum
{
    private readonly T enumValue;
    private static readonly Color WHITE = Palette.AzureishWhite;
    
    public GenericIconBgSwapMediator(T value)
    {
        this.enumValue = value;
    }
    
    public void ApplyStyle(Image iconImage, Image bgImage, bool isSelected)
    {
        // ⭐ 런타임에 확장 메서드 호출하여 색상 가져오기
        Color themeColor = GetThemeColorDynamic();
        
        if (isSelected)
        {
            // 선택: 아이콘 흰색, 배경 타입 색상
            if (iconImage != null)
                iconImage.color = WHITE;
            
            if (bgImage != null)
                bgImage.color = themeColor;
        }
        else
        {
            // 미선택: 아이콘 타입 색상, 배경 흰색
            if (iconImage != null)
                iconImage.color = themeColor;
            
            if (bgImage != null)
                bgImage.color = WHITE;
        }
    }
    
    /// <summary>
    /// 동적으로 확장 메서드 호출 (리플렉션 없이 타입 체크)
    /// </summary>
    private Color GetThemeColorDynamic()
    {
        // TacticalRole인 경우
        if (enumValue is TacticalRole role)
            return role.GetThemeColor();
        
        // Role인 경우
        if (enumValue is Role roleType)
            return roleType.GetThemeColor();
        
        // AttackType인 경우
        if (enumValue is AttackType attackType)
            return attackType.GetThemeColor();
        
        // DefenseType인 경우
        if (enumValue is DefenseType defenseType)
            return defenseType.GetThemeColor();
        
        // 폴백: 흰색
        return Color.white;
    }
}




/// <summary>
/// 중재자 팩토리
/// </summary>
public static class FilterButtonMediatorFactory
{
    public static IFilterButtonMediator CreateGrayToggle(Color selectedBgColor) 
        => new GrayToggleMediator(selectedBgColor);

    public static IFilterButtonMediator CreateIconColor(Color iconColor, Color selectedBgColor) 
        => new IconColorMediator(iconColor, selectedBgColor);

    public static IFilterButtonMediator CreateFullColor(Color themeColor) 
        => new FullColorMediator(themeColor);

    /// <summary>
    /// ⭐ NEW: 아이콘만 회색↔흰색 토글 (배경 변경 없음)
    /// DefenseType, AttackType 필터에서 사용
    /// </summary>
    public static IFilterButtonMediator CreateIconGrayToggle() 
        => new IconGrayToggleMediator();

    public static IFilterButtonMediator TextToggleMediator() 
        => new TextToggleMediator();

    /// <summary>
    /// ⭐ NEW: 검정 아이콘/회색 배경 → 흰색 아이콘/검정 배경
    /// ItemCategory 필터에서 사용
    /// </summary>
    public static IFilterButtonMediator CreateBlackIconGrayBg() 
        => new BlackIconGrayBgMediator();

    /// <summary>
    /// ⭐ NEW: 배경만 회색↔흰색 토글 (아이콘 변경 없음)
    /// - 사용처: AffiliationFilterBar
    /// </summary>
    public static IFilterButtonMediator CreateWhiteGrayBg() 
        => new WhiteBgGrayMediator();

    /// <summary>
    /// ⭐ NEW: 아이콘-배경 색상 교환
    /// TacticalRole 필터에서 사용
    /// </summary>
    public static IFilterButtonMediator CreateIconBgColorSwap(Color themeColor) 
        => new IconBgColorSwapMediator(themeColor);

    /// <summary>
    /// ⭐ NEW: Generic 아이콘-배경 색상 교환 (확장 메서드 자동 호출)
    /// </summary>
    public static IFilterButtonMediator CreateGenericIconBgSwap<T>(T enumValue) where T : struct, System.Enum 
        => new GenericIconBgSwapMediator<T>(enumValue);
}
