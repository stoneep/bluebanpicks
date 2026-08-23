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

    // ★ 추가: 밴픽 등에서 "이 캐릭터를 지금 고를 수 있는가"를 물어보는 콜백.
    // 미할당이면 항상 선택 가능한 것으로 간주 (기존 동작 그대로 유지).
    private Func<string, bool> availabilityPredicate;

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

    /// <summary>
    /// 밴픽 등 외부에서 캐릭터 가용 여부를 판단하는 콜백을 등록.
    /// 등록 후 리스트를 다시 그려야(예: Refresh) 오버레이에 반영된다.
    /// </summary>
    public void SetAvailabilityPredicate(Func<string, bool> predicate) => availabilityPredicate = predicate;

    private void BindSlot(int dataIndex, CharacterSlotView slot)
    {
        if (dataIndex < 0 || dataIndex >= currentItems.Count) return;

        var data = currentItems[dataIndex];
        bool isDraftUnavailable = availabilityPredicate != null && !availabilityPredicate(data.Id);
        slot.Bind(dataIndex, data, OnClicked, artProvider, isDraftUnavailable);
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
