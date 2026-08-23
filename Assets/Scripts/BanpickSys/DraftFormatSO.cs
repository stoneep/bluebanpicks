using UnityEngine;

/// <summary>
/// 밴픽 포맷(각 그룹의 슬롯 수 + 턴 순서) 정의.
/// 슬롯 수뿐 아니라 밴/픽 순서도 코드 수정 없이 기획자가
/// 인스펙터에서 바꿀 수 있도록 SO에서 함께 관리한다.
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

    [Header("턴 순서 (비워두면 기본 교대: A,B,A,B...)")]
    [Tooltip("A=선공, B=후공. 예: ABABAB / ABBAAB. 슬롯 수 합계와 A/B 개수가 일치해야 함.")]
    [SerializeField] private string banOrderPattern;
    [Tooltip("A=선공, B=후공. 예: ABABAB / ABBAAB. 슬롯 수 합계와 A/B 개수가 일치해야 함.")]
    [SerializeField] private string pickOrderPattern;

    public int FirstPickSlots => firstPickSlots;
    public int FirstBanSlots => firstBanSlots;
    public int SecondBanSlots => secondBanSlots;
    public int SecondPickSlots => secondPickSlots;

    /// <summary>
    /// banOrderPattern이 비어 있으면 기본 교대 규칙, 채워져 있으면 해당 패턴의 SequenceTurnOrderRule을 만든다.
    /// </summary>
    public ITurnOrderRule BuildBanTurnOrder() =>
        string.IsNullOrWhiteSpace(banOrderPattern)
            ? new AlternatingTurnOrderRule()
            : SequenceTurnOrderRule.FromPattern(banOrderPattern);

    /// <summary>
    /// pickOrderPattern이 비어 있으면 기본 교대 규칙, 채워져 있으면 해당 패턴의 SequenceTurnOrderRule을 만든다.
    /// </summary>
    public ITurnOrderRule BuildPickTurnOrder() =>
        string.IsNullOrWhiteSpace(pickOrderPattern)
            ? new AlternatingTurnOrderRule()
            : SequenceTurnOrderRule.FromPattern(pickOrderPattern);
}