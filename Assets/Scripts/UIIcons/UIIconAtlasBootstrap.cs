using UnityEngine;

public static class UIIconAtlasBootstrap
{
    
    private static bool _initialized = false;
    
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        if (_initialized)
        {
            Debug.LogWarning("[UIIconAtlasBootstrap] 이미 초기화됨 - 스킵");
            return;
        }
        
        if (UIIconAtlasService.Instance != null)
        {
            Debug.Log("[UIIconAtlasBootstrap] UIIconAtlasService.Instance 이미 존재 - 스킵");
            _initialized = true;
            return;
        }
        
        var existing = GameObject.Find("UIIconAtlasService");
        if (existing != null)
        {
            Debug.LogWarning("[UIIconAtlasBootstrap] GameObject 'UIIconAtlasService'가 씬에 이미 존재함");
            
            var service = existing.GetComponent<UIIconAtlasService>();
            if (service == null)
            {
                Debug.LogError("[UIIconAtlasBootstrap] GameObject는 있지만 컴포넌트가 없음 - 추가");
                existing.AddComponent<UIIconAtlasService>();
            }
            
            _initialized = true;
            return;
        }
        
        var go = new GameObject("UIIconAtlasService");
        go.AddComponent<UIIconAtlasService>();
        
        _initialized = true;
    }
    
#if UNITY_EDITOR
    [UnityEditor.InitializeOnEnterPlayMode]
    private static void OnEnterPlayMode()
    {
        _initialized = false;
    }
#endif

}