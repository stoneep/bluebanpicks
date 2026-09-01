using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class BaseFilterBar<T> : MonoBehaviour where T : struct, Enum
{
    [Header("Base Settings")]
    [SerializeField] protected UniversalFilterButton buttonPrefab;
    [SerializeField] protected Transform contentRoot;
    
    public T? CurrentValue { get; protected set; } = null;
    
    public event Action<T?> OnValueChanged;
    
    protected Dictionary<T, UniversalFilterButton> buttonMap = new();
    protected UniversalFilterButton allButton;
    
    private bool isInitialized = false;
   
    protected abstract void Initialize();
    
    protected virtual bool AllowToggle => true;
    
    public void SyncVisual(T? value)
    {
        CurrentValue = value;
        RefreshVisuals();
    }
    
    protected void OnItemClicked(T? clickedValue)
    {
        if (CurrentValue.Equals(clickedValue))
        {
            if (AllowToggle)
            {
                CurrentValue = null;
            }
            else
            {
                return;
            }
        }
        else
        {
            CurrentValue = clickedValue;
        }

        RefreshVisuals();
        OnValueChanged?.Invoke(CurrentValue);
    }
    
    protected void RefreshVisuals()
    {
        if (!isInitialized) return;
        
        if (allButton != null)
            allButton.SetSelected(CurrentValue == null);
        
        foreach (var kv in buttonMap)
        {
            bool isSelected = CurrentValue.HasValue && EqualityComparer<T>.Default.Equals(CurrentValue.Value, kv.Key);
            kv.Value.SetSelected(isSelected);
        }
    }
    
    protected void MarkAsInitialized()
    {
        isInitialized = true;
    }
    
    protected void SetValueAndNotify(T? value)
    {
        CurrentValue = value;
        RefreshVisuals();
        OnValueChanged?.Invoke(CurrentValue);
    }
    
    protected virtual void OnDisable()
    {
    }
}