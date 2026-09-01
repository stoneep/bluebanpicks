using System;
using UnityEngine.Serialization;


[Serializable]
public sealed class CharDatabaseRoot
{
    public CharEntry[] characters;
}

[Serializable]
public sealed class CharPatchRoot
{
    public CharEntry[] adds;
    public CharEntry[] overrides;
}

[Serializable]
public class CharEntry
{
    public string Id;
    public string BaseId;
    public string DisplayName;
    public string DisplayName_Kr;
    
    public int Level;
    public int    Rarity;
    public string Affiliation;
    public string TacticalRole;
    public string Role;
    public string Position;
    public string AttackType;
    public string DefenseType;
    public string WeaponClass;
    
    public EquipDTO equip;
    
    public TerrainDTO Preferred;
}

[Serializable]
public sealed class EquipDTO
{
    public string weapon;
    public string armor;
    public string accessory;
    public bool   unique;
}

[Serializable]
public sealed class TerrainDTO
{
    public int Urban;
    public int Field;
    public int Indoor;
}