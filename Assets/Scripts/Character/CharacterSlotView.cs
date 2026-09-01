using System;
using TMPro;
using Common.Pooling;
using UnityEngine.UI;
using UnityEngine;

public sealed partial class CharacterSlotView : MonoBehaviour, IUIReusable
{
    [Header("Refs - Main")]
    [SerializeField] private Button button;
    [SerializeField] private Image portraitIcon; 
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private Image[] rarityStars;
    [SerializeField] private Image lockOverlay; 

    [Header("Lock Overlay Colors")]
    [Tooltip("data.IsLocked == true (미보유 캐릭터)일 때 오버레이 색")]
    [SerializeField] private Color lockedOverlayColor = new(0f, 0f, 0f, 0.6f);
    [Tooltip("드래프트에서 이미 밴/픽되어 선택 불가할 때 오버레이 색")]
    [SerializeField] private Color draftUnavailableOverlayColor = new(0.5f, 0f, 0f, 0.6f);

    [Header("Selection Highlight (밴/픽 확인 대기)")]
    [Tooltip("클릭해서 밴/픽 후보로 선택됐지만 아직 확인 버튼을 누르기 전 상태의 테두리/하이라이트. " +
             "비워두면 선택 표시 없이 클릭만 동작함.")]
    [SerializeField] private GameObject selectionHighlight;

    [Header("Refs - Icons")]
    [SerializeField] private Image affiliationIcon;
    [SerializeField] private Image positionIcon;
    [SerializeField] private Image tacticalRoleIcon;
    [SerializeField] private Image roleTypeIcon;

    [Header("Refs - Combat Types")]
    [SerializeField] private Image attackTypeIcon;
    [SerializeField] private Image attackTypeBg; 
    [SerializeField] private Image defenseTypeIcon;
    [SerializeField] private Image defenseTypeBg; 

    [Header("Refs - Texts")]
    [SerializeField] private TMP_Text roleText;
    [SerializeField] private TMP_Text positionText;

    
    private readonly AtlasImageBinder _affiliationBinder = new();
    private readonly AtlasImageBinder _roleBinder = new();
    private readonly AtlasImageBinder _tacticalBinder = new();
    private readonly AtlasImageBinder _attackBinder = new();
    private readonly AtlasImageBinder _defenseBinder = new();

    
    private int _portraitToken;
    private Action<int> _onClickIndex;
    private int _boundIndex;

    public void OnRent() { } 

