using System;               // ← 추가
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public sealed class CharacterGridViewAdapter : MonoBehaviour, IFilteredListView<CharacterViewData>
{
    [SerializeField] private VirtualizedCharacterGrid grid;
    [FormerlySerializedAs("scroller"),SerializeField] private GridScroller charScroller;

    // ★ 추가: 밴픽 등 외부 UI가 구독할 이벤트
    public event Action<CharacterViewData> OnCharacterPicked;

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
        grid.SetTotalCount(currentItems.Count);
        charScroller.JumpToTop();
    }

    private void BindSlot(int dataIndex, CharacterSlotView slot)
    {
        if (dataIndex < 0 || dataIndex >= currentItems.Count) return;
        slot.Bind(dataIndex, currentItems[dataIndex], OnClicked, artProvider);
    }

    private void OnClicked(int index)
    {
        var data = currentItems[index];
        Debug.Log($"Clicked: {data.DisplayName}");   // 기존 디버그는 그대로 유지
        OnCharacterPicked?.Invoke(data);              // ★ 밴픽용 이벤트 발행
    }

    public void Refresh(bool jumpToTop)
    {
        if (jumpToTop) charScroller.JumpToTop();
        else grid.Refresh(0);
    }
}