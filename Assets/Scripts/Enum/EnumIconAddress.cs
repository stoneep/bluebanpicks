public static class EnumIconAddress
{
    public static string Affiliation(Affiliation v)
        => $"logo/{v.ToString().ToLowerInvariant()}";

    public static string AttackType(AttackType v)
        => $"icon/attack_type/{v.ToString().ToLowerInvariant()}";

    public static string DefenseType(DefenseType v)
        => $"icon/defense_type/{v.ToString().ToLowerInvariant()}";

    public static string Role(Role v)
        => $"icon/role/{v.ToString().ToLowerInvariant()}";

    public static string Position(Position v)
        => $"icon/position/{v.ToString().ToLowerInvariant()}";
}