using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// IVirtualizedGrid.cs
/// <summary>
/// 가상화된 그리드의 공통 인터페이스
/// UniversalGridScroller가 다양한 그리드 타입을 지원하기 위해 사용
/// </summary>
/// 
public interface IVirtualizedGrid
{
    /// <summary>
    /// 스크롤 위치에 따라 보이는 슬롯을 갱신
    /// </summary>
    /// <param name="scrollY">현재 스크롤 Y 위치</param>
    void Refresh(float scrollY);
    
    /// <summary>
    /// 마지막 렌더링 상태를 초기화하고 강제로 다시 그리기
    /// </summary>
    void ForceRefresh();
}