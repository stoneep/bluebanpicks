using UnityEngine;

// ─────────────────────────────────────────────
// CharacterViewData.cs
// UI/필터/정렬에서 사용하는 읽기 전용 뷰 모델
// ─────────────────────────────────────────────

public struct CharacterViewData
{
    // ── 기본 정보 ──
    public string Id;
    public string DisplayName;   // 현재 언어로 해석된 표시명 (UI 렌더링용)
    public int    Level;
    public int    Rarity;

    // ── 검색 전용 ──
    // DisplayName은 현재 언어에 따라 En/Kr 중 하나로 고정되어 버리므로,
    // 표시 언어와 무관하게 검색이 되도록 원본 이름을 둘 다 보관한다.
    public string DisplayNameEn;
    public string DisplayNameKr;

    // ── 전투 분류 ──
    public Affiliation  Affiliation;
    public TacticalRole TacticalRole;
    public Role         Role;
    public Position     Position;
    public AttackType   AttackType;
    public DefenseType  DefenseType;
    public WeaponClass WeaponClass;

    // ── 장비 ──
    public WeaponType    WeaponType;
    public ArmorType     ArmorType;
    public AccessoryType AccessoryType;
    public bool          HasUnique;
    
    // ── 지형 선호도 ──
    public TerrainPreference Terrain;

    // ── UI 전용 ──
    public bool   IsLocked;
}