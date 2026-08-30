using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 대기실 라운드 목록의 행(row) 하나. 슬롯 수/시작 진영을 표시하고,
/// 호스트에게는 입력 가능한 필드로, 게스트에게는 SetInteractable(false)로 읽기 전용으로 보여준다.
/// 이 컴포넌트는 서버에 아무것도 직접 쓰지 않는다 - 값이 바뀌면 OnEdited만 발행하고,
/// 실제로 DraftSessionServer.HostSetFormat을 호출할지는 DraftLobbyController가 결정한다.
/// </summary>
public class DraftRoundRowView : MonoBehaviour
{
    [SerializeField] private TMP_InputField roundNameField;
    [SerializeField] private TMP_InputField firstBanField;
    [SerializeField] private TMP_InputField secondBanField;
    [SerializeField] private TMP_InputField firstPickField;
    [SerializeField] private TMP_InputField secondPickField;
    [SerializeField] private TMP_Dropdown startingSideDropdown; // Option 0 = 선공, 1 = 후공
    [Tooltip("비워두면 startingSide 기준 단순 교대. 채우면 이게 우선한다. 예: ABBAAB")]
    [SerializeField] private TMP_InputField banOrderPatternField;
    [SerializeField] private TMP_InputField pickOrderPatternField;
    [SerializeField] private Button removeButton;

    [Header("타이머 (라운드별 값이 아니라 세션 전체 공통값 - 어느 행에서 고쳐도 전체에 적용됨)")]
    [Tooltip("DraftSessionServer.PreDraftLoadBufferSeconds. 밴픽씬 로드 후 실제 시작 전 대기 시간(초).")]
    [SerializeField] private TMP_InputField preDraftLoadBufferField;
    [Tooltip("DraftSessionServer.TurnTimeLimitSeconds. 밴/픽 각 턴의 제한 시간(초). 0 이하면 턴 타이머 없음.")]
    [SerializeField] private TMP_InputField turnTimeLimitField;

    /// <summary>라운드 필드(슬롯 수/패턴 등) 값이 바뀌었을 때 (호스트만 구독해서 서버에 반영하면 됨).</summary>
    public event Action OnEdited;

    /// <summary>
    /// 타이머 필드(preDraftLoadBuffer/turnTimeLimit) 값이 바뀌었을 때. 세션 전체 공통값이므로
    /// OnEdited와 분리했다 - 이 이벤트는 DraftFormatData가 아니라 DraftSessionServer.HostSetTimerSettings로 보내야 한다.
    /// </summary>
    public event Action OnTimerEdited;

    /// <summary>삭제 버튼을 눌렀을 때.</summary>
    public event Action OnRemoveRequested;

    private bool suppressEvents; // Bind()로 값을 채우는 동안 OnEdited가 잘못 발화하지 않도록
    private bool suppressTimerEvents; // BindTimers()로 값을 채우는 동안 OnTimerEdited가 잘못 발화하지 않도록

    private void Awake()
    {
        roundNameField.onEndEdit.AddListener(_ => RaiseEdited());
        firstBanField.onEndEdit.AddListener(_ => RaiseEdited());
        secondBanField.onEndEdit.AddListener(_ => RaiseEdited());
        firstPickField.onEndEdit.AddListener(_ => RaiseEdited());
        secondPickField.onEndEdit.AddListener(_ => RaiseEdited());
        startingSideDropdown.onValueChanged.AddListener(_ => RaiseEdited());
        banOrderPatternField.onEndEdit.AddListener(_ => RaiseEdited());
        pickOrderPatternField.onEndEdit.AddListener(_ => RaiseEdited());
        removeButton.onClick.AddListener(() => OnRemoveRequested?.Invoke());

        if (preDraftLoadBufferField != null) preDraftLoadBufferField.onEndEdit.AddListener(_ => RaiseTimerEdited());
        if (turnTimeLimitField != null) turnTimeLimitField.onEndEdit.AddListener(_ => RaiseTimerEdited());
    }

    public void SetInteractable(bool interactable)
    {
        roundNameField.interactable = interactable;
        firstBanField.interactable = interactable;
        secondBanField.interactable = interactable;
        firstPickField.interactable = interactable;
        secondPickField.interactable = interactable;
        startingSideDropdown.interactable = interactable;
        banOrderPatternField.interactable = interactable;
        pickOrderPatternField.interactable = interactable;
        removeButton.interactable = interactable;

        if (preDraftLoadBufferField != null) preDraftLoadBufferField.interactable = interactable;
        if (turnTimeLimitField != null) turnTimeLimitField.interactable = interactable;
    }

