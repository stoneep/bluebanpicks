using System;
using System.Collections.Generic;
using UnityEngine;













public sealed class PickSlotBar : MonoBehaviour
{
    [Header("Base Settings")]
    [SerializeField] private PickedCharacterView slotPrefab;
    [SerializeField] private Transform contentRoot; 

    [Header("Config")]
    [SerializeField] private PickSlotBarConfig config = new();

    private readonly List<PickedCharacterView> slots = new();
    private bool isInitialized;

    public int SlotCount => config.SlotCount;
    public IReadOnlyList<PickedCharacterView> Slots => slots;

    private void Awake() => SafeInitialize();

    
    
    
    
    public void ApplyConfig(PickSlotBarConfig newConfig)
    {
        config = newConfig ?? new PickSlotBarConfig();
        isInitialized = false;
        SafeInitialize();
    }

    private void SafeInitialize()
    {
        if (isInitialized) return;

        if (!slotPrefab || !contentRoot)
        {
            Debug.LogError($"[{nameof(PickSlotBar)}] slotPrefab/contentRoot가 할당되지 않았습니다.");
            return;
        }

        if (!config.IsValid())
        {
            Debug.LogError($"[{nameof(PickSlotBar)}] 잘못된 설정: {config}");
            return;
        }

        try
        {
            Initialize();
            isInitialized = true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[{nameof(PickSlotBar)}] 초기화 실패: {e.Message}\n{e.StackTrace}");
        }
    }

    private void Initialize()
    {
        CleanupExisting();

        for (int i = 0; i < config.SlotCount; i++)
        {
            var slot = Instantiate(slotPrefab, contentRoot);
            slot.name = $"Slot_{i:00}";
            slot.Clear();
            slots.Add(slot);
        }
    }

    private void CleanupExisting()
    {
        foreach (var s in slots)
        {
            if (s) Destroy(s.gameObject);
        }
        slots.Clear();
    }

    

    public void SetCharacter(int index, string characterId)
    {
        SafeInitialize(); 
        if (!IsValidIndex(index)) return;
        slots[index].Show(characterId);
    }

    public void SetCharacter(int index, CharacterViewData data) => SetCharacter(index, data.Id);

    public void ClearSlot(int index)
    {
        SafeInitialize();
        if (!IsValidIndex(index)) return;
        slots[index].Clear();
    }

    public void ClearAll()
    {
        SafeInitialize();
        foreach (var s in slots) s.Clear();
    }

    private bool IsValidIndex(int index) => index >= 0 && index < slots.Count;
}
