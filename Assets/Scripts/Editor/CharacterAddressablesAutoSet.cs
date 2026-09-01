#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

public static class CharacterAddressablesAutoSet
{
    private const string DefaultRoot = "Assets/Art/Characters";
    private const string DefaultGroupName = "Characters";
    
    private static readonly Dictionary<string, string> SuffixToAddress = new(StringComparer.OrdinalIgnoreCase)
    {
        { "",           "portrait_large" },
        { "small",      "portrait_small" },
        { "collection", "portrait_collection" },
        { "slot",       "portrait_slot" },
    };

    [MenuItem("Tools/Addressables/Auto Set Character Sprite Addresses")]
    public static void AutoSetAddresses()
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            Debug.LogError("[CharAddr] AddressableAssetSettings not found. " +
                           "Create settings first (Window > Asset Management > Addressables > Groups).");
            return;
        }

        var roots = GetSelectedFolders();
        if (roots.Count == 0)
        {
            if (!AssetDatabase.IsValidFolder(DefaultRoot))
            {
                Debug.LogError($"[CharAddr] Default root folder not found: {DefaultRoot}");
                return;
            }
            roots.Add(DefaultRoot);
        }

        var group = GetOrCreateGroup(settings, DefaultGroupName);

        int totalFound = 0, totalSet = 0, totalSkipped = 0, totalErrors = 0;
        var changedLog = new List<string>();
        var errorLog = new List<string>();

        foreach (var root in roots)
        {
            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { root });

            foreach (var guid in guids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(assetPath)) continue;

                totalFound++;

                if (!TryParsePortraitFileName(assetPath, out var id, out var addressSuffix))
                {
                    totalSkipped++;
                    continue;
                }

                var desiredAddress = $"char/{id}/{addressSuffix}";

                var sprites = LoadSpritesAtPath(assetPath);
                if (sprites.Count == 0)
                {
                    totalErrors++;
                    errorLog.Add($"[ERROR] No sprites found: {assetPath}");
                    continue;
                }

                var mainGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(sprites[0]));
                var entry = settings.FindAssetEntry(mainGuid);
                entry = entry != null
                    ? settings.CreateOrMoveEntry(mainGuid, group)
                    : settings.CreateOrMoveEntry(mainGuid, group);

                if (entry.address == desiredAddress)
                {
                    totalSkipped++;
                    continue;
                }

                entry.address = desiredAddress;
                totalSet++;
                changedLog.Add($"[SET] {desiredAddress} <= {assetPath}");
            }
        }

        settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, null, true);
        AssetDatabase.SaveAssets();

        Debug.Log(
            "[CharAddr] Auto Set DONE\n" +
            $"  Roots   : {string.Join(", ", roots)}\n" +
            $"  Found   : {totalFound}\n" +
            $"  Set     : {totalSet}\n" +
            $"  Skipped : {totalSkipped}\n" +
            $"  Errors  : {totalErrors}\n" +
            $"  Group   : {DefaultGroupName}");

        if (changedLog.Count > 0) Debug.Log(string.Join("\n", changedLog));
        if (errorLog.Count > 0) Debug.LogWarning(string.Join("\n", errorLog));
    }
    
    private static bool TryParsePortraitFileName(string assetPath, out string id, out string addressSuffix)
    {
        id = null;
        addressSuffix = null;

        var fileName = Path.GetFileNameWithoutExtension(assetPath);
        var parts = fileName.Split('_');
        
        if (parts.Length < 3) return false;

        if (!parts[0].Equals("Student", StringComparison.OrdinalIgnoreCase) ||
            !parts[1].Equals("Portrait", StringComparison.OrdinalIgnoreCase))
            return false;

        id = parts[2];
        if (string.IsNullOrWhiteSpace(id)) return false;
        
        var suffix = parts.Length >= 4 ? parts[3] : "";

        if (!SuffixToAddress.TryGetValue(suffix, out addressSuffix))
        {
            id = null;
            return false;
        }

        return true;
    }

    private static List<Sprite> LoadSpritesAtPath(string assetPath)
    {
        var reps = AssetDatabase.LoadAllAssetRepresentationsAtPath(assetPath);
        var sprites = reps.OfType<Sprite>().ToList();

        if (sprites.Count == 0)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite != null) sprites.Add(sprite);
        }

        return sprites;
    }

    private static AddressableAssetGroup GetOrCreateGroup(AddressableAssetSettings settings, string groupName)
    {
        var group = settings.FindGroup(groupName);
        if (group != null) return group;

        return settings.CreateGroup(
            groupName, false, false, false,
            new List<AddressableAssetGroupSchema>
            {
                ScriptableObject.CreateInstance<BundledAssetGroupSchema>(),
                ScriptableObject.CreateInstance<ContentUpdateGroupSchema>()
            });
    }

    private static List<string> GetSelectedFolders()
    {
        var list = new List<string>();
        foreach (var obj in Selection.objects)
        {
            var path = AssetDatabase.GetAssetPath(obj);
            if (AssetDatabase.IsValidFolder(path))
                list.Add(path);
        }
        return list;
    }
}
#endif
