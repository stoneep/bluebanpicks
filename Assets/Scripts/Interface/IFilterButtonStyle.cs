using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IFilterButtonStyle
{
    void Initialize(UniversalFilterButton button);
    void ApplyVisuals(UniversalFilterButton button, bool isSelected);
}

public interface IColorableStyle
{
    void SetColor(Color color);
}