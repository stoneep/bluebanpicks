using TMPro;
using UnityEngine;
using UnityEngine.UI;

public interface IFilterButtonMediator
{
    void ApplyStyle(Image iconImage, Image bgImage, TMP_Text labelText, bool isSelected);
}

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
        if (bgImage != null)
        {
            bgImage.color = assignedColor;
        }
        
        if (iconImage != null)
        {
            float alpha = isSelected ? 1.0f : 0.4f;
            iconImage.color = new Color(1f, 1f, 1f, alpha);
        }
        
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


public class IconGrayToggleMediator : IFilterButtonMediator
{
    private readonly Color normalIconColor = Palette.SemiBlack;
    private readonly Color selectedIconColor = Palette.searchWhite;

    public void ApplyStyle(Image iconImage, Image bgImage, TMP_Text labelText, bool isSelected)
    {
        if (iconImage != null)
        {
            iconImage.color = isSelected ? selectedIconColor : normalIconColor;
        }
        
    }
}


public class WhiteBgGrayMediator : IFilterButtonMediator
{
    private static readonly Color WHITE = Palette.OffWhite;
    private static readonly Color deeblue = Palette.DeepBlue;

    public void ApplyStyle(Image iconImage, Image bgImage, TMP_Text labelText, bool isSelected)
    {

        if (bgImage != null)
        {
            bgImage.color = isSelected ? deeblue : WHITE;
        }
    }
}

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
    }
}

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
        Debug.Log($"[GenericIconBgSwap] type={enumValue.GetType().Name}, value={enumValue}");
        
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

public static class FilterButtonMediatorFactory
{
    public static IFilterButtonMediator CreateGrayToggle(Color selectedBgColor)
        => new GrayToggleMediator(selectedBgColor);
    
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