using UnityEngine;

public class RoleFilterBar : DynamicFilterBar<Role>
{
    [Header("All Button Text Color")]
    [SerializeField] private Color allTextColorDefault = Color.white;
    [SerializeField] private Color allTextColorSelected = Color.white;

    private void Awake() => config = FilterBarConfig.Default;

    protected override string GetSpriteName(Role value) =>
        $"role_{value.ToString().ToLowerInvariant()}";
    
    protected override string GetAllButtonText() => "ALL";

    protected override IFilterButtonMediator CreateButtonMediator(Role? value)
    {
        if (!value.HasValue)
        {
            return FilterButtonMediatorFactory.CreateGrayToggle(
                Color.white, allTextColorDefault, allTextColorSelected);
        }

        return FilterButtonMediatorFactory.CreateGenericIconBgSwap(value.Value);
    }
}