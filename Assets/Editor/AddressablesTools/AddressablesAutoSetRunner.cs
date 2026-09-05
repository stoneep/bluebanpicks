#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

public static class AddressablesAutoSetRunner
{
    // 접미사 → 주소 suffix 매핑 (CharacterAddressablesAutoSet과 동일 규칙)
    private static readonly Dictionary<string, string> PortraitSuffixMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "large",           "portrait_large" },
        { "small",      "portrait_small" },
        { "collection", "portrait_collection" },
        { "slot",       "portrait_slot" },
    };

    [MenuItem("Tools/Addressables/Auto Set Addresses (Rules)")]
    public static void Run()
    {
        var rulesAsset = FindRulesAsset();
        if (rulesAsset == null)
        {
            Debug.LogError("[AutoSet] AddressablesAutoSetRules asset not found. " +
                           "Create one via: Create > Tools > Addressables > Auto Set Rules");
            return;
        }

        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            Debug.LogError("[AutoSet] Addressables settings not found.");
            return;
        }

        int totalFound = 0, totalSet = 0, totalSkipped = 0, totalErrors = 0;

        foreach (var rule in rulesAsset.rules)
        {
            if (string.IsNullOrWhiteSpace(rule.rootFolder) || !AssetDatabase.IsValidFolder(rule.rootFolder))
            {
                Debug.LogError($"[AutoSet] Invalid folder: {rule.rootFolder}");
                totalErrors++;
                continue;
            }

            var group = settings.FindGroup(rule.groupName) ??
                        settings.CreateGroup(rule.groupName, false, false, false, null,
                            typeof(UnityEditor.AddressableAssets.Settings.GroupSchemas.BundledAssetGroupSchema));

            var guids = AssetDatabase.FindAssets("t:Sprite", new[] { rule.rootFolder });
            totalFound += guids.Length;

            foreach (var guid in guids)
            {
                try
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var fileNameNoExt = Path.GetFileNameWithoutExtension(path);

                    string address = rule.mode switch
                    {
                        AddressablesAutoSetRules.RuleMode.ByFileNameLower
                            => rule.addressPrefix + fileNameNoExt.ToLowerInvariant(),

                        AddressablesAutoSetRules.RuleMode.OnlyThisFileName
                            => BuildOnlyThisFileNameAddress(rule, fileNameNoExt),

                        AddressablesAutoSetRules.RuleMode.CharacterPortraitByFolderAndCut
                            => BuildCharacterPortraitAddress(path),

                        _ => null
                    };

                    if (string.IsNullOrEmpty(address))
                    {
                        totalSkipped++;
                        continue;
                    }

                    var entry = settings.FindAssetEntry(guid) ?? settings.CreateOrMoveEntry(guid, group);

                    if (entry.address == address)
                    {
                        totalSkipped++;
                        continue;
                    }

                    entry.address = address;
                    totalSet++;
                    Debug.Log($"[SET] {address} <= {path}");
                }
                catch (Exception e)
                {
                    totalErrors++;
                    Debug.LogError($"[ERROR] {guid}: {e.Message}");
                }
            }
        }

        settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, null, true);
        AssetDatabase.SaveAssets();

        Debug.Log($"[AutoSet] DONE | Found: {totalFound} | Set: {totalSet} | Skipped: {totalSkipped} | Errors: {totalErrors}");
    }

    private static AddressablesAutoSetRules FindRulesAsset()
    {
        var guids = AssetDatabase.FindAssets("t:AddressablesAutoSetRules");
        if (guids == null || guids.Length == 0) return null;
        var path = AssetDatabase.GUIDToAssetPath(guids[0]);
        return AssetDatabase.LoadAssetAtPath<AddressablesAutoSetRules>(path);
    }

    private static string BuildOnlyThisFileNameAddress(AddressablesAutoSetRules.Rule rule, string fileNameNoExt)
    {
        if (string.IsNullOrWhiteSpace(rule.onlyThisFileNameNoExt)) return null;
        if (!string.Equals(fileNameNoExt, rule.onlyThisFileNameNoExt, StringComparison.Ordinal)) return null;
        return rule.addressPrefix + rule.onlyThisFileNameNoExt.ToLowerInvariant();
    }

    /// <summary>
    /// Student_Portrait_{Id}            → char/{Id}/portrait_large
    /// Student_Portrait_{Id}_Small      → char/{Id}/portrait_small
    /// Student_Portrait_{Id}_Collection → char/{Id}/portrait_collection
    /// Student_Portrait_{Id}_Slot       → char/{Id}/portrait_slot
    /// </summary>
    private static string BuildCharacterPortraitAddress(string assetPath)
    {
        var fileNameNoExt = Path.GetFileNameWithoutExtension(assetPath);
        var parts = fileNameNoExt.Split('_');

        // 최소 3덩어리: Student_Portrait_{Id}
        if (parts.Length < 3) return null;

        if (!parts[0].Equals("Student", StringComparison.OrdinalIgnoreCase) ||
            !parts[1].Equals("Portrait", StringComparison.OrdinalIgnoreCase))
            return null;

        var id = parts[2];
        if (string.IsNullOrWhiteSpace(id)) return null;

        var suffix = parts.Length >= 4 ? parts[3] : "";

        return PortraitSuffixMap.TryGetValue(suffix, out var addressSuffix)
            ? $"char/{id}/{addressSuffix}"
            : null;
    }
}
#endif
