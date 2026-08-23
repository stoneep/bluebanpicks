using UnityEngine;

/// <summary>
/// UIIconAtlasService 자동 초기화 부트스트랩 (개선 버전)
/// 
/// ⭐ 개선 사항:
/// 1. 중복 생성 완벽 방지 (GameObject 체크 추가)
/// 2. 초기화 로그로 디버깅 용이
/// 3. 에디터 모드 안전성
/// </summary>
public static class UIIconAtlasBootstrap
{
    
    private static bool _initialized = false;
    
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        // ⭐ 중복 초기화 방지 (정적 플래그)
        if (_initialized)
        {
            Debug.LogWarning("[UIIconAtlasBootstrap] 이미 초기화됨 - 스킵");
            return;
        }
        
        // ⭐ 1. Instance 체크 (이미 존재하면 스킵)
        if (UIIconAtlasService.Instance != null)
        {
            Debug.Log("[UIIconAtlasBootstrap] UIIconAtlasService.Instance 이미 존재 - 스킵");
            _initialized = true;
            return;
        }
        
        // ⭐ 2. GameObject 체크 (씬에 이미 있으면 스킵)
        var existing = GameObject.Find("UIIconAtlasService");
        if (existing != null)
        {
            Debug.LogWarning("[UIIconAtlasBootstrap] GameObject 'UIIconAtlasService'가 씬에 이미 존재함");
            
            // Instance가 null인데 GameObject는 있는 경우 - 컴포넌트 확인
            var service = existing.GetComponent<UIIconAtlasService>();
            if (service == null)
            {
                Debug.LogError("[UIIconAtlasBootstrap] GameObject는 있지만 컴포넌트가 없음 - 추가");
                existing.AddComponent<UIIconAtlasService>();
            }
            
            _initialized = true;
            return;
        }
        
        // ⭐ 3. 새로 생성
        var go = new GameObject("UIIconAtlasService");
        go.AddComponent<UIIconAtlasService>();
        
        _initialized = true;
        Debug.Log("[UIIconAtlasBootstrap] UIIconAtlasService 생성 완료");
    }
    
#if UNITY_EDITOR
    /// <summary>
    /// 에디터 모드에서 플레이 종료 시 플래그 리셋
    /// </summary>
    [UnityEditor.InitializeOnEnterPlayMode]
    private static void OnEnterPlayMode()
    {
        _initialized = false;
    }
#endif

}