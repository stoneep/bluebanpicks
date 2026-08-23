using UnityEngine;

public class RoleFilterBar : DynamicFilterBar<Role>
{
    private void Awake() => config = FilterBarConfig.Default;
    
    protected override string GetSpriteName(Role value) =>
        $"role_{value.ToString().ToLowerInvariant()}";
    
    protected override IFilterButtonMediator CreateButtonMediator(Role? value)
    {
        if (!value.HasValue)
        {
            return FilterButtonMediatorFactory.CreateGrayToggle(Color.white);
        }
        
        return FilterButtonMediatorFactory.CreateGenericIconBgSwap(value.Value);
    }
}
