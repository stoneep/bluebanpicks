using UnityEngine;

public class DefenseTypeFilterBar : DynamicFilterBar<DefenseType>
{
    private void Awake() => config = FilterBarConfig.Default;
    
    protected override string GetSpriteName(DefenseType value) => value.ToCommonSpriteName();
    protected override string GetAllButtonText() => "All";

    protected override IFilterButtonMediator CreateButtonMediator(DefenseType? value)
    {
        if (!value.HasValue)
        {
            return FilterButtonMediatorFactory.CreateTextBgSwap(Palette.DeepBlue);
        }
        
        return FilterButtonMediatorFactory.CreateGenericIconBgSwap(value.Value);
    }
}
