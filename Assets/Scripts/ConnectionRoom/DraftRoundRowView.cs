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
    [SerializeField] private Button removeButton;

    /// <summary>필드 값이 바뀌었을 때 (호스트만 구독해서 서버에 반영하면 됨).</summary>
    public event Action OnEdited;

    /// <summary>삭제 버튼을 눌렀을 때.</summary>
    public event Action OnRemoveRequested;

    private bool suppressEvents; // Bind()로 값을 채우는 동안 OnEdited가 잘못 발화하지 않도록

    private void Awake()
    {
        roundNameField.onEndEdit.AddListener(_ => RaiseEdited());
        firstBanField.onEndEdit.AddListener(_ => RaiseEdited());
        secondBanField.onEndEdit.AddListener(_ => RaiseEdited());
        firstPickField.onEndEdit.AddListener(_ => RaiseEdited());
        secondPickField.onEndEdit.AddListener(_ => RaiseEdited());
        startingSideDropdown.onValueChanged.AddListener(_ => RaiseEdited());
        removeButton.onClick.AddListener(() => OnRemoveRequested?.Invoke());
    }

    public void SetInteractable(bool interactable)
    {
        roundNameField.interactable = interactable;
        firstBanField.interactable = interactable;
        secondBanField.interactable = interactable;
        firstPickField.interactable = interactable;
        secondPickField.interactable = interactable;
        startingSideDropdown.interactable = interactable;
        removeButton.interactable = interactable;
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

        suppressEvents = false;
    }

    /// <summary>지금 UI에 입력된 값을 DraftRoundConfig로 읽어온다 (서버에 보낼 때 사용).</summary>
    public DraftRoundConfig ReadValue() => new DraftRoundConfig(
        ParseNonNegativeInt(firstBanField.text), ParseNonNegativeInt(secondBanField.text),
        ParseNonNegativeInt(firstPickField.text), ParseNonNegativeInt(secondPickField.text),
        startingSideDropdown.value == 0 ? DraftSide.First : DraftSide.Second,
        roundNameField.text);

    private void RaiseEdited()
    {
        if (suppressEvents) return;
        OnEdited?.Invoke();
    }

    private static int ParseNonNegativeInt(string text) =>
        int.TryParse(text, out var value) ? Mathf.Max(0, value) : 0;
}
