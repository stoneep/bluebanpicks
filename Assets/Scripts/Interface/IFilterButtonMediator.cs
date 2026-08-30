using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 방안 2: 진정한 중재자 패턴
/// - FilterBar와 Style 사이의 중재자
/// - 색상 결정 로직을 캡슐화
/// </summary>
public interface IFilterButtonMediator
{
    void ApplyStyle(Image iconImage, Image bgImage, TMP_Text labelText, bool isSelected);
}

/// <summary>
/// Gray Toggle 중재자
/// - 아이콘: 회색 ↔ 흰색
/// - 배경: 투명 ↔ 지정 색상
/// - 텍스트: (선택) 지정 시 선택/미선택 색상 적용, 미지정 시 기존 색 유지
/// </summary>
public class GrayToggleMediator : IFilterButtonMediator
{
    private readonly Color assignedColor;
    private readonly Color? textColorDefault;
    private readonly Color? textColorSelected;

    public GrayToggleMediator(Color assignedColor, Color? textColorDefault = null, Color? textColorSelected = null)
    {
        this.assignedColor = assignedColor;
        this.textColorDefault = textColorDefault;
        this.textColorSelected = textColorSelected;
    }

    public void ApplyStyle(Image iconImage, Image bgImage, TMP_Text labelText, bool isSelected)
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

        // 텍스트: 색상이 지정된 경우에만 적용 (하위 호환)
        if (labelText != null)
        {
            if (isSelected && textColorSelected.HasValue)
                labelText.color = textColorSelected.Value;
            else if (!isSelected && textColorDefault.HasValue)
                labelText.color = textColorDefault.Value;
        }
    }
}

public class TextToggleMediator : IFilterButtonMediator
{
    private readonly Color defaultColor = Palette.searchWhite;
    private readonly Color selectedColor = Palette.SemiBlack;

    public void ApplyStyle(Image iconImage, Image bgImage, TMP_Text labelText, bool isSelected)
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

    public void ApplyStyle(Image iconImage, Image bgImage, TMP_Text labelText, bool isSelected)
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

    public void ApplyStyle(Image iconImage, Image bgImage, TMP_Text labelText, bool isSelected)
    {
        iconImage.color = isSelected ? Color.white : darkIconColor;
        bgImage.color = isSelected ? themeColor : Color.clear;
    }
}


/// <summary>
/// ⭐ Icon Gray Toggle 중재자
/// - 아이콘: 회색 ↔ 흰색
/// - 배경: 변경 없음 (투명 유지)
/// - 사용처: DefenseType, AttackType 필터
/// </summary>
public class IconGrayToggleMediator : IFilterButtonMediator
{
    private readonly Color normalIconColor = Palette.SemiBlack;
    private readonly Color selectedIconColor = Palette.searchWhite;

    public void ApplyStyle(Image iconImage, Image bgImage, TMP_Text labelText, bool isSelected)
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
    private static readonly Color WHITE = Palette.OffWhite;
    private static readonly Color deeblue = Palette.DeepBlue;

    public void ApplyStyle(Image iconImage, Image bgImage, TMP_Text labelText, bool isSelected)
    {
        // if (iconImage != null)
        // {
        //     iconImage.color = isSelected ? WHITE : BLACK;
        // }

        if (bgImage != null)
        {
            bgImage.color = isSelected ? deeblue : WHITE;
        }
    }
}

/// <summary>
/// ⭐ Black Icon Gray Background 중재자
/// - 미선택: 검정 아이콘  회색 배경 /아이콘이 카테고리 글자
/// - 선택: 흰색 아이콘  검정 배경
/// - 사용처: ItemCategory 필터
/// </summary>
public class BlackIconGrayBgMediator : IFilterButtonMediator
{
    private static readonly Color BLACK = Color.black;
    private static readonly Color WHITE = Palette.searchWhite;
    private static readonly Color GRAY = Palette.MenuDarkBlue;

    public void ApplyStyle(Image iconImage, Image bgImage, TMP_Text labelText, bool isSelected)
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
/// ⭐ Icon-Background Color Swap 중재자
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

    public void ApplyStyle(Image iconImage, Image bgImage, TMP_Text labelText, bool isSelected)
    {
        if (isSelected)
        {
            if (iconImage != null)
                iconImage.color = WHITE;

            if (bgImage != null)
                bgImage.color = themeColor;
        }
        else
        {
            if (iconImage != null)
                iconImage.color = themeColor;

            if (bgImage != null)
                bgImage.color = WHITE;
        }
    }
}

/// <summary>
/// ⭐ Text-Background Color Swap 중재자 (아이콘 없는 버튼 전용)
/// - 미선택: 텍스트=지정 색상, 배경=흰색
/// - 선택: 텍스트=흰색, 배경=지정 색상
/// - 아이콘은 건드리지 않음 (아이콘이 아예 없는 버튼, 예: All 버튼용)
/// </summary>
public class TextBgSwapMediator : IFilterButtonMediator
{
    private readonly Color themeColor;
    private static readonly Color WHITE = Palette.searchWhite;

    public TextBgSwapMediator(Color themeColor)
    {
        this.themeColor = themeColor;
    }

