using UnityEngine;

/// <summary>
/// 밴픽 포맷(각 그룹의 슬롯 수) 정의.
/// "밴픽을 늘리거나 선택픽을 늘릴 수도 있다"는 요구사항 때문에
/// 슬롯 수를 코드에 하드코딩하지 않고 SO 에셋으로 분리.
/// 기획자가 인스펙터에서 값만 바꾸면 PickSlotBar 4개가 그 값을 그대로 반영한다.
/// </summary>
[CreateAssetMenu(menuName = "Config/DraftFormat", fileName = "DraftFormat")]
public class DraftFormatSO : ScriptableObject
{
    [Header("선공 (First)")]
    [SerializeField] private int firstPickSlots = 6;
    [SerializeField] private int firstBanSlots = 5;

    [Header("후공 (Second)")]
    [SerializeField] private int secondBanSlots = 5;
    [SerializeField] private int secondPickSlots = 6;

    public int FirstPickSlots => firstPickSlots;
    public int FirstBanSlots => firstBanSlots;
    public int SecondBanSlots => secondBanSlots;
    public int SecondPickSlots => secondPickSlots;
}