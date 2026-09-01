using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class CharacterSearchBar : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Button clearButton;

    [Header("Debounce")]
    [SerializeField, Range(0f, 0.5f)] private float debounceSeconds = 0.15f;

    public event Action<string> OnValueChanged;

    private Coroutine debounceRoutine;
    private string lastRawSent = string.Empty;

    private void Awake()
    {
        if (inputField) inputField.onValueChanged.AddListener(HandleRawValueChanged);
        if (clearButton) clearButton.onClick.AddListener(Clear);

        UpdateClearButtonVisibility(inputField ? inputField.text : string.Empty);
    }

    private void OnDisable()
    {
        if (debounceRoutine != null)
        {
            StopCoroutine(debounceRoutine);
            debounceRoutine = null;
        }
        lastRawSent = string.Empty;
    }

    private void OnDestroy()
    {
        if (inputField) inputField.onValueChanged.RemoveListener(HandleRawValueChanged);
        if (clearButton) clearButton.onClick.RemoveListener(Clear);
    }

    private void HandleRawValueChanged(string raw)
    {
        raw ??= string.Empty;

        UpdateClearButtonVisibility(raw);
        
        if (IsComposing()) return;

        if (raw == lastRawSent) return;
        lastRawSent = raw;

        if (debounceRoutine != null) StopCoroutine(debounceRoutine);

        if (debounceSeconds <= 0f)
        {
            Emit(raw);
            return;
        }

        debounceRoutine = StartCoroutine(EmitAfterDelay(raw));
    }

    private bool IsComposing()
        => inputField != null && inputField.isFocused && !string.IsNullOrEmpty(Input.compositionString);

    private IEnumerator EmitAfterDelay(string raw)
    {
        yield return new WaitForSecondsRealtime(debounceSeconds);
        debounceRoutine = null;
        Emit(raw);
    }

    private void Emit(string raw) => OnValueChanged?.Invoke(raw ?? string.Empty);

    private void UpdateClearButtonVisibility(string raw)
    {
        if (clearButton) clearButton.gameObject.SetActive(!string.IsNullOrEmpty(raw));
    }

    public void SyncVisual(string text)
    {
        if (debounceRoutine != null)
        {
            StopCoroutine(debounceRoutine);
            debounceRoutine = null;
        }

        if (inputField) inputField.SetTextWithoutNotify(text ?? string.Empty);
        lastRawSent = text ?? string.Empty;
        UpdateClearButtonVisibility(text);
    }

    public void Clear()
    {
        if (inputField) inputField.text = string.Empty;
        else Emit(string.Empty);
    }
}