    public void ApplyStyle(Image iconImage, Image bgImage, TMP_Text labelText, bool isSelected)
    {
        if (isSelected)
        {
            if (labelText != null) labelText.color = WHITE;
            if (bgImage != null) bgImage.color = themeColor;
        }
        else
        {
            if (labelText != null) labelText.color = themeColor;
            if (bgImage != null) bgImage.color = WHITE;
        }
        // iconImage는 의도적으로 건드리지 않음
    }
}

/// <summary>
/// ⭐ Generic Icon-Background Color Swap 중재자
/// - Enum 타입의 GetThemeColor() 확장 메서드를 직접 호출
/// - 미선택: 아이콘=타입 색상, 배경=흰색
/// - 선택: 아이콘=흰색, 배경=타입 색상
/// - 사용처: TacticalRoleFilterBar, RoleFilterBar 등
/// </summary>
public class GenericIconBgSwapMediator<T> : IFilterButtonMediator where T : struct, System.Enum
{
    private readonly T enumValue;
    private static readonly Color WHITE = Palette.searchWhite;

    public GenericIconBgSwapMediator(T value)
    {
        this.enumValue = value;
    }

    public void ApplyStyle(Image iconImage, Image bgImage, TMP_Text labelText, bool isSelected)
    {
        Color themeColor = GetThemeColorDynamic();

        if (isSelected)
        {
            if (iconImage != null)
                iconImage.color = WHITE;

            if (bgImage != null)
                bgImage.color = themeColor;
        }
        else
        {
            if (iconImage != null)
                iconImage.color = themeColor;

            if (bgImage != null)
                bgImage.color = WHITE;
        }
    }

    private Color GetThemeColorDynamic()
    {
        if (enumValue is TacticalRole role)
            return role.GetThemeColor();

        if (enumValue is Role roleType)
            return roleType.GetThemeColor();

        if (enumValue is AttackType attackType)
            return attackType.GetThemeColor();

        if (enumValue is DefenseType defenseType)
            return defenseType.GetThemeColor();

        return Color.white;
    }
}

public class GenericIconTextBgSwapMediator<T> : IFilterButtonMediator where T : struct, System.Enum
{
    private readonly T enumValue;
    private static readonly Color WHITE = Palette.searchWhite;

    public GenericIconTextBgSwapMediator(T value) { this.enumValue = value; }

    public void ApplyStyle(Image iconImage, Image bgImage, TMP_Text labelText, bool isSelected)
    {
        Color themeColor = GetThemeColorDynamic();
        if (isSelected)
        {
            if (iconImage != null) iconImage.color = WHITE;
            if (labelText != null) labelText.color = WHITE;
            if (bgImage != null) bgImage.color = themeColor;
        }
        else
        {
            if (iconImage != null) iconImage.color = themeColor;
            if (labelText != null) labelText.color = themeColor;
            if (bgImage != null) bgImage.color = WHITE;
        }
    }
    
    private Color GetThemeColorDynamic()
    {
        if (enumValue is TacticalRole role)
            return role.GetThemeColor();

        if (enumValue is Role roleType)
            return roleType.GetThemeColor();

        if (enumValue is AttackType attackType)
            return attackType.GetThemeColor();

        if (enumValue is DefenseType defenseType)
            return defenseType.GetThemeColor();

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

    /// <summary>
    /// ⭐ NEW: 텍스트 색상까지 지정하는 오버로드
    /// 선택/미선택 상태에 따라 라벨 텍스트 색상도 함께 전환됩니다.
    /// </summary>
    public static IFilterButtonMediator CreateGrayToggle(Color selectedBgColor, Color textColorDefault, Color textColorSelected)
        => new GrayToggleMediator(selectedBgColor, textColorDefault, textColorSelected);

    public static IFilterButtonMediator CreateIconColor(Color iconColor, Color selectedBgColor)
        => new IconColorMediator(iconColor, selectedBgColor);

    public static IFilterButtonMediator CreateFullColor(Color themeColor)
        => new FullColorMediator(themeColor);

    public static IFilterButtonMediator CreateIconGrayToggle()
        => new IconGrayToggleMediator();

    public static IFilterButtonMediator TextToggleMediator()
        => new TextToggleMediator();

    public static IFilterButtonMediator CreateBlackIconGrayBg()
        => new BlackIconGrayBgMediator();

    public static IFilterButtonMediator CreateWhiteGrayBg()
        => new WhiteBgGrayMediator();

    public static IFilterButtonMediator CreateIconBgColorSwap(Color themeColor)
        => new IconBgColorSwapMediator(themeColor);

    public static IFilterButtonMediator CreateGenericIconBgSwap<T>(T enumValue) where T : struct, System.Enum
        => new GenericIconBgSwapMediator<T>(enumValue);

    public static IFilterButtonMediator CreateGenericIconTextBgSwap<T>(T enumValue) where T : struct, System.Enum
        => new GenericIconTextBgSwapMediator<T>(enumValue);
    
    public static IFilterButtonMediator CreateTextBgSwap(Color themeColor) 
        => new TextBgSwapMediator(themeColor);
}