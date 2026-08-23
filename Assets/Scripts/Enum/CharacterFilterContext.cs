using System;

[Serializable]
public struct CharacterFilterContext
{
    // 1. 필터 데이터
    public Affiliation? Affiliation;
    public TacticalRole? TacticalRole;
    public Role? Role;           // 추가
    public AttackType? AttackType; // 추가
    public DefenseType? DefenseType; // 추가
    public Position? Position;   // 추가
    public string SearchText;    // 추가: 이름 검색어

    // 2. 정렬 데이터
    public CharacterSortType SortType;
    public SortOrder SortOrder;
    
    // 3. 기본값(리셋용)
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