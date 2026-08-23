// ─────────────────────────────────────────────
// CharacterEnums.cs
// 캐릭터 관련 모든 열거형 + 공용 구조체 정의
// ─────────────────────────────────────────────

// ── 소속 ──
public enum Affiliation
{
    hyakkiyako, redwinter, trinity, gehenna, abydos,
    millennium, arius, shanhaijing, valkyrie, srt,
    highlander, wildhunt, etc
}

public enum WeaponClass
{
    SG, SMG, AR, GL, HG, RL, SR, RG, MG, MT, FT
}

// ── 전투 분류 ──
public enum TacticalRole { Striker, Special }
public enum Role        { Dealer, Support, Healer, Tank, TacticalSupport }
public enum Position    { Front, Middle, Back }
public enum AttackType  { Explosive, Piercing, Decomposite, Mystic, Sonic }
public enum DefenseType { Light, Heavy, Composite, Special, Elastic }

// ── 장비 타입 ──
public enum WeaponType    { None, Hat, Shoes, Gloves }
public enum ArmorType     { None, Bag, Badge, Hairpin }
public enum AccessoryType { None, Amulet, Wristwatch, Necklace }

// 지형 적응도 등급 (1~6 매핑)
public enum TerrainGrade
{
    SS = 1,  // 최상
    S  = 2,
    A  = 3,
    B  = 4,
    C  = 5,
    D  = 6   // 최하
}

// ── 지형 선호도 ──
[System.Serializable]
public struct TerrainPreference
{
    public TerrainGrade Urban;
    public TerrainGrade Field;
    public TerrainGrade Indoor;

    public override string ToString() 
        => $"Urban={Urban} Field={Field} Indoor={Indoor}";
}