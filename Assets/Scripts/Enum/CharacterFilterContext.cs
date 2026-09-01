using System;

[Serializable]
public struct CharacterFilterContext
{
    public Affiliation? Affiliation;
    public TacticalRole? TacticalRole;
    public Role? Role;
    public AttackType? AttackType;
    public DefenseType? DefenseType;
    public Position? Position;
    public string SearchText;
    
    public CharacterSortType SortType;
    public SortOrder SortOrder;
    
    public static CharacterFilterContext Default => new CharacterFilterContext
    {
        Affiliation = null,
        TacticalRole = null,
        AttackType = null,
        DefenseType = null,
        Position = null,
        SearchText = string.Empty,
        SortType = CharacterSortType.ByAffiliation,
        SortOrder = SortOrder.Descending
    };
}