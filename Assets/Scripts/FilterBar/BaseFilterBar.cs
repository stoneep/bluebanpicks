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

    // ⭐ 초기화 완료 플래그 추가
    private bool isInitialized = false;
   
    protected abstract void Initialize();

    // ⭐ 클릭 동작 전략 - 자식 클래스에서 오버라이드 가능
    /// <summary>
    /// 이미 선택된 항목을 다시 클릭했을 때 선택 해제(null) 허용 여부
    /// - true: 토글 동작 (All 버튼이 있는 필터에 적합)
    /// - false: 필수 선택 (RefreshCycleTab처럼 항상 하나는 선택되어야 하는 경우)
    /// </summary>
    protected virtual bool AllowToggle => true;
    
    // 외부에서 강제로 값을 세팅하고 UI 갱신 (팝업 열 때 사용)
    public void SyncVisual(T? value)
    {
        CurrentValue = value;
        RefreshVisuals();
    }
    
    /// <summary>
    /// 항목 클릭 처리 - AllowToggleOff 설정에 따라 동작 결정
    /// </summary>
    protected void OnItemClicked(T? clickedValue)
    {
        // 이미 선택된 항목을 다시 클릭한 경우
        if (CurrentValue.Equals(clickedValue))
        {
            // 토글 해제가 허용되는 경우에만 null로 설정
            if (AllowToggle)
            {
                CurrentValue = null;
            }
            // AllowToggleOff가 false면 아무 동작도 하지 않음 (현재 선택 유지)
            else
            {
                return; // Early return - 이벤트도 발생시키지 않음
            }
        }
        else
        {
            // 다른 항목 선택
            CurrentValue = clickedValue;
        }

        RefreshVisuals();
        OnValueChanged?.Invoke(CurrentValue);
    }
    
    protected void RefreshVisuals()
    {
        // ⭐ 초기화되지 않았으면 실행하지 않음
        if (!isInitialized) return;
        
        // 1. All 버튼
        if (allButton != null)
            allButton.SetSelected(CurrentValue == null);

        // 2. 개별 버튼들
        foreach (var kv in buttonMap)
        {
            bool isSelected = CurrentValue.HasValue && EqualityComparer<T>.Default.Equals(CurrentValue.Value, kv.Key);
            kv.Value.SetSelected(isSelected);
        }
    }
    
    // ⭐ 초기화 완료 표시 헬퍼 메서드 추가
    protected void MarkAsInitialized()
    {
        isInitialized = true;
    }
    
    /// <summary>
    /// 외부에서 값을 강제 설정하고 이벤트 발생 (자식 클래스용)
    /// </summary>
    protected void SetValueAndNotify(T? value)
    {
        CurrentValue = value;
        RefreshVisuals();
        OnValueChanged?.Invoke(CurrentValue);
    }
    
    // ⭐ OnDisable에서 플래그 리셋 (선택사항)
    protected virtual void OnDisable()
    {
        // 씬 전환 등으로 비활성화될 때 플래그 유지
        // 필요시 isInitialized = false; 추가
    }
}