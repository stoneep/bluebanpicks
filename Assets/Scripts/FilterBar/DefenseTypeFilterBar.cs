using UnityEngine;

public class DefenseTypeFilterBar : DynamicFilterBar<DefenseType>
{
    private void Awake() => config = FilterBarConfig.Default;
    
    protected override string GetSpriteName(DefenseType value)
    {
        return value.ToCommonSpriteName();
    }
    
    protected override IFilterButtonMediator CreateButtonMediator(DefenseType? value)
    {
        if (!value.HasValue)
        {
            // All 버튼
            return FilterButtonMediatorFactory.CreateGrayToggle(Color.white);
        }
        
        return FilterButtonMediatorFactory.CreateGenericIconBgSwap(value.Value);
    }
}
