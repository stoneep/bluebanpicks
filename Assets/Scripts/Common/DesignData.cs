using System;
using UnityEngine;

public static class CombatTypeColor
{
    public static Color TacticalRoleAll => Palette.DeepBlue;
    
    public static Color Attack(AttackType type)
    {
        return type switch
        {
            AttackType.Explosive => Palette.CombatRed,
            AttackType.Piercing  => Palette.CombatYellow,
            AttackType.Decomposite => Palette.CombatMint,
            AttackType.Mystic    => Palette.CombatBlue,
            AttackType.Sonic    => Palette.CombatPurple,
            _ => Color.white
        };
    }
    
    public static Color Defense(DefenseType type)
    {
        return type switch
        {
            DefenseType.Light    => Palette.CombatRed,
            DefenseType.Heavy    => Palette.CombatYellow,
            DefenseType.Composite=> Palette.CombatMint,
            DefenseType.Special  => Palette.CombatBlue,
            DefenseType.Elastic  => Palette.CombatPurple,
            _ => Color.white
        };
    }
    public static Color Roles(Role type)
    {
        return type switch
        {
            Role.Dealer    => Palette.AzureishWhite,
            Role.Support    => Palette.AzureishWhite,
            Role.Healer  => Palette.AzureishWhite,
            Role.Tank  => Palette.AzureishWhite,
            Role.TacticalSupport  => Palette.AzureishWhite,
            _ => Palette.AzureishWhite
        };
    }
    
    public static Color TacticalRoleType(TacticalRole type)
    {
        return type switch
        {
            TacticalRole.Striker   => Palette.TacRed,
            TacticalRole.Special   => Palette.TacBlue,
            _ => Color.white
        };
    }
}

public static class UIStylePalette
{
    public static readonly Color Selected = new Color(0.2f, 0.8f, 1.0f);
    public static readonly Color Normal = Color.white;
    public static readonly Color Disabled = new Color(0.5f, 0.5f, 0.5f);
    
    public static Color GetRarityColor(int rarity)
    {
        return rarity switch
        {
            3 => new Color(1f, 0.8f, 0.2f),
            2 => new Color(0.8f, 0.5f, 1f),
            _ => new Color(0.7f, 0.7f, 0.7f)
        };
    }
    
    public static readonly string HighlightHex = "#32C8FF";
}

    [Serializable]
    public struct FilterColorOption<T> where T : struct, Enum
    {
        public T Type;
        public Color Color;
    }
