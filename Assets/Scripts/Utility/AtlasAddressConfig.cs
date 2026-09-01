using UnityEngine;

public static class AtlasAddressConfig
{
    public const string AFFILIATION = "atlas/icon_affiliation";
    public const string COMBAT_TYPE = "atlas/icon_combatType";
    public const string COMBAT = "atlas/icon_combat";
    
    public static string[] GetAllAtlasKeys()
    {
        return new[]
        {
            AFFILIATION,
            COMBAT,
            COMBAT_TYPE,
        };
    }
    
    public static string[] GetCoreAtlasKeys()
    {
        return new[]
        {
            AFFILIATION,
            COMBAT,
            COMBAT_TYPE,
        };
    }
}