    /// <summary>서버 값으로 UI를 채운다. 이 중에는 OnEdited가 발화하지 않는다.</summary>
    public void Bind(DraftRoundConfig round)
    {
        suppressEvents = true;

        roundNameField.text = round.RoundName;
        firstBanField.text = round.FirstBanSlots.ToString();
        secondBanField.text = round.SecondBanSlots.ToString();
        firstPickField.text = round.FirstPickSlots.ToString();
        secondPickField.text = round.SecondPickSlots.ToString();
        startingSideDropdown.value = round.StartingSide == DraftSide.First ? 0 : 1;
        banOrderPatternField.text = round.BanOrderPattern;
        pickOrderPatternField.text = round.PickOrderPattern;

        suppressEvents = false;
    }

    /// <summary>
    /// 세션 공통 타이머 값으로 UI를 채운다. 이 중에는 OnTimerEdited가 발화하지 않는다.
    /// 라운드별 값이 아니라 세션 전체 값이므로, 여러 행을 동시에 이 값으로 채워도 문제없다
    /// (DraftLobbyController가 모든 행에 동일한 값을 넣어준다).
    /// </summary>
    public void BindTimers(float preDraftLoadBufferSeconds, float turnTimeLimitSeconds)
    {
        suppressTimerEvents = true;

        if (preDraftLoadBufferField != null) preDraftLoadBufferField.text = FormatSeconds(preDraftLoadBufferSeconds);
        if (turnTimeLimitField != null) turnTimeLimitField.text = FormatSeconds(turnTimeLimitSeconds);

        suppressTimerEvents = false;
    }

    /// <summary>지금 UI에 입력된 타이머 값을 읽어온다 (DraftSessionServer.HostSetTimerSettings에 보낼 때 사용).</summary>
    public (float preDraftLoadBufferSeconds, float turnTimeLimitSeconds) ReadTimerValues() => (
        ParseNonNegativeFloat(preDraftLoadBufferField != null ? preDraftLoadBufferField.text : null),
        ParseNonNegativeFloat(turnTimeLimitField != null ? turnTimeLimitField.text : null));

    /// <summary>지금 UI에 입력된 값을 DraftRoundConfig로 읽어온다 (서버에 보낼 때 사용).</summary>
    public DraftRoundConfig ReadValue() => new DraftRoundConfig(
        ParseNonNegativeInt(firstBanField.text), ParseNonNegativeInt(secondBanField.text),
        ParseNonNegativeInt(firstPickField.text), ParseNonNegativeInt(secondPickField.text),
        startingSideDropdown.value == 0 ? DraftSide.First : DraftSide.Second,
        roundNameField.text,
        banOrderPatternField.text?.Trim() ?? "",
        pickOrderPatternField.text?.Trim() ?? "");

    private void RaiseEdited()
    {
        if (suppressEvents) return;
        WarnIfPatternMismatched();
        OnEdited?.Invoke();
    }

    /// <summary>
    /// 패턴의 A/B 개수가 슬롯 수와 안 맞으면, 드래프트 시작 시 서버(SequenceTurnOrderRule.Validate)에서
    /// 예외로 터지기 전에 편집 시점에 미리 콘솔로 경고한다. (인스펙터에 별도 경고 UI가 없다면
    /// 이 로그가 유일한 신호이므로, 실사용 시엔 이 자리에 경고 아이콘/텍스트를 붙이는 걸 권장)
    /// </summary>
    private void WarnIfPatternMismatched()
    {
        WarnIfMismatched(banOrderPatternField.text, ParseNonNegativeInt(firstBanField.text), ParseNonNegativeInt(secondBanField.text), "밴");
        WarnIfMismatched(pickOrderPatternField.text, ParseNonNegativeInt(firstPickField.text), ParseNonNegativeInt(secondPickField.text), "픽");
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

    private void RaiseTimerEdited()
    {
        if (suppressTimerEvents) return;
        OnTimerEdited?.Invoke();
    }

    private static int ParseNonNegativeInt(string text) =>
        int.TryParse(text, out var value) ? Mathf.Max(0, value) : 0;

    private static float ParseNonNegativeFloat(string text) =>
        float.TryParse(text, out var value) ? Mathf.Max(0f, value) : 0f;

    private static string FormatSeconds(float seconds) => seconds.ToString("0.##");
}
