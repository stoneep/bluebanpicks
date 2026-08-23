using UnityEngine;

/// <summary>
/// 선공 선택픽(6) / 선공 밴픽(5) / 후공 밴픽(5) / 후공 선택픽(6)
/// 4개의 PickSlotBar를 DraftFormatSO 값으로 초기화하는 예시.
///
/// 슬롯 수가 바뀌어도 이 클래스는 수정할 필요 없이
/// DraftFormatSO 에셋 값만 바꾸면 됨.
/// </summary>
public class DraftBoardController : MonoBehaviour
{
    [Header("Format")]
    [SerializeField] private DraftFormatSO format;

    [Header("Bars")]
    [SerializeField] private PickSlotBar firstPickBar;   // 선공 선택픽 1x6
    [SerializeField] private PickSlotBar firstBanBar;    // 선공 밴픽   1x5
    [SerializeField] private PickSlotBar secondBanBar;   // 후공 밴픽   1x5
    [SerializeField] private PickSlotBar secondPickBar;  // 후공 선택픽 1x6

    private void Awake()
    {
        if (!format)
        {
            Debug.LogError($"[{nameof(DraftBoardController)}] DraftFormatSO가 할당되지 않았습니다.");
            return;
        }

        firstPickBar.ApplyConfig(PickSlotBarConfig.Of(format.FirstPickSlots));
        firstBanBar.ApplyConfig(PickSlotBarConfig.Of(format.FirstBanSlots));
        secondBanBar.ApplyConfig(PickSlotBarConfig.Of(format.SecondBanSlots));
        secondPickBar.ApplyConfig(PickSlotBarConfig.Of(format.SecondPickSlots));
    }

    // ==================== 예시 진행 API ====================

    public void OnFirstPick(int slotIndex, string characterId) => firstPickBar.SetCharacter(slotIndex, characterId);
    public void OnFirstBan(int slotIndex, string characterId) => firstBanBar.SetCharacter(slotIndex, characterId);
    public void OnSecondBan(int slotIndex, string characterId) => secondBanBar.SetCharacter(slotIndex, characterId);
    public void OnSecondPick(int slotIndex, string characterId) => secondPickBar.SetCharacter(slotIndex, characterId);

    public void ResetBoard()
    {
        firstPickBar.ClearAll();
        firstBanBar.ClearAll();
        secondBanBar.ClearAll();
        secondPickBar.ClearAll();
    }
}