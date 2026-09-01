using System;
using System.Collections.Generic;
using UnityEngine;


public static class CharDatabaseLoader
{

    private static readonly List<string> _cachedIds = new();
    private static readonly Dictionary<string, string> _baseIdMap = new();
    private static readonly Dictionary<string, CharacterViewData> _viewDataCache = new();
    public static GameLanguage CurrentLanguage { get; private set; } = GameLanguage.English;
    
    public static IReadOnlyList<string> AllIds => _cachedIds;
    
    public static string GetBaseId(string id)
    {
        if (string.IsNullOrEmpty(id)) return "";
        return _baseIdMap.TryGetValue(id, out var baseId) ? baseId : id;
    }
    
    public static bool IsSameBase(string idA, string idB)
    {
        return string.Equals(GetBaseId(idA), GetBaseId(idB), StringComparison.OrdinalIgnoreCase);
    }
    
    public static bool TryGetViewData(string id, out CharacterViewData data)
    {
        if (string.IsNullOrEmpty(id))
        {
            data = default;
            return false;
        }
        return _viewDataCache.TryGetValue(id, out data);
    }
    
    public static string GetDisplayName(string id) =>
        TryGetViewData(id, out var data) ? data.DisplayName : id;
    

    public static List<CharacterViewData> LoadFromJson(
        TextAsset charactersData, TextAsset patchJson = null,
        GameLanguage language = GameLanguage.English)
    {
        if (charactersData == null)
            throw new ArgumentNullException(nameof(charactersData));
        
        CurrentLanguage = language;  
        
        var baseRoot = JsonUtility.FromJson<CharDatabaseRoot>(charactersData.text);
        var entries  = new List<CharEntry>(baseRoot?.characters ?? Array.Empty<CharEntry>());

        if (patchJson != null)
            ApplyPatch(entries, patchJson.text);

        var result = BuildViewData(entries);
        
        RebuildCache(entries);
        RebuildViewDataCache(result);

        return result;
    }
    

    private static void RebuildCache(List<CharEntry> entries)
    {
        _cachedIds.Clear();
        _baseIdMap.Clear();

        foreach (var c in entries)
        {
            if (string.IsNullOrWhiteSpace(c.Id)) continue;

            _cachedIds.Add(c.Id);
            
            _baseIdMap[c.Id] = string.IsNullOrEmpty(c.BaseId) ? c.Id : c.BaseId;
        }
        
    }

    private static void RebuildViewDataCache(List<CharacterViewData> data)
    {
        _viewDataCache.Clear();
        foreach (var d in data)
        {
            if (string.IsNullOrWhiteSpace(d.Id)) continue;
            _viewDataCache[d.Id] = d;
        }
    }
    

    private static void ApplyPatch(List<CharEntry> entries, string patchText)
    {
        var patch = JsonUtility.FromJson<CharPatchRoot>(patchText);
        if (patch == null) return;
        
        if (patch.overrides != null)
        {
            foreach (var o in patch.overrides)
            {
                if (o == null || string.IsNullOrWhiteSpace(o.Id)) continue;
                int idx = entries.FindIndex(x =>
                    string.Equals(x.Id, o.Id, StringComparison.OrdinalIgnoreCase));
                if (idx >= 0) entries[idx] = MergeEntry(entries[idx], o);
            }
        }
        
        if (patch.adds != null)
        {
            foreach (var a in patch.adds)
            {
                if (a == null || string.IsNullOrWhiteSpace(a.Id)) continue;
                bool exists = entries.Exists(x =>
                    string.Equals(x.Id, a.Id, StringComparison.OrdinalIgnoreCase));
                if (!exists) entries.Add(a);
            }
        }
    }
    
    private static CharEntry MergeEntry(CharEntry baseDto, CharEntry patchDto)
    {
        if (!string.IsNullOrWhiteSpace(patchDto.DisplayName))
            baseDto.DisplayName = patchDto.DisplayName;
        if (patchDto.Rarity != 0)
            baseDto.Rarity = patchDto.Rarity;

        MergeString(ref baseDto.BaseId,      patchDto.BaseId);
        MergeString(ref baseDto.Affiliation,  patchDto.Affiliation);
        MergeString(ref baseDto.TacticalRole, patchDto.TacticalRole);
        MergeString(ref baseDto.Role,         patchDto.Role);
        MergeString(ref baseDto.Position,     patchDto.Position);
        MergeString(ref baseDto.AttackType,   patchDto.AttackType);
        MergeString(ref baseDto.DefenseType,  patchDto.DefenseType);
        MergeString(ref baseDto.WeaponClass, patchDto.WeaponClass);

        if (patchDto.Preferred != null) baseDto.Preferred = patchDto.Preferred;

        return baseDto;
    }

