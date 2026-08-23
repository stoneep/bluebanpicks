using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.U2D;

public sealed class UIIconAtlasService : MonoBehaviour
{
    
    public static UIIconAtlasService Instance { get; private set; }

    // atlasKey -> atlas handle
    private readonly Dictionary<string, AsyncOperationHandle<SpriteAtlas>> atlasHandles = new();
    
    // atlasKey -> atlas (로드 완료 후)
    private readonly Dictionary<string, SpriteAtlas> atlasCache = new();
    
    // (atlasKey, spriteName) -> sprite (조회 결과 캐시)
    private readonly Dictionary<(string atlasKey, string spriteName), Sprite> spriteCache = new();
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this); 
            return;
        }
        Instance = this;
        
        if (transform.parent == null) DontDestroyOnLoad(gameObject);
    }


    public AsyncOperationHandle<SpriteAtlas> LoadAtlas(string atlasKey)
    {
        if (string.IsNullOrWhiteSpace(atlasKey))
            throw new ArgumentException("atlasKey is null/empty.", nameof(atlasKey));

        if (atlasHandles.TryGetValue(atlasKey, out var existing))
            return existing;

        var handle = Addressables.LoadAssetAsync<SpriteAtlas>(atlasKey);
        atlasHandles[atlasKey] = handle;

        // 완료 시 atlasCache에 저장
        handle.Completed += op =>
        {
            if (op.Status == AsyncOperationStatus.Succeeded && op.Result != null)
                atlasCache[atlasKey] = op.Result;
        };

        return handle;
    }
    
    public Sprite GetSprite(string atlasKey, string spriteName)
    {
        if (string.IsNullOrWhiteSpace(atlasKey) || string.IsNullOrWhiteSpace(spriteName))
            return null;

        var key = (atlasKey, spriteName);

        // 1) sprite 캐시 히트
        if (spriteCache.TryGetValue(key, out var cached) && cached != null)
            return cached;

        // 2) atlas가 로드 완료되었는지 확인
        if (!TryGetReadyAtlas(atlasKey, out var atlas))
            return null;

        // 3) atlas에서 sprite 조회 + 캐시
        var sprite = atlas.GetSprite(spriteName);
        if (sprite != null)
            spriteCache[key] = sprite;

        return sprite;
    }

    public bool TryGetSprite(string atlasKey, string spriteName, out Sprite sprite)
    {
        sprite = GetSprite(atlasKey, spriteName);
        return sprite != null;
    }
    
    public void GetSpriteAsync(string atlasKey, string spriteName, Action<Sprite> onDone)
    {
        if (onDone == null) return;

        var s = GetSprite(atlasKey, spriteName);
        if (s != null)
        {
            onDone(s);
            return;
        }

        var h = LoadAtlas(atlasKey);
        void Apply(AsyncOperationHandle<SpriteAtlas> h)
        {
            if (h.Status != AsyncOperationStatus.Succeeded)
            {
                onDone?.Invoke(null);
                return;
            }

            var atlas = h.Result;
            
            var sprite = atlas != null ? atlas.GetSprite(spriteName) : null;
            if (sprite != null)
                spriteCache[(atlasKey, spriteName)] = sprite;
            onDone?.Invoke(sprite);
        }

        if (h.IsDone) Apply(h);
        else h.Completed += Apply;
    }

    public bool IsAtlasReady(string atlasKey)
    {
        return TryGetReadyAtlas(atlasKey, out _);
    }

    private bool TryGetReadyAtlas(string atlasKey, out SpriteAtlas atlas)
    {
        atlas = null;

        // atlasCache 우선
        if (atlasCache.TryGetValue(atlasKey, out atlas) && atlas != null)
            return true;

        // handle이 있고 로드 완료면 cache에 넣고 반환
        if (atlasHandles.TryGetValue(atlasKey, out var h))
        {
            if (h.IsValid() && h.IsDone && h.Status == AsyncOperationStatus.Succeeded && h.Result != null)
            {
                atlas = h.Result;
                atlasCache[atlasKey] = atlas;
                return true;
            }
        }

        return false;
    }

    public void ClearSpriteCache(string atlasKey)
    {
        if (string.IsNullOrWhiteSpace(atlasKey)) return;

        // atlasKey에 해당하는 spriteCache 엔트리를 제거
        var removeKeys = new List<(string, string)>();
        foreach (var k in spriteCache.Keys)
        {
            if (k.atlasKey == atlasKey)
                removeKeys.Add(k);
        }
        for (int i = 0; i < removeKeys.Count; i++)
            spriteCache.Remove(removeKeys[i]);
    }

    public void ReleaseAll()
    {
        foreach (var kv in atlasHandles)
        {
            if (kv.Value.IsValid())
                Addressables.Release(kv.Value);
        }
        
        
        atlasHandles.Clear();
        atlasCache.Clear();
        spriteCache.Clear();
    }
    
}
