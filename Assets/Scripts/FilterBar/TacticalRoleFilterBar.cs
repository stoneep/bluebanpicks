using UnityEngine;

public class TacticalRoleFilterBar : DynamicFilterBar<TacticalRole>
{
    private void Awake() => config = FilterBarConfig.Default;
    
    protected override string GetSpriteName(TacticalRole value) => 
        value.ToSpriteName();

    protected override string GetAllButtonSpriteName()
    {
        return "tacticalRole_all";
    }
    
    protected override IFilterButtonMediator CreateButtonMediator(TacticalRole? value)
    {
        if (!value.HasValue)
        {
            return FilterButtonMediatorFactory.CreateIconBgColorSwap(CombatTypeColor.TacticalRoleAll);
        }
    
        //return FilterButtonMediatorFactory.CreateGenericIconBgSwap(value.Value);
        return FilterButtonMediatorFactory.CreateIconBgColorSwap(value.Value.GetThemeColor());
    }
}