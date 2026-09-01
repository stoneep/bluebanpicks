using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public sealed class AddressableSpriteProvider<TVariant> where TVariant : System.Enum
{
    private readonly Dictionary<string, AsyncOperationHandle<Sprite>> _cache = new();
    private readonly System.Func<string, TVariant, string> _keyBuilder;
    
    public AddressableSpriteProvider(System.Func<string, TVariant, string> keyBuilder)
    {
        _keyBuilder = keyBuilder;
    }

    public AsyncOperationHandle<Sprite> Load(string id, TVariant variant)
    {
        string key = _keyBuilder(id, variant);
        if (_cache.TryGetValue(key, out var handle)) return handle;

        var h = Addressables.LoadAssetAsync<Sprite>(key);
        _cache[key] = h;
        return h;
    }

    public void Release(string id, TVariant variant)
    {
        string key = _keyBuilder(id, variant);
        if (!_cache.TryGetValue(key, out var handle)) return;
        Addressables.Release(handle);
        _cache.Remove(key);
    }

    public void ReleaseAll()
    {
        foreach (var kv in _cache) Addressables.Release(kv.Value);
        _cache.Clear();
    }
}