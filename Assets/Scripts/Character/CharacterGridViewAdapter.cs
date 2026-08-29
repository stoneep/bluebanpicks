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

    // ★ 추가: 확인 버튼을 누르기 전, 클릭으로 "선택"만 된 캐릭터 id (밴/픽 확정 아님).
    // null/빈 문자열이면 선택된 캐릭터 없음.
    private string selectedCharacterId;

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

    /// <summary>
    /// 밴/픽 확인 대기 중인 캐릭터를 지정(하이라이트 표시)한다. null/빈 문자열이면 선택 해제.
    /// 실제 밴/픽 제출과는 무관한 순수 UI 상태 - 제출은 확인 버튼에서 별도로 이루어진다.
    /// </summary>
    public void SetSelectedCharacter(string characterId)
    {
        selectedCharacterId = characterId;
        Refresh(jumpToTop: false);
    }

    private void BindSlot(int dataIndex, CharacterSlotView slot)
    {
        if (dataIndex < 0 || dataIndex >= currentItems.Count) return;

        var data = currentItems[dataIndex];
        bool isDraftUnavailable = availabilityPredicate != null && !availabilityPredicate(data.Id);
        bool isSelected = !string.IsNullOrEmpty(selectedCharacterId) && selectedCharacterId == data.Id;
        slot.Bind(dataIndex, data, OnClicked, artProvider, isDraftUnavailable, isSelected);
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
