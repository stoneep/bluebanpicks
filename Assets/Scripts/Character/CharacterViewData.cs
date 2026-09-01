using UnityEngine;






public struct CharacterViewData
{
    
    public string Id;
    public string DisplayName;   
    public int    Level;
    public int    Rarity;

    
    
    
    public string DisplayNameEn;
    public string DisplayNameKr;

    
    public Affiliation  Affiliation;
    public TacticalRole TacticalRole;
    public Role         Role;
    public Position     Position;
    public AttackType   AttackType;
    public DefenseType  DefenseType;
    public WeaponClass WeaponClass;

    
    public WeaponType    WeaponType;
    public ArmorType     ArmorType;
    public AccessoryType AccessoryType;
    public bool          HasUnique;
    
    
    public TerrainPreference Terrain;

    
    public bool   IsLocked;
}