    private static TerrainGrade ParseGrade(int value)
    {
        if (value < 1 || value > 6)
        {
            Debug.LogWarning($"[CharDB] 잘못된 지형 등급: {value} → D로 설정");
            return TerrainGrade.D;
        }
        return (TerrainGrade)value;
    }
    
    private static void MergeString(ref string target, string source)
    {
        if (!string.IsNullOrWhiteSpace(source)) target = source;
    }
    

    private static List<CharacterViewData> BuildViewData(List<CharEntry> dtos)
    {
        var result = new List<CharacterViewData>(dtos.Count);

        foreach (var c in dtos)
        {
            if (!ValidateEntry(c)) continue;

            result.Add(new CharacterViewData
            {
                Id          = c.Id,
                DisplayName = ResolveDisplayName(c, CurrentLanguage),
                
                DisplayNameEn = c.DisplayName,
                DisplayNameKr = c.DisplayName_Kr,

                Rarity      = Mathf.Clamp(c.Rarity, 1, 5),
                
                Affiliation  = ParseEnum(c.Affiliation,  Affiliation.etc),
                TacticalRole = ParseEnum(c.TacticalRole, TacticalRole.Striker),
                Role         = ParseEnum(c.Role,         Role.Dealer),
                Position     = ParseEnum(c.Position,     Position.Middle),
                AttackType   = ParseEnum(c.AttackType,   AttackType.Explosive),
                DefenseType  = ParseEnum(c.DefenseType,  DefenseType.Light),
                WeaponClass = ParseEnum(c.WeaponClass, WeaponClass.SG),
                
                WeaponType    = ParseEnum(c.equip.weapon,    WeaponType.Hat),
                ArmorType     = ParseEnum(c.equip.armor,     ArmorType.Bag),
                AccessoryType = ParseEnum(c.equip.accessory, AccessoryType.Amulet),
                HasUnique     = c.equip.unique,
                
                Terrain = c.Preferred != null
                    ? new TerrainPreference
                    {
                        Urban  = ParseGrade(c.Preferred.Urban),
                        Field  = ParseGrade(c.Preferred.Field),
                        Indoor = ParseGrade(c.Preferred.Indoor)
                    }
                    : default,
                        
                IsLocked = false,
            });
        }

        return result;
    }
    

    private static bool ValidateEntry(CharEntry c)
    {
        if (string.IsNullOrWhiteSpace(c.Id))
        {
            Debug.LogError($"[CharDB] id가 비어있음. displayName={c.DisplayName}");
            return false;
        }

        if (c.equip == null)
        {
            Debug.LogError($"[CharDB] equip 누락: {c.Id}");
            return false;
        }

        if (string.IsNullOrWhiteSpace(c.equip.weapon) ||
            string.IsNullOrWhiteSpace(c.equip.armor)  ||
            string.IsNullOrWhiteSpace(c.equip.accessory))
        {
            Debug.LogError($"[CharDB] 필수 장비 슬롯 누락: {c.Id} " +
                           $"(weapon='{c.equip.weapon}' armor='{c.equip.armor}' accessory='{c.equip.accessory}')");
            return false;
        }

        return true;
    }

    private static string ResolveDisplayName(CharEntry c, GameLanguage language)
    {
        if (language == GameLanguage.Korean && !string.IsNullOrWhiteSpace(c.DisplayName_Kr))
            return c.DisplayName_Kr;
        return c.DisplayName;
    }
    

    private static T ParseEnum<T>(string raw, T fallback) where T : struct
    {
        if (string.IsNullOrWhiteSpace(raw)) return fallback;
        if (Enum.TryParse<T>(raw, true, out var value)) return value;

        Debug.LogWarning($"[CharDB] Enum 파싱 실패: {typeof(T).Name} raw='{raw}' → fallback={fallback}");
        return fallback;
    }
}
