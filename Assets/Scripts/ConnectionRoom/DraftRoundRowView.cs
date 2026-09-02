using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DraftRoundRowView : MonoBehaviour
{
    [SerializeField] private TMP_InputField roundNameField;
    [Tooltip("밴 슬롯 수. 숫자 하나만 입력하면 선공/후공에 동일하게 적용되고, '선공/후공' 형식(예: 2/1)으로 입력하면 각각 다르게 지정된다.")]
    [SerializeField] private TMP_InputField banSlotsField;
    [Tooltip("픽 슬롯 수. 숫자 하나만 입력하면 선공/후공에 동일하게 적용되고, '선공/후공' 형식(예: 3/2)으로 입력하면 각각 다르게 지정된다.")]
    [SerializeField] private TMP_InputField pickSlotsField;
    [SerializeField] private TMP_Dropdown startingSideDropdown;
    [Tooltip("비워두면 startingSide 기준 단순 교대. 채우면 이게 우선한다. 예: ABBAAB")]
    [SerializeField] private TMP_InputField banOrderPatternField;
    [SerializeField] private TMP_InputField pickOrderPatternField;
    [SerializeField] private Button removeButton;

    // 타이머(preDraftLoadBufferField/turnTimeLimitField/postDraftDisplayField)는 라운드별 값이 아니라
    // 세션 전체 공통값이라, 라운드 행마다 반복해서 두면 화면에 같은 필드가 2번, 3번씩 보이는 문제가 있었다.
    // 그래서 이 행에서는 제거했고, DraftLobbyController에 한 번만 두고 편집하도록 옮겼다.

    public event Action OnEdited;
    
    public event Action OnRemoveRequested;

    private bool suppressEvents;

    private void Awake()
    {
        roundNameField.onEndEdit.AddListener(_ => RaiseEdited());
        banSlotsField.onEndEdit.AddListener(_ => RaiseEdited());
        pickSlotsField.onEndEdit.AddListener(_ => RaiseEdited());
        startingSideDropdown.onValueChanged.AddListener(_ => RaiseEdited());
        banOrderPatternField.onEndEdit.AddListener(_ => RaiseEdited());
        pickOrderPatternField.onEndEdit.AddListener(_ => RaiseEdited());
        removeButton.onClick.AddListener(() => OnRemoveRequested?.Invoke());
    }

    public void SetInteractable(bool interactable)
    {
        roundNameField.interactable = interactable;
        banSlotsField.interactable = interactable;
        pickSlotsField.interactable = interactable;
        startingSideDropdown.interactable = interactable;
        banOrderPatternField.interactable = interactable;
        pickOrderPatternField.interactable = interactable;
        removeButton.interactable = interactable;
    }
    
    public void Bind(DraftRoundConfig round)
    {
        suppressEvents = true;

        roundNameField.text = round.RoundName;
        banSlotsField.text = FormatSlotsPair(round.FirstBanSlots, round.SecondBanSlots);
        pickSlotsField.text = FormatSlotsPair(round.FirstPickSlots, round.SecondPickSlots);
        startingSideDropdown.value = round.StartingSide == DraftSide.First ? 0 : 1;
        banOrderPatternField.text = round.BanOrderPattern;
        pickOrderPatternField.text = round.PickOrderPattern;

        suppressEvents = false;
    }

    public DraftRoundConfig ReadValue()
    {
        var (firstBan, secondBan) = ParseSlotsPair(banSlotsField.text);
        var (firstPick, secondPick) = ParseSlotsPair(pickSlotsField.text);

        return new DraftRoundConfig(
            firstBan, secondBan,
            firstPick, secondPick,
            startingSideDropdown.value == 0 ? DraftSide.First : DraftSide.Second,
            roundNameField.text,
            banOrderPatternField.text?.Trim() ?? "",
            pickOrderPatternField.text?.Trim() ?? "");
    }

    private void RaiseEdited()
    {
        if (suppressEvents) return;
        WarnIfPatternMismatched();
        OnEdited?.Invoke();
    }
    
    private void WarnIfPatternMismatched()
    {
        var (firstBan, secondBan) = ParseSlotsPair(banSlotsField.text);
        var (firstPick, secondPick) = ParseSlotsPair(pickSlotsField.text);

        WarnIfMismatched(banOrderPatternField.text, firstBan, secondBan, "밴");
        WarnIfMismatched(pickOrderPatternField.text, firstPick, secondPick, "픽");
    }

    private void WarnIfMismatched(string pattern, int firstSlots, int secondSlots, string label)
    {
        if (string.IsNullOrWhiteSpace(pattern)) return;

        int firstCount = 0, secondCount = 0;
        foreach (var raw in pattern.Trim())
        {
            char c = char.ToUpperInvariant(raw);
            if (c == 'A') { firstCount++; continue; }
            if (c == 'B') { secondCount++; continue; }

            Debug.LogWarning($"[{nameof(DraftRoundRowView)}] {label} 패턴에 A/B가 아닌 문자('{raw}')가 있습니다.");
            return;
        }

        if (firstCount != firstSlots || secondCount != secondSlots)
        {
            Debug.LogWarning(
                $"[{nameof(DraftRoundRowView)}] {label} 패턴 '{pattern}'의 구성(A={firstCount}/B={secondCount})이 " +
                $"슬롯 수(선공={firstSlots}/후공={secondSlots})와 다릅니다. 이대로 드래프트를 시작하면 서버에서 예외가 발생합니다.");
        }
    }

    private static (int first, int second) ParseSlotsPair(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return (0, 0);

        var parts = text.Split('/');
        if (parts.Length >= 2)
            return (ParseNonNegativeInt(parts[0]), ParseNonNegativeInt(parts[1]));

        int value = ParseNonNegativeInt(parts[0]);
        return (value, value);
    }

    private static string FormatSlotsPair(int first, int second) =>
        first == second ? first.ToString() : $"{first}/{second}";

    private static int ParseNonNegativeInt(string text) =>
        int.TryParse(text, out var value) ? Mathf.Max(0, value) : 0;
}
