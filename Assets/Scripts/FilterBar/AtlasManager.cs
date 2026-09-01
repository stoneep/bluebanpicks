using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class AtlasManager : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private RectTransform content;
    [SerializeField] private IconFilterButton buttonPrefab;
    [SerializeField] private string atlasKey;
    [SerializeField] private string iconPrefix;
    [SerializeField] private bool includeAllButton = true;
    [SerializeField] private string allIconName = "icon_all";
    
    private readonly List<IconFilterButton> buttons = new();
    private object currentSelection = null;
    private Action<object> onSelectionChanged;
    
    public void Initialize<T>(Action<object> onSelected) where T : struct, Enum
    {
        this.onSelectionChanged = onSelected;
        
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
        foreach (Transform child in content) Destroy(child.gameObject);
        buttons.Clear();
        
        if (includeAllButton)
        {
            CreateButton(null, allIconName);
        }
        
        foreach (T value in Enum.GetValues(typeof(T)))
        {
            
            string spriteName = $"{iconPrefix}{value.ToString().ToLowerInvariant()}";
            CreateButton(value, spriteName);
        }
    }

    private void CreateButton(object data, string spriteName)
    {
        var btn = Instantiate(buttonPrefab, content);
        
        Sprite sp = UIIconAtlasService.Instance.GetSprite(atlasKey, spriteName);
        
        bool isSelected = IsSelected(data);
        btn.Setup(data, sp, OnButtonClicked, isSelected);
        
        buttons.Add(btn);
    }

    private bool IsSelected(object data)
    {
        if (currentSelection == null) return data == null;
        return currentSelection.Equals(data);
    }

    private void OnButtonClicked(IconFilterButton clickedBtn)
    {
        object clickedData = clickedBtn.DataValue;
        
        if (IsSelected(clickedData)) return;

        currentSelection = clickedData;
        
        foreach (var b in buttons)
        {
            b.SetSelected(IsSelected(b.DataValue));
        }
        
        onSelectionChanged?.Invoke(currentSelection);
    }
}