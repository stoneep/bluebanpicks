using System;               
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public sealed class CharacterGridViewAdapter : MonoBehaviour, IFilteredListView<CharacterViewData>
{
    [SerializeField] private VirtualizedCharacterGrid grid;
    [FormerlySerializedAs("scroller"),SerializeField] private GridScroller charScroller;

    
    public event Action<CharacterViewData> OnCharacterPicked;

    private List<CharacterViewData> currentItems = new();
    private CharacterArtProvider artProvider;

    
    
    private Func<string, bool> availabilityPredicate;

    
    
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
        
        
        
    }

    
    
    
    
    public void SetAvailabilityPredicate(Func<string, bool> predicate) => availabilityPredicate = predicate;

    
    
    
    
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
        Debug.Log($"Clicked: {data.DisplayName}");   
        OnCharacterPicked?.Invoke(data);              
    }

    public void Refresh(bool jumpToTop)
    {
        if (jumpToTop) charScroller.JumpToTop();
        
        
        
        else grid.RebindVisible();
    }
}
