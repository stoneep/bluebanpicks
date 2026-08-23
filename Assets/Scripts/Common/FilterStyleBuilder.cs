using UnityEngine;

/// <summary>
/// 스타일 빌더 (더 복잡한 설정이 필요한 경우)
/// </summary>

public enum StyleType
{
    OpacityToggle,
    BackgroundToggle,
    BlackIconWhiteBg,
    OnlyBgChange,
    TextOnly
}

public class FilterStyleBuilder
{
    private string iconAtlas;
    private string iconSprite;
    private string bgAtlas;
    private string bgSprite;
    private Color? baseColor;
    private StyleType sType;
    
    
    public FilterStyleBuilder(StyleType sType)
    {
        this.sType = sType;
    }
    
    public FilterStyleBuilder WithBackground(string atlas, string sprite)
    {
        bgAtlas = atlas;
        bgSprite = sprite;
        return this;
    }
    
    public FilterStyleBuilder WithBaseColor(Color color)
    {
        baseColor = color;
        return this;
    }
    
    // public IFilterButtonStyle Build()
    // {
    //     return sType switch
    //     {
    //         StyleType.OpacityToggle => FilterButtonStyles.OpacityToggle(bgAtlas, bgSprite),
    //         StyleType.BackgroundToggle => FilterButtonStyles.BackgroundToggle(bgAtlas, bgSprite),
    //         StyleType.BlackIconWhiteBg => FilterButtonStyles.BlackIconWhiteBg(bgAtlas, bgSprite),
    //         StyleType.OnlyBgChange => FilterButtonStyles.BlackIconWhiteBg(bgAtlas, bgSprite),
    //         _ => FilterButtonStyles.TextOnly()
    //     };
    // }
}

/*

// how to use
public class ComplexFilterBar : DynamicFilterBar<SomeEnum>
{
    protected override IFilterButtonStyle CreateButtonStyle(SomeEnum? value)
    {
        return new FilterStyleBuilder(FilterStyleBuilder.StyleType.BackgroundToggle)
            .WithBackground(AtlasAddressConfig.UI_COMMON, "frame_round")
            .WithBaseColor(Color.cyan)
            .Build();
    }
}

*/