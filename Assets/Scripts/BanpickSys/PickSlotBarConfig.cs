using System;
using UnityEngine;

/// <summary>
/// PickSlotBar 설정.
/// FilterBarConfig와 동일하게 Getter/Setter + Fluent API 패턴을 따름.
/// Enum 대신 "슬롯 개수"만 있으면 되므로 훨씬 단순함.
/// </summary>
[Serializable]
public class PickSlotBarConfig
{
    [Header("Slot Settings")]
    [SerializeField] private int slotCount = 5;

    /// <summary>
    /// 표시할 슬롯 개수 (밴픽 5칸, 선택픽 6칸 등 기획에 따라 가변)
    /// </summary>
    public int SlotCount
    {
        get => slotCount;
        set => slotCount = Mathf.Max(0, value);
    }

    public static PickSlotBarConfig Of(int count) => new PickSlotBarConfig().SetSlotCount(count);

    public PickSlotBarConfig SetSlotCount(int count)
    {
        slotCount = Mathf.Max(0, count);
        return this;
    }

    public bool IsValid() => slotCount >= 0;

    public override string ToString() => $"PickSlotBarConfig [SlotCount:{slotCount}]";
}