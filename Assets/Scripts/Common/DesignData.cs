using System;
using UnityEngine;

public static class CombatTypeColor
{
    // Attack Type Color
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

    // Defense Type Color
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
    // 1. 버튼 상태 색상
    public static readonly Color Selected = new Color(0.2f, 0.8f, 1.0f); // 하늘색 (선택됨)
    public static readonly Color Normal = Color.white; // 기본색
    public static readonly Color Disabled = new Color(0.5f, 0.5f, 0.5f); // 비활성

    // 2. 등급(Rarity)별 배경색 (필요 시)
    public static Color GetRarityColor(int rarity)
    {
        return rarity switch
        {
            3 => new Color(1f, 0.8f, 0.2f), // 3성: 금색
            2 => new Color(0.8f, 0.5f, 1f), // 2성: 보라색
            _ => new Color(0.7f, 0.7f, 0.7f) // 1성: 회색
        };
    }

    // 3. 텍스트 강조 색상
    public static readonly string HighlightHex = "#32C8FF";
}

// 이 스크립트는 단독 컴포넌트가 아니라, 설정을 담는 그릇입니다.
    [Serializable]
    public struct FilterColorOption<T> where T : struct, Enum
    {
        public T Type; // 공격타입, 방어타입 등
        public Color Color; // 인스펙터에서 지정할 색상
    }
