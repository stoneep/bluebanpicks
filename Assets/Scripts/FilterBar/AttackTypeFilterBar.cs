using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackTypeFilterBar : DynamicFilterBar<AttackType>
{
    private void Awake() => config = FilterBarConfig.Default;

    protected override string GetSpriteName(AttackType value) => 
        value.ToCommonSpriteName();
    protected override string GetAllButtonText() => "All";
    
    protected override IFilterButtonMediator CreateButtonMediator(AttackType? value)
    {
        if (!value.HasValue)
        {
            return FilterButtonMediatorFactory.CreateTextBgSwap(Palette.DeepBlue);
        }
        
        //return FilterButtonMediatorFactory.CreateGenericIconBgSwap(value.Value);
        return FilterButtonMediatorFactory.CreateIconBgColorSwap(value.Value.GetThemeColor());
    }
}