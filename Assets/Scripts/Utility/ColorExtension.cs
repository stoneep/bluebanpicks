using UnityEngine;

/// <summary>
/// 문자열(Hex Code)과 Color 간의 변환을 쉽게 해주는 확장 클래스입니다.
/// </summary>
public static class ColorExtensions
{
    /// <summary>
    /// Hex Code -> Unity Color
    /// "#FF0000".ToColor(), "FF0000".ToColor()
    /// </summary>
    public static Color ToColor(this string hexString)
    {
        // 1. 빈 문자열 기본 흰색
        if (string.IsNullOrEmpty(hexString)) 
        {
            return Color.white; 
        }

        // 2. "FF0000" -> "#FF0000"
        if (!hexString.StartsWith("#"))
        {
            hexString = "#" + hexString;
        }

        // 3. 변환 시도
        if (ColorUtility.TryParseHtmlString(hexString, out Color color))
        {
            return color;
        }

        // 4. 변환 실패
        Debug.LogWarning($"[ColorExtensions] 색상 코드가 유효하지 않습니다: {hexString}");
        return Color.white;
    }

    /// <summary>
    /// Color To Hex Code
    /// myColor.ToHex()
    /// </summary>
    public static string ToHex(this Color color)
    {
        return "#" + ColorUtility.ToHtmlStringRGBA(color);
    }
}

public static class Palette
{
    // ==========================================
    // 1. RGB 방식
    // ==========================================
    public static readonly Color32 AzureishWhite = new Color32(225, 235, 237, 255);
    public static readonly Color32 MintCream     = new Color32(245, 255, 250, 255);

    public static readonly Color32 CombatRed = new Color32(255, 89, 89, 255);
    public static readonly Color32 CombatYellow = new Color32(255, 191, 51, 255);
    public static readonly Color32 CombatBlue = new Color32(102, 178, 255, 255);
    public static readonly Color32 CombatMint = new Color32(19, 121, 115, 255);
    public static readonly Color32 CombatPurple = new Color32(193, 22, 211, 255);

    public static readonly Color32 TacRed = new Color32(198, 25, 20, 255);
    public static readonly Color32 TacBlue = new Color32(5,102,234,255);
    // ==========================================
    // 2. Hex Code 방식 (확장 메서드 활용)
    // ==========================================
    
    public static readonly Color DeepOrange  = "#FF5722".ToColor();
    public static readonly Color SkyBlue     = "#87CEEB".ToColor();
    public static readonly Color NeonGreen   = "39FF14".ToColor();
    public static readonly Color MenuDarkBlue= "686880".ToColor();
    public static readonly Color MenuWhite   = "DBD7D6".ToColor();
    public static readonly Color SemiBlack   = "#00000080".ToColor(); // 반투명


    // ==========================================
    // Helper
    // ==========================================
    /*
    private static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString(hex.StartsWith("#") ? hex : "#" + hex, out Color c);
        return c;
    }

    public static readonly Color MyColor = Hex("FF0000");
    */
}