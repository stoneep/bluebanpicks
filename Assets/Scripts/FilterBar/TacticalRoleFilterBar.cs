using UnityEngine;

public class TacticalRoleFilterBar : DynamicFilterBar<TacticalRole>
{
    private void Awake() => config = FilterBarConfig.Default;
    
    protected override string GetSpriteName(TacticalRole value)
    {
        return value.ToSpriteName(); 
    }
    
    protected override IFilterButtonMediator CreateButtonMediator(TacticalRole? value)
    {
        if (!value.HasValue)
        {
            return FilterButtonMediatorFactory.CreateGrayToggle(Color.white);
        }
        
        return FilterButtonMediatorFactory.CreateGenericIconBgSwap(value.Value);
    }
}