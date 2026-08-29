using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// 아틀라스 프리로딩을 담당하는 유틸리티 클래스
/// 여러 UI 컨트롤러에서 공통으로 사용 가능
/// </summary>
public class AtlasPreloader
{
    private UIIconAtlasService AtlasService => UIIconAtlasService.Instance;
    private HashSet<string> loadedAtlases = new HashSet<string>();
    private int pendingLoads = 0;

    /// <summary>
    /// 필요한 모든 아틀라스를 미리 로드
    /// </summary>
    /// <param name="atlasKeys">로드할 아틀라스 키 목록</param>
    /// <param name="onComplete">완료 콜백</param>
    public void LoadAtlases(IEnumerable<string> atlasKeys, Action onComplete)
    {
        if (AtlasService == null)
        {
            Debug.LogError("[AtlasPreloader] UIIconAtlasService.Instance is null!");
            onComplete?.Invoke();
            return;
        }

        HashSet<string> uniqueAtlases = new HashSet<string>(atlasKeys);
        
        // 빈 목록인 경우 즉시 완료
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
//                        Debug.Log($"[AtlasPreloader] 아틀라스 로드 성공: {atlasKey}");
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
//            Debug.Log($"[AtlasPreloader] 모든 아틀라스 로드 완료: {loadedAtlases.Count}개");
            onComplete?.Invoke();
        }
    }

    /// <summary>
    /// 특정 아틀라스가 로드되었는지 확인
    /// </summary>
    public bool IsAtlasLoaded(string atlasKey)
    {
        return loadedAtlases.Contains(atlasKey);
    }

    /// <summary>
    /// 로드된 아틀라스 개수 반환
    /// </summary>
    public int GetLoadedCount()
    {
        return loadedAtlases.Count;
    }
}
