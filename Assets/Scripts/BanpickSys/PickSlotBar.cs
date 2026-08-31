using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 고정 개수의 PickedCharacterView 슬롯을 한 줄(HorizontalLayoutGroup)로 생성/관리하는 바.
///
/// DynamicFilterBar&lt;T&gt;가 "Enum 값 하나당 버튼 하나"를 만드는 것과 동일한 구조로,
/// 이 클래스는 "config.SlotCount 만큼 PickedCharacterView 슬롯"을 만든다.
/// Enum이 없으므로 Base/Dynamic으로 나눌 필요가 없어 훨씬 단순한 단일 클래스로 충분함.
///
/// 배치는 contentRoot에 붙인 HorizontalLayoutGroup(1줄, 고정 개수)이 담당한다.
/// 슬롯 수가 5~6개 수준으로 적고 스크롤/재활용이 필요 없으므로
/// VirtualizedCharacterGrid 같은 풀링/가상화 구조는 과함 - DynamicFilterBar처럼
/// Initialize 시 한 번에 Instantiate하는 방식이 가장 간단하다.
/// </summary>
public sealed class PickSlotBar : MonoBehaviour
{
    [Header("Base Settings")]
    [SerializeField] private PickedCharacterView slotPrefab;
    [SerializeField] private Transform contentRoot; // HorizontalLayoutGroup 부착 (1줄 고정)

    [Header("Config")]
    [SerializeField] private PickSlotBarConfig config = new();

    private readonly List<PickedCharacterView> slots = new();
    private bool isInitialized;

    public int SlotCount => config.SlotCount;
    public IReadOnlyList<PickedCharacterView> Slots => slots;

    private void Awake() => SafeInitialize();

    /// <summary>
    /// 런타임에 슬롯 개수를 바꾸고 재생성.
    /// 밴픽/선택픽 슬롯 수는 기획에 따라 바뀔 수 있으므로 이 한 줄만 호출하면 됨.
    /// </summary>
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

    // ==================== Public API ====================

    public void SetCharacter(int index, string characterId)
    {
        SafeInitialize(); // root가 비활성 상태라 Awake()가 아직 안 돌았을 수 있으므로 여기서도 보장
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
