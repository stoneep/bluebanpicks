using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.U2D;
using UnityEngine.ResourceManagement.AsyncOperations;

public class AtlasImageBinder
{
    private int _token = 0;
    
    public void Bind(Image target, string atlasKey, string spriteName, Action<Image> onComplete = null)
    {
        if (target == null)
        {
            Debug.LogWarning("[AtlasImageBinder] Target image is null");
            return;
        }

        Release(target);
        int currentToken = ++_token;

        if (string.IsNullOrEmpty(spriteName))
        {
            Debug.LogWarning("[AtlasImageBinder] Sprite name is null or empty");
            return;
        }

        if (string.IsNullOrEmpty(atlasKey))
        {
            Debug.LogWarning("[AtlasImageBinder] Atlas key is null or empty");
            return;
        }

        var service = UIIconAtlasService.Instance;
        if (service == null)
        {
            Debug.LogError("[AtlasImageBinder] UIIconAtlasService.Instance is null! Make sure UIIconAtlasBootstrap is running.");
            return;
        }

        AsyncOperationHandle<SpriteAtlas> handle;
        
        try
        {
            handle = service.LoadAtlas(atlasKey);
        }
        catch (Exception e)
        {
            Debug.LogError($"[AtlasImageBinder] Error loading atlas '{atlasKey}': {e.Message}");
            return;
        }

        void OnLoaded(AsyncOperationHandle<SpriteAtlas> h)
        {
            if (_token != currentToken) return;
            
            if (target == null) return;
            
            if (h.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogWarning($"[AtlasImageBinder] Failed to load atlas '{atlasKey}'. Status: {h.Status}");
                return;
            }

            if (h.Result == null)
            {
                Debug.LogWarning($"[AtlasImageBinder] Atlas '{atlasKey}' loaded but result is null");
                return;
            }

            Sprite sprite = null;
            try
            {
                sprite = h.Result.GetSprite(spriteName);
            }
            catch (Exception e)
            {
                Debug.LogError($"[AtlasImageBinder] Error getting sprite '{spriteName}' from atlas '{atlasKey}': {e.Message}");
                return;
            }

            if (sprite != null)
            {
                try
                {
                    target.sprite = sprite;
                    target.enabled = true;
                    target.preserveAspect = true;
                    target.color = Color.white;
                    
                    onComplete?.Invoke(target);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[AtlasImageBinder] Error applying sprite to image: {e.Message}");
                }
            }
            else
            {
                Debug.LogWarning($"[AtlasImageBinder] Sprite '{spriteName}' not found in atlas '{atlasKey}'");
            }
        }

        try
        {
            if (handle.IsDone)
                OnLoaded(handle);
            else
                handle.Completed += OnLoaded;
        }
        catch (Exception e)
        {
            Debug.LogError($"[AtlasImageBinder] Error setting up completion callback: {e.Message}");
        }
    }

    public void Release(Image target)
    {
        _token++;
        if (target != null)
        {
            try
            {
                target.sprite = null;
                target.enabled = false;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[AtlasImageBinder] Error during release: {e.Message}");
            }
        }
    }
}
