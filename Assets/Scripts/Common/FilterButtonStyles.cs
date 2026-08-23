using UnityEngine;

/// <summary>
/// 설정 가능한 범용 필터 버튼 스타일
/// 대부분의 케이스를 커버하는 통합 스타일
/// </summary>
public class ConfigurableFilterStyle : MonoBehaviour
{
    // // 설정 옵션
    // public enum VisualMode
    // {
    //     BackgroundToggle,    // 배경 색상 변경 (IconColorStyle)
    //     OpacityToggle,       // 투명도 변경 (GrayToggleStyle)
    //     TextOnly,            // 텍스트만 (TextOnlyStyle)
    //     BlackIconWhiteBg,     // 검은 아이콘 + 흰 배경
    //     OnlyBgChange
    // }
    //
    // private readonly VisualMode mode;
    // private Color assignedColor = Color.white;
    //
    // // 색상 설정
    // private readonly Color normalBgColor;
    // private readonly Color selectedBgColor;
    // private readonly Color iconColorNormal;
    // private readonly Color iconColorSelected;
    // private readonly Color textColorNormal;
    // private readonly Color textColorSelected;
    //
    // private readonly float normalOpacity;
    // private readonly float selectedOpacity;
    //
    // /// <summary>
    // /// 프리셋 생성자
    // /// </summary>
    // public ConfigurableFilterStyle(VisualMode mode)
    // {
    //     this.mode = mode;
    //     
    //     switch (mode)
    //     {
    //         case VisualMode.BackgroundToggle:
    //             normalBgColor = new Color(0.9f, 0.9f, 0.9f);
    //             selectedBgColor = Color.white; // assignedColor로 덮어씀
    //             iconColorNormal = Color.white;
    //             iconColorSelected = Color.white;
    //             normalOpacity = 1f;
    //             selectedOpacity = 1f;
    //             break;
    //             
    //         case VisualMode.OpacityToggle:
    //             normalBgColor = Color.white; // assignedColor로 덮어씀
    //             selectedBgColor = Color.white;
    //             iconColorNormal = Color.white;
    //             iconColorSelected = Color.white;
    //             normalOpacity = 0.4f;
    //             selectedOpacity = 1f;
    //             textColorNormal = Color.white;
    //             textColorSelected = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    //             break;
    //             
    //         case VisualMode.BlackIconWhiteBg:
    //             normalBgColor = Color.white;
    //             selectedBgColor = new Color(0.5f, 0.5f, 0.5f);
    //             iconColorNormal = Color.black;
    //             iconColorSelected = Color.black;
    //             normalOpacity = 1f;
    //             selectedOpacity = 1f;
    //             break;
    //             
    //         case VisualMode.OnlyBgChange:
    //             iconColorNormal = Color.white;
    //             iconColorSelected = Color.black;
    //             break;
    //         
    //         case VisualMode.TextOnly:
    //             textColorNormal = new Color(0.7f, 0.7f, 0.7f);
    //             textColorSelected = new Color(0.2f, 0.8f, 1.0f);
    //             break;
    //     }
    // }
    //
    // /// <summary>
    // /// 완전 커스텀 생성자 (필요한 경우에만 사용)
    // /// </summary>
    // public ConfigurableFilterStyle(
    //     Color? normalBg = null,
    //     Color? selectedBg = null,
    //     Color? iconNormal = null,
    //     Color? iconSelected = null,
    //     float normalOpacity = 1f,
    //     float selectedOpacity = 1f)
    // {
    //     this.mode = VisualMode.BackgroundToggle; // 기본값
    //     this.normalBgColor = normalBg ?? Color.white;
    //     this.selectedBgColor = selectedBg ?? new Color(0.5f, 0.5f, 0.5f);
    //     this.iconColorNormal = iconNormal ?? Color.white;
    //     this.iconColorSelected = iconSelected ?? Color.white;
    //     this.normalOpacity = normalOpacity;
    //     this.selectedOpacity = selectedOpacity;
    // }
    //
    // public void SetColor(Color color)
    // {
    //     assignedColor = color;
    // }
    //
    // public void Initialize(UniversalFilterButton button)
    // {
    //     switch (mode)
    //     {
    //         case VisualMode.TextOnly:
    //             if (button.BackgroundImage)
    //                 button.BackgroundImage.gameObject.SetActive(false);
    //             if (button.IconImage)
    //                 button.IconImage.enabled = false;
    //             break;
    //             
    //         default:
    //             if (button.BackgroundImage)
    //             {
    //                 button.BackgroundImage.gameObject.SetActive(true);
    //                 button.BackgroundImage.color = mode == VisualMode.OpacityToggle 
    //                     ? assignedColor 
    //                     : normalBgColor;
    //             }
    //             break;
    //     }
    // }
    //
    // public void ApplyVisuals(UniversalFilterButton button, bool isSelected)
    // {
    //     switch (mode)
    //     {
    //         case VisualMode.BackgroundToggle:
    //             ApplyBackgroundToggle(button, isSelected);
    //             break;
    //             
    //         case VisualMode.OpacityToggle:
    //             ApplyOpacityToggle(button, isSelected);
    //             break;
    //             
    //         case VisualMode.BlackIconWhiteBg:
    //             ApplyBlackIconWhiteBg(button, isSelected);
    //             break;
    //             
    //         case VisualMode.TextOnly:
    //             ApplyTextOnly(button, isSelected);
    //             break;
    //     }
    // }
    //
    // private void ApplyBackgroundToggle(UniversalFilterButton button, bool isSelected)
    // {
    //     if (button.BackgroundImage)
    //     {
    //         button.BackgroundImage.color = isSelected ? assignedColor : normalBgColor;
    //     }
    //     
    //     if (button.IconImage && button.IconImage.enabled)
    //     {
    //         button.IconImage.color = iconColorNormal;
    //     }
    // }
    //
    // private void ApplyOpacityToggle(UniversalFilterButton button, bool isSelected)
    // {
    //     if (button.BackgroundImage)
    //     {
    //         button.BackgroundImage.color = assignedColor;
    //     }
    //     
    //     if (button.IconImage && button.IconImage.enabled)
    //     {
    //         float alpha = isSelected ? selectedOpacity : normalOpacity;
    //         button.IconImage.color = new Color(1f, 1f, 1f, alpha);
    //     }
    //     
    //     if (button.LabelText && button.LabelText.gameObject.activeSelf)
    //     {
    //         button.LabelText.color = isSelected ? textColorSelected : textColorNormal;
    //     }
    // }
    //
    // private void ApplyBlackIconWhiteBg(UniversalFilterButton button, bool isSelected)
    // {
    //     if (button.BackgroundImage)
    //     {
    //         button.BackgroundImage.color = isSelected ? selectedBgColor : normalBgColor;
    //     }
    //     
    //     if (button.IconImage && button.IconImage.enabled)
    //     {
    //         button.IconImage.color = iconColorNormal;
    //     }
    //     
    //     if (button.LabelText && button.LabelText.gameObject.activeSelf)
    //     {
    //         button.LabelText.color = iconColorNormal;
    //     }
    // }
    //
    // private void ApplyTextOnly(UniversalFilterButton button, bool isSelected)
    // {
    //     if (button.LabelText)
    //     {
    //         button.LabelText.color = isSelected ? textColorSelected : textColorNormal;
    //     }
    // }
}