    public void OnReturn()
    {
        
        _affiliationBinder.Release(affiliationIcon);
        _roleBinder.Release(roleTypeIcon);
        _tacticalBinder.Release(tacticalRoleIcon);
        _attackBinder.Release(attackTypeIcon);
        _defenseBinder.Release(defenseTypeIcon);
        
        
        if (attackTypeBg) attackTypeBg.gameObject.SetActive(false);
        if (defenseTypeBg) defenseTypeBg.gameObject.SetActive(false);
        if (portraitIcon) { portraitIcon.sprite = null; portraitIcon.enabled = false; }
        
        
        if (button) button.onClick.RemoveAllListeners();

        
        SetSelected(false);

        
    }

    
    public void Bind(int dataIndex, in CharacterViewData data, Action<int> onClick, CharacterArtProvider artProvider, bool isDraftUnavailable = false, bool isSelected = false)
    {
        _boundIndex = dataIndex;
        _onClickIndex = onClick;

        
        SetNameAndStats(data);
        UpdateRarityStars(data.Rarity);
        ApplyLockOverlay(data.IsLocked, isDraftUnavailable);
        SetSelected(isSelected);

        
        LoadIcons(data);

        
        LoadPortrait(data.Id, artProvider);

        
        if (button)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => _onClickIndex?.Invoke(_boundIndex));
        }
    }

    
    
    
    
    
    public void SetDraftUnavailable(bool isDraftUnavailable) => ApplyLockOverlay(_lastIsLocked, isDraftUnavailable);

    
    
    
    
    public void SetSelected(bool isSelected)
    {
        if (selectionHighlight) selectionHighlight.SetActive(isSelected);
    }

    private bool _lastIsLocked;

    private void ApplyLockOverlay(bool isLocked, bool isDraftUnavailable)
    {
        _lastIsLocked = isLocked;

        if (!lockOverlay) return;

        bool shouldShow = isLocked || isDraftUnavailable;
        lockOverlay.gameObject.SetActive(shouldShow);

        if (shouldShow)
        {
            
            lockOverlay.color = isDraftUnavailable ? draftUnavailableOverlayColor : lockedOverlayColor;
        }
    }

    private void SetNameAndStats(in CharacterViewData data)
    {
        if (nameText) nameText.text = data.DisplayName;
        if (levelText) levelText.text = $"Lv {data.Level}";
        if (roleText) roleText.text = data.Role.ToString();
        if (positionText) positionText.text = data.Position.ToString();
    }

    private void LoadIcons(in CharacterViewData data)
    {
        
        _affiliationBinder.Bind(
            affiliationIcon, 
            UIExtensions.ATLAS_AFFILIATION, 
            data.Affiliation.ToSpriteName()
        );

        
        _roleBinder.Bind(
            roleTypeIcon, 
            UIExtensions.ATLAS_COMMON, 
            data.Role.ToSpriteName(), 
            (img) => img.color = Color.black 
        );

        
        _tacticalBinder.Bind(
            tacticalRoleIcon, 
            UIExtensions.ATLAS_COMMON, 
            data.TacticalRole.ToSpriteName()
        );

        
        var myAttack = data.AttackType;
        var myDefense = data.DefenseType;
        
        
        _attackBinder.Bind(
            attackTypeIcon, 
            UIExtensions.ATLAS_COMMON, 
            myAttack.ToCommonSpriteName(), 
            (img) => 
            {
                img.color = Color.white;
                
                SetBackground(attackTypeBg, myAttack.GetThemeColor()); 
            }
        );

        
        _defenseBinder.Bind(
            defenseTypeIcon, 
            UIExtensions.ATLAS_COMMON, 
            myDefense.ToCommonSpriteName(), 
            (img) => 
            {
                img.color = Color.white;
                SetBackground(defenseTypeBg, myDefense.GetThemeColor());
            }
        );
    }

    
    private void SetBackground(Image bg, Color color)
    {
        if (bg == null) return;
        bg.color = color;
        bg.gameObject.SetActive(true);
    }

    private void LoadPortrait(string charId, CharacterArtProvider artProvider)
    {
        if (!portraitIcon) return;
        
        portraitIcon.enabled = false;
        int token = ++_portraitToken;

        var handle = artProvider.LoadSprite(charId, CharacterCut.Slot);
        
        if (handle.IsDone) ApplyPortrait(token, handle.Result);
        else handle.Completed += h => ApplyPortrait(token, h.Result);
    }

    private void ApplyPortrait(int token, Sprite sprite)
    {
        if (token != _portraitToken || !portraitIcon) return;
        portraitIcon.sprite = sprite;
        portraitIcon.enabled = (sprite != null);
        portraitIcon.preserveAspect = true;
    }

    private void UpdateRarityStars(int rarity)
    {
        if (rarityStars == null) return;
        for (int i = 0; i < rarityStars.Length; i++)
        {
            if (rarityStars[i]) rarityStars[i].enabled = (i < rarity);
        }
    }

    
    
    
    
    public void SetVisible(bool visible) => gameObject.SetActive(visible);

    private void OnDisable()
    {
        
        ClearAllVisuals();
    }

    private void ClearAllVisuals()
    {
        if (portraitIcon) { portraitIcon.sprite = null; portraitIcon.enabled = false; }
        if (affiliationIcon) { affiliationIcon.sprite = null; affiliationIcon.enabled = false; }
        if (roleTypeIcon) { roleTypeIcon.sprite = null; roleTypeIcon.enabled = false; }
        if (attackTypeIcon) { attackTypeIcon.sprite = null; attackTypeIcon.enabled = false; }
        if (defenseTypeIcon) { defenseTypeIcon.sprite = null; defenseTypeIcon.enabled = false; }
        if (attackTypeBg) attackTypeBg.gameObject.SetActive(false);
        if (defenseTypeBg) defenseTypeBg.gameObject.SetActive(false);
        SetSelected(false);
    }
}
