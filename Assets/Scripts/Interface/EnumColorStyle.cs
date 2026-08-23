using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnumColorStyle : IFilterButtonStyle
{
    private Dictionary<Enum, Color> _colorMap;

    public EnumColorStyle(Dictionary<Enum, Color> colorMap)
    {
        _colorMap = colorMap;
    }

    public Sprite GetIcon(Enum filterValue) => null;

    public Color GetColor(Enum filterValue, bool isSelected)
    {
        // enum 고유 색상 반환 (선택 여부 무시)
        return _colorMap.TryGetValue(filterValue, out var color) 
            ? color 
            : Color.white;
    }
    public void Initialize(UniversalFilterButton button)
    {
        
    }
    public void ApplyVisuals(UniversalFilterButton button, bool isSelected)
    {
        
    }
}