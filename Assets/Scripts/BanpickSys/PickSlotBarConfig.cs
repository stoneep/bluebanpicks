using System;
using UnityEngine;






[Serializable]
public class PickSlotBarConfig
{
    [Header("Slot Settings")]
    [SerializeField] private int slotCount = 5;

    
    
    
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