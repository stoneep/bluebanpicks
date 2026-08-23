using System;
using UnityEngine.Serialization;

// ─────────────────────────────────────────────
// CharDatabaseDTO.cs
// JSON 직렬화/역직렬화용 DTO (JsonUtility 호환)
//
// 규칙: 필드명 == JSON 키명 (JsonUtility 제약)
//       PascalCase 필드 → JSON도 PascalCase
//       camelCase 필드  → JSON도 camelCase
// ─────────────────────────────────────────────

// ── Root: characters.json ──
[Serializable]
public sealed class CharDatabaseRoot
{
    public CharEntry[] characters;
}

// ── Root: patch.json ──
[Serializable]
public sealed class CharPatchRoot
{
    public CharEntry[] adds;
    public CharEntry[] overrides;
}

// ── 캐릭터 한 줄 ──
[Serializable]
public class CharEntry
{
    // ── 식별 (camelCase) ──
    public string Id;
    public string BaseId;
    public string DisplayName;
    public string DisplayName_Kr;
    
    // ── 전투 속성 (PascalCase — JSON 원본 그대로) ──
    public int Level;
    public int    Rarity;
    public string Affiliation;
    public string TacticalRole;
    public string Role;
    public string Position;
    public string AttackType;
    public string DefenseType;
    public string WeaponClass;
    
    // ── 장비 ──
    public EquipDTO equip;

    // ── 지형 선호도 ──
    public TerrainDTO Preferred;
}

// ── 장비 슬롯 ──
[Serializable]
public sealed class EquipDTO
{
    public string weapon;
    public string armor;
    public string accessory;
    public bool   unique;
}

// ── 지형 선호도 ──
[Serializable]
public sealed class TerrainDTO
{
    public int Urban;
    public int Field;
    public int Indoor;
}