using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class AtlasManager : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private RectTransform content;
    [SerializeField] private IconFilterButton buttonPrefab;
    [SerializeField] private string atlasKey;    // 예: "atlas/ui_common"
    [SerializeField] private string iconPrefix;  // 예: "logo_" 또는 "icon_"
    [SerializeField] private bool includeAllButton = true;
    [SerializeField] private string allIconName = "icon_all";

    // 내부 상태 관리
    private readonly List<IconFilterButton> buttons = new();
    private object currentSelection = null; // null = All
    private Action<object> onSelectionChanged;

    /// <summary>
    /// 외부에서 이 함수를 호출하여 필터 바를 초기화합니다.
    /// T: 사용할 Enum 타입 (예: ItemType, Affiliation)
    /// </summary>
    public void Initialize<T>(Action<object> onSelected) where T : struct, Enum
    {
        this.onSelectionChanged = onSelected;

        // 아틀라스 로드 대기
        if (UIIconAtlasService.Instance.IsAtlasReady(atlasKey))
        {
            BuildButtons<T>();
        }
        else
        {
            UIIconAtlasService.Instance.LoadAtlas(atlasKey).Completed += _ => BuildButtons<T>();
        }
    }

    private void BuildButtons<T>() where T : struct, Enum
    {
        // 1. 기존 버튼 청소
        foreach (Transform child in content) Destroy(child.gameObject);
        buttons.Clear();

        // 2. [ALL] 버튼 생성
        if (includeAllButton)
        {
            CreateButton(null, allIconName);
        }

        // 3. [Enum] 버튼 생성
        foreach (T value in Enum.GetValues(typeof(T)))
        {
            // "None", "Etc" 등 제외하고 싶으면 여기서 조건문 추가
            // if (value.ToString() == "None") continue;

            // 스프라이트 이름 조합: prefix + enum이름(소문자)
            string spriteName = $"{iconPrefix}{value.ToString().ToLowerInvariant()}";
            CreateButton(value, spriteName);
        }
    }

    private void CreateButton(object data, string spriteName)
    {
        var btn = Instantiate(buttonPrefab, content);
        
        // 서비스에서 스프라이트 가져오기
        Sprite sp = UIIconAtlasService.Instance.GetSprite(atlasKey, spriteName);
        
        // 버튼 초기화
        bool isSelected = IsSelected(data);
        btn.Setup(data, sp, OnButtonClicked, isSelected);
        
        buttons.Add(btn);
    }

    private bool IsSelected(object data)
    {
        if (currentSelection == null) return data == null; // All 선택 상태
        return currentSelection.Equals(data);
    }

    private void OnButtonClicked(IconFilterButton clickedBtn)
    {
        object clickedData = clickedBtn.DataValue;

        // 같은 거 누르면 무시 or 토글? (여기선 단순 변경 로직)
        if (IsSelected(clickedData)) return;

        currentSelection = clickedData;

        // UI 갱신 (Highlight 이동)
        foreach (var b in buttons)
        {
            b.SetSelected(IsSelected(b.DataValue));
        }

        // 컨트롤러에 알림
        onSelectionChanged?.Invoke(currentSelection);
    }
}