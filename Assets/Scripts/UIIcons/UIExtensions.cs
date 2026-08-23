using UnityEngine;

// 데이터(Enum) -> UI리소스(String/Color) 변환 스크립트
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
        //test
    }
    
    // 4. 공격/방어 타입 -> 스프라이트 이름 (공통 이름)
    public static string ToCommonSpriteName(this AttackType _) => "atk_common";
    public static string ToCommonSpriteName(this DefenseType _) => "def_common";
    
    // 5. 색상 관련 (기존 로직 활용 또는 여기서 통합)
    public static Color GetThemeColor(this AttackType type) => CombatTypeColor.Attack(type);
    public static Color GetThemeColor(this DefenseType type) => CombatTypeColor.Defense(type);
    public static Color GetThemeColor(this Role type) => CombatTypeColor.Roles(type);
    public static Color GetThemeColor(this TacticalRole type) => CombatTypeColor.TacticalRoleType(type);
}