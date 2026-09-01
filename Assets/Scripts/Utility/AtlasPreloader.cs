using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

public class AtlasPreloader
{
    private UIIconAtlasService AtlasService => UIIconAtlasService.Instance;
    private HashSet<string> loadedAtlases = new HashSet<string>();
    private int pendingLoads = 0;
    
    public void LoadAtlases(IEnumerable<string> atlasKeys, Action onComplete)
    {
        if (AtlasService == null)
        {
            Debug.LogError("[AtlasPreloader] UIIconAtlasService.Instance is null!");
            onComplete?.Invoke();
            return;
        }

        HashSet<string> uniqueAtlases = new HashSet<string>(atlasKeys);
        
        int totalCount = uniqueAtlases.Count;
        if (totalCount == 0)
        {
            onComplete?.Invoke();
            return;
        }

        pendingLoads = totalCount;
        loadedAtlases.Clear();

        foreach (string atlasKey in uniqueAtlases)
        {
            if (string.IsNullOrEmpty(atlasKey))
            {
                OnAtlasLoaded(onComplete);
                continue;
            }

            if (AtlasService.IsAtlasReady(atlasKey))
            {
                loadedAtlases.Add(atlasKey);
                OnAtlasLoaded(onComplete);
            }
            else
            {
                var handle = AtlasService.LoadAtlas(atlasKey);
                handle.Completed += asyncHandle => 
                {
                    if (asyncHandle.Status == AsyncOperationStatus.Succeeded)
                    {
                        loadedAtlases.Add(atlasKey);
                    }
                    else
                    {
                        Debug.LogWarning($"[AtlasPreloader] 아틀라스 로드 실패: {atlasKey}");
                    }
                    
                    OnAtlasLoaded(onComplete);
                };
            }
        }
    }

    private void OnAtlasLoaded(Action onComplete)
    {
        pendingLoads--;

        if (pendingLoads <= 0)
        {
            onComplete?.Invoke();
        }
    }
    
    public bool IsAtlasLoaded(string atlasKey)
    {
        return loadedAtlases.Contains(atlasKey);
    }
    
    public int GetLoadedCount()
    {
        return loadedAtlases.Count;
    }
}
