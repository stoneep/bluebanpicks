using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 필터 버튼의 시각적 표현 전략
/// </summary>
public interface IFilterButtonStyle
{
    void Initialize(UniversalFilterButton button);
    void ApplyVisuals(UniversalFilterButton button, bool isSelected);
}

/// <summary>
/// 색상 설정이 가능한 스타일
/// </summary>
public interface IColorableStyle
{
    void SetColor(Color color);
}