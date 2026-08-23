using System.Collections.Generic;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine;

public sealed class CharacterArtProvider
{
    private readonly Dictionary<string, AsyncOperationHandle<Sprite>> cache = new();

    // Address 규칙은 여기 단 한 곳에서만 관리
    private static string Key(string id, CharacterCut cut) => cut switch
    {
        CharacterCut.Large => $"char/{id}/portrait_large",
        CharacterCut.Small => $"char/{id}/portrait_small",
        CharacterCut.Slot => $"char/{id}/portrait_slot",
        CharacterCut.Collection => $"char/{id}/portrait_collection",
        _ => $"char/{id}/portrait_small",
    };

    public AsyncOperationHandle<Sprite> LoadSprite(string id, CharacterCut cut)
    {
        string key = Key(id, cut);

        if (cache.TryGetValue(key, out var handle))
            return handle;

        var newHandle = Addressables.LoadAssetAsync<Sprite>(key);
        cache[key] = newHandle;
        return newHandle;
    }

    public void Release(string id, CharacterCut cut)
    {
        string key = Key(id, cut);

        if (!cache.TryGetValue(key, out var handle)) return;

        Addressables.Release(handle);
        cache.Remove(key);
    }

    public void ReleaseAll()
    {
        foreach (var kv in cache)
            Addressables.Release(kv.Value);

        cache.Clear();
    }
}