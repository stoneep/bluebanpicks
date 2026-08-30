using System;
using UnityEngine;

public class AffiliationFilterBar : DynamicFilterBar<Affiliation>
{
    private void Awake()
    {
        config = FilterBarConfig.Default
            .SetAtlasKey("atlas/icon_affiliation")
            .SetAutoSelectFirst(true);
    }
    
    protected override string GetSpriteName(Affiliation value)
    {
        return $"logo_{value.ToString().ToLowerInvariant()}";
    }

    protected override string GetAllButtonText() => "All";

    protected override IFilterButtonMediator CreateButtonMediator(Affiliation? value)
    {
        if (!value.HasValue)
        {
            // return FilterButtonMediatorFactory.CreateGrayToggle(
            //     Color.white, allTextColorDefault, allTextColorSelected);
            // All 버튼 (텍스트 전용) - 배경 <-> 텍스트 색상 스왑
            return FilterButtonMediatorFactory.CreateTextBgSwap(Palette.DeepBlue);
        }
        
        return FilterButtonMediatorFactory.CreateWhiteGrayBg();
    }
}