using System;
using System.Collections.Generic;
using UnityEngine;

public enum PickSlotFillDirection
{
    FromStart, // 기존과 동일: 논리적 0번 슬롯 = contentRoot의 첫 번째 자식 (세로 바는 항상 이거)
    FromEnd    // 논리적 0번 슬롯 = contentRoot의 마지막 자식 (가로 바에서 오른쪽부터 채울 때)
}

public sealed class PickSlotBar : MonoBehaviour
{
    [Header("Base Settings")]
    [SerializeField] private PickedCharacterView slotPrefab;
    [SerializeField] private Transform contentRoot; 

    [Header("Layout")]
    [Tooltip("가로 배치 바에서만 의미 있음. 세로 바는 FromStart로 두면 기존 동작 그대로.")]
    [SerializeField] private PickSlotFillDirection fillDirection = PickSlotFillDirection.FromStart;
    
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

        ApplyFillDirection();
    }

    private void CleanupExisting()
    {
        foreach (var s in slots)
        {
            if (s) Destroy(s.gameObject);
        }
        slots.Clear();
    }

    // 논리적 인덱스(=픽/밴 제출 순서, slots 리스트의 인덱스)는 그대로 두고,
    // contentRoot 안에서의 실제 표시 위치(sibling index)만 뒤집는다.
    private void ApplyFillDirection()
    {
        if (fillDirection != PickSlotFillDirection.FromEnd) return;

        for (int i = 0; i < slots.Count; i++)
            slots[i].transform.SetSiblingIndex(slots.Count - 1 - i);
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
    
    public void HighlightNextSlot(int index)
    {
        SafeInitialize();
        for (int i = 0; i < slots.Count; i++)
        {
            slots[i].SetNextTurnHighlight(i == index);
        }
    }

    
    public void ClearNextSlotHighlight()
    {
        SafeInitialize();
        foreach (var s in slots) s.SetNextTurnHighlight(false);
    }

    public void SetPendingCharacter(int index, string characterId)
    {
        SafeInitialize();
        if (!IsValidIndex(index)) return;
        slots[index].ShowPending(characterId);
    }

    public void ClearPendingCharacter(int index)
    {
        SafeInitialize();
        if (!IsValidIndex(index)) return;
        slots[index].ClearPending();
    }
    
    private bool IsValidIndex(int index) => index >= 0 && index < slots.Count;
}
