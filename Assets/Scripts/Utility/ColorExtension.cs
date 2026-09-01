using UnityEngine;

public static class ColorExtensions
{
    public static Color ToColor(this string hexString)
    {
        if (string.IsNullOrEmpty(hexString)) 
        {
            return Color.white; 
        }
        
        if (!hexString.StartsWith("#"))
        {
            hexString = "#" + hexString;
        }
        
        if (ColorUtility.TryParseHtmlString(hexString, out Color color))
        {
            return color;
        }
        
        Debug.LogWarning($"[ColorExtensions] 색상 코드가 유효하지 않습니다: {hexString}");
        return Color.white;
    }
    
    public static string ToHex(this Color color)
    {
        return "#" + ColorUtility.ToHtmlStringRGBA(color);
    }
}

public static class Palette
{
    public static readonly Color32 AzureishWhite = new Color32(225, 235, 237, 255);
    public static readonly Color32 MintCream     = new Color32(245, 255, 250, 255);

    public static readonly Color32 CombatRed = new Color32(255, 89, 89, 255);
    public static readonly Color32 CombatYellow = new Color32(255, 191, 51, 255);
    public static readonly Color32 CombatBlue = new Color32(102, 178, 255, 255);
    public static readonly Color32 CombatMint = new Color32(19, 121, 115, 255);
    public static readonly Color32 CombatPurple = new Color32(193, 22, 211, 255);

    public static readonly Color32 TacRed = new Color32(198, 25, 20, 255);
    public static readonly Color32 TacBlue = new Color32(5,102,234,255);
    
    public static readonly Color DeepOrange  = "#FF5722".ToColor();
    public static readonly Color OffWhite  = "#FAF9F6".ToColor();
    public static readonly Color searchWhite  = "#FFFFFF".ToColor();
    public static readonly Color SkyBlue     = "#87CEEB".ToColor();
    public static readonly Color DeepBlue     = "#153756".ToColor();
    public static readonly Color SoftBlue    = "#8ED8FC".ToColor();
    public static readonly Color NeonGreen   = "39FF14".ToColor();
    public static readonly Color MenuDarkBlue= "686880".ToColor();
    public static readonly Color MenuWhite   = "DBD7D6".ToColor();
    public static readonly Color AntibioticsWhite   = "EAF1F1".ToColor();
    public static readonly Color SemiBlack   = "#00000080".ToColor();
}