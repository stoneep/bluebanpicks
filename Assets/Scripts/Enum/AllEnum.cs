
/// <summary>
/// GridLayoutGroup과 동일한 레이아웃 모드
/// BaseVirtualizedGrid의 제네릭 의존성을 제거하기 위해 별도 파일로 분리
/// </summary>
public enum GridLayoutMode
{
    Vertical,   // 세로 스크롤: columns 고정, rows 자동 증가
    Horizontal  // 가로 스크롤: rows 고정, columns 자동 증가
}

public enum CharacterSortType
{
    ByRarity,
    ByLevel,
    ByName,
    ByAffiliation,
    ByTacticalRole,
    ByRole,
    ByWeaponType
}

public enum GameLanguage
{
    English,
    Korean
}

public enum SortOrder
{
    Ascending,
    Descending
}

public enum CharacterCut
{
    Large,
    Slot,
    Small,
    Collection
}