using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public sealed class CharacterGridViewAdapter : MonoBehaviour, IFilteredListView<CharacterViewData>
{
    [SerializeField] private VirtualizedCharacterGrid grid;
    [FormerlySerializedAs("scroller"),SerializeField] private GridScroller charScroller;
    
    private List<CharacterViewData> currentItems = new();
    private CharacterArtProvider artProvider;
    
    private void Awake()
    {
        artProvider = new CharacterArtProvider();
        grid.OnRequestBind += BindSlot;
    }
    
    private void OnDestroy() => artProvider?.ReleaseAll();

    public void SetData(List<CharacterViewData> data)
    {
        currentItems = data ?? new List<CharacterViewData>();
        
        // [디버그] 실제로 화면에 표시될 개수 로그
        //Debug.Log($"[Adapter] 필터링 결과: {currentItems.Count}개가 화면에 표시됩니다.");
        
        grid.SetTotalCount(currentItems.Count);
        charScroller.JumpToTop();
    }
    
    private void BindSlot(int dataIndex, CharacterSlotView slot)
    {
        if (dataIndex < 0 || dataIndex >= currentItems.Count) return;
        
        // 실제 데이터 바인딩 로직만 수행
        slot.Bind(dataIndex, currentItems[dataIndex], OnClicked, artProvider);
    }
    
    private void OnClicked(int index) => Debug.Log($"Clicked: {currentItems[index].DisplayName}");
    
    public void Refresh(bool jumpToTop)
    {
        if (jumpToTop) charScroller.JumpToTop();
        else grid.Refresh(0); // 현재 스크롤 위치 기반 갱신 로직 필요
    }
}