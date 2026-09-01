using UnityEngine;

public static class UIExtensions
{
    public const string ATLAS_COMMON = AtlasAddressConfig.COMBAT;
    public const string ATLAS_AFFILIATION = AtlasAddressConfig.AFFILIATION;

    public static string ToSpriteName(this Affiliation affiliation)
    {
        return $"logo_{affiliation.ToString().ToLowerInvariant()}";
    }

    public static string ToSpriteName(this Role role)
    {
        return $"role_{role.ToString().ToLowerInvariant()}";
    }

    public static string ToSpriteName(this TacticalRole tacticalRole)
    {
        return $"tacticalRole_{tacticalRole.ToString().ToLowerInvariant()}";
    }
    
    public static string ToCommonSpriteName(this AttackType _) => "atk_common";
    public static string ToCommonSpriteName(this DefenseType _) => "def_common";
    
    public static Color GetThemeColor(this AttackType type) => CombatTypeColor.Attack(type);
    public static Color GetThemeColor(this DefenseType type) => CombatTypeColor.Defense(type);
    public static Color GetThemeColor(this Role type) => CombatTypeColor.Roles(type);
    public static Color GetThemeColor(this TacticalRole type) => CombatTypeColor.TacticalRoleType(type);
}