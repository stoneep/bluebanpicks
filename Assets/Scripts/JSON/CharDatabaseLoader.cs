using System;
using System.Collections.Generic;
using UnityEngine;

// ─────────────────────────────────────────────
// CharDatabaseLoader.cs
// JSON → List<CharacterViewData> 변환 파이프라인
//
// 흐름: JSON TextAsset
//       → CharDatabaseRoot / CharPatchRoot (DTO)
//       → Merge (patch 적용)
//       → BuildViewData (DTO → ViewData)
//       → 캐시 구축 (AllIds, BaseId 매핑)
// ─────────────────────────────────────────────

public static class CharDatabaseLoader
{
    // ════════════════════════════════════════
    // 캐시: Id 목록 + BaseId 매핑
    // ════════════════════════════════════════

    private static readonly List<string> _cachedIds = new();
    private static readonly Dictionary<string, string> _baseIdMap = new();
    private static readonly Dictionary<string, CharacterViewData> _viewDataCache = new();
    public static GameLanguage CurrentLanguage { get; private set; } = GameLanguage.English;
    
    /// <summary> 로드된 전체 캐릭터 Id 목록 </summary>
    public static IReadOnlyList<string> AllIds => _cachedIds;

    /// <summary>
    /// Id → BaseId 조회.
    /// BaseId가 없거나 매핑에 없으면 자기 자신 반환.
    /// 예: "aru_newyear" → "aru", "aru" → "aru", "unknown" → "unknown"
    /// </summary>
    public static string GetBaseId(string id)
    {
        if (string.IsNullOrEmpty(id)) return "";
        return _baseIdMap.TryGetValue(id, out var baseId) ? baseId : id;
    }

    /// <summary>
    /// 두 캐릭터가 같은 원본(base)인지 판별
    /// 예: IsSameBase("aru", "aru_newyear") → true
    /// </summary>
    public static bool IsSameBase(string idA, string idB)
    {
        return string.Equals(GetBaseId(idA), GetBaseId(idB), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Id로 전체 CharacterViewData를 조회. 밴픽 결과창처럼 "id만 갖고 있는" 코드에서
    /// 이름/초상화 정보를 되찾을 때 사용한다. LoadFromJson 이후에만 채워진다.
    /// </summary>
    public static bool TryGetViewData(string id, out CharacterViewData data)
    {
        if (string.IsNullOrEmpty(id))
        {
            data = default;
            return false;
        }
        return _viewDataCache.TryGetValue(id, out data);
    }

    /// <summary>Id에 대응하는 표시명(현재 언어 기준). 캐시에 없으면 id 자체를 그대로 반환한다.</summary>
    public static string GetDisplayName(string id) =>
        TryGetViewData(id, out var data) ? data.DisplayName : id;

    // ════════════════════════════════════════
    // Public API
    // ════════════════════════════════════════

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

        // 캐시 구축
        RebuildCache(entries);
        RebuildViewDataCache(result);

        return result;
    }

    // ════════════════════════════════════════
    // 캐시 구축
    // ════════════════════════════════════════

    private static void RebuildCache(List<CharEntry> entries)
    {
        _cachedIds.Clear();
        _baseIdMap.Clear();

        foreach (var c in entries)
        {
            if (string.IsNullOrWhiteSpace(c.Id)) continue;

            _cachedIds.Add(c.Id);

            // BaseId가 비어있으면 자기 자신이 base
            _baseIdMap[c.Id] = string.IsNullOrEmpty(c.BaseId) ? c.Id : c.BaseId;
        }

//        Debug.Log($"[CharDB] 캐시 구축 완료 (Ids: {_cachedIds.Count}, BaseId 매핑: {_baseIdMap.Count})");
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

    // ════════════════════════════════════════
    // Patch
    // ════════════════════════════════════════

    private static void ApplyPatch(List<CharEntry> entries, string patchText)
    {
        var patch = JsonUtility.FromJson<CharPatchRoot>(patchText);
        if (patch == null) return;

        // Override: 기존 항목 덮어쓰기
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

        // Add: 신규 항목 추가 (중복 무시)
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

    /// <summary>
    /// patch 값이 비어있지 않으면 base를 덮어씀 ("null/0이면 유지" 전략)
    /// </summary>
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

    // ════════════════════════════════════════
    // DTO → ViewData 변환
    // ════════════════════════════════════════

    private static List<CharacterViewData> BuildViewData(List<CharEntry> dtos)
    {
        var result = new List<CharacterViewData>(dtos.Count);

        foreach (var c in dtos)
        {
            if (!ValidateEntry(c)) continue;

            result.Add(new CharacterViewData
            {
                // 기본
                Id          = c.Id,
                DisplayName = ResolveDisplayName(c, CurrentLanguage),

                // 검색은 현재 표시 언어와 무관하게 동작해야 하므로 원본 이름을 그대로 보관
                DisplayNameEn = c.DisplayName,
                DisplayNameKr = c.DisplayName_Kr,

                Rarity      = Mathf.Clamp(c.Rarity, 1, 5),

                // 전투 분류
                Affiliation  = ParseEnum(c.Affiliation,  Affiliation.etc),
                TacticalRole = ParseEnum(c.TacticalRole, TacticalRole.Striker),
                Role         = ParseEnum(c.Role,         Role.Dealer),
                Position     = ParseEnum(c.Position,     Position.Middle),
                AttackType   = ParseEnum(c.AttackType,   AttackType.Explosive),
                DefenseType  = ParseEnum(c.DefenseType,  DefenseType.Light),
                WeaponClass = ParseEnum(c.WeaponClass, WeaponClass.SG),

                // 장비
                WeaponType    = ParseEnum(c.equip.weapon,    WeaponType.Hat),
                ArmorType     = ParseEnum(c.equip.armor,     ArmorType.Bag),
                AccessoryType = ParseEnum(c.equip.accessory, AccessoryType.Amulet),
                HasUnique     = c.equip.unique,
                
                // 지형 선호도
                Terrain = c.Preferred != null
                    ? new TerrainPreference
                    {
                        Urban  = ParseGrade(c.Preferred.Urban),
                        Field  = ParseGrade(c.Preferred.Field),
                        Indoor = ParseGrade(c.Preferred.Indoor)
                    }
                    : default,

                // UI 기본값
                IsLocked = false,
            });
        }

        return result;
    }

    // ════════════════════════════════════════
    // 검증
    // ════════════════════════════════════════

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
    
    // ════════════════════════════════════════
    // Enum 파싱
    // ════════════════════════════════════════

    private static T ParseEnum<T>(string raw, T fallback) where T : struct
    {
        if (string.IsNullOrWhiteSpace(raw)) return fallback;
        if (Enum.TryParse<T>(raw, true, out var value)) return value;

        Debug.LogWarning($"[CharDB] Enum 파싱 실패: {typeof(T).Name} raw='{raw}' → fallback={fallback}");
        return fallback;
    }
}
