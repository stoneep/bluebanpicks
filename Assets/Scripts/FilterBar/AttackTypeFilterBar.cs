using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackTypeFilterBar : DynamicFilterBar<AttackType>
{
    private void Awake() => config = FilterBarConfig.Default;

    protected override string GetSpriteName(AttackType value)
    {
        return value.ToCommonSpriteName(); 
    }
    
    protected override IFilterButtonMediator CreateButtonMediator(AttackType? value)
    {
        if (!value.HasValue)
        {
            // All 버튼
            return FilterButtonMediatorFactory.CreateGrayToggle(Color.white);
        }
        
        // Generic Mediator가 자동으로 GetThemeColor() 호출
        return FilterButtonMediatorFactory.CreateGenericIconBgSwap(value.Value);
    }
}