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

    protected override IFilterButtonMediator CreateButtonMediator(Affiliation? value)
    {
        if (!value.HasValue)
        {
            // All 버튼
            return FilterButtonMediatorFactory.CreateGrayToggle(Color.white);
        }
        
        return FilterButtonMediatorFactory.CreateWhiteGrayBg();
    }
}