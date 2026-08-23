using System;
using TMPro;
using Common.Pooling;
using UnityEngine.UI;
using UnityEngine;

public sealed partial class CharacterSlotView : MonoBehaviour, IUIReusable
{
    [Header("Refs - Main")]
    [SerializeField] private Button button;
    [SerializeField] private Image portraitIcon; // 변수명 명확화 (icon -> portraitIcon)
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private Image[] rarityStars;
    [SerializeField] private GameObject lockOverlay;

    [Header("Refs - Icons")]
    [SerializeField] private Image affiliationIcon;
    [SerializeField] private Image positionIcon;
    [SerializeField] private Image tacticalRoleIcon;
    [SerializeField] private Image roleTypeIcon;

    [Header("Refs - Combat Types")]
    [SerializeField] private Image attackTypeIcon;
    [SerializeField] private Image attackTypeBg; // 배경
    [SerializeField] private Image defenseTypeIcon;
    [SerializeField] private Image defenseTypeBg; // 배경

    [Header("Refs - Texts")]
    [SerializeField] private TMP_Text roleText;
    [SerializeField] private TMP_Text positionText;

    // ⭐ 각 아이콘별 로더 (복잡한 토큰 로직은 얘가 다 처리함)
    private readonly AtlasImageBinder _affiliationBinder = new();
    private readonly AtlasImageBinder _roleBinder = new();
    private readonly AtlasImageBinder _tacticalBinder = new();
    private readonly AtlasImageBinder _attackBinder = new();
    private readonly AtlasImageBinder _defenseBinder = new();

    // 초상화 로딩용 토큰 (초상화는 Atlas가 아니라 개별 로드라 따로 관리)
    private int _portraitToken;
    private Action<int> _onClickIndex;
    private int _boundIndex;

    public void OnRent() { } // 풀링 초기화 (필요 시)

    public void OnReturn()
    {
        // 1. 모든 로더 취소 (잔상 제거)
        _affiliationBinder.Release(affiliationIcon);
        _roleBinder.Release(roleTypeIcon);
        _tacticalBinder.Release(tacticalRoleIcon);
        _attackBinder.Release(attackTypeIcon);
        _defenseBinder.Release(defenseTypeIcon);
        
        // 2. 배경 및 텍스트 초기화 덜됐음
        if (attackTypeBg) attackTypeBg.gameObject.SetActive(false);
        if (defenseTypeBg) defenseTypeBg.gameObject.SetActive(false);
        if (portraitIcon) { portraitIcon.sprite = null; portraitIcon.enabled = false; }
        
        // 3. 버튼 연결 해제
        if (button) button.onClick.RemoveAllListeners();
        
        gameObject.SetActive(false);
    }

    // 메인 바인딩 함수
    public void Bind(int dataIndex, in CharacterViewData data, Action<int> onClick, CharacterArtProvider artProvider)
    {
        _boundIndex = dataIndex;
        _onClickIndex = onClick;

        // 1. 기본 텍스트 및 상태 설정
        SetNameAndStats(data);
        UpdateRarityStars(data.Rarity);
        
        // 2. 아이콘 로딩 (로더에게 위임)
        LoadIcons(data);

        // 3. 초상화 로딩
        LoadPortrait(data.Id, artProvider);

        // 4. 클릭 이벤트 연결
        if (button)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => _onClickIndex?.Invoke(_boundIndex));
        }
    }

    private void SetNameAndStats(in CharacterViewData data)
    {
        if (nameText) nameText.text = data.DisplayName;
        if (levelText) levelText.text = $"Lv {data.Level}";
        if (roleText) roleText.text = data.Role.ToString();
        if (positionText) positionText.text = data.Position.ToString();
        if (lockOverlay) lockOverlay.SetActive(data.IsLocked);
    }

    private void LoadIcons(in CharacterViewData data)
    {
        // 학원 로고
        _affiliationBinder.Bind(
            affiliationIcon, 
            UIExtensions.ATLAS_AFFILIATION, // 상수도 확장클래스에서 가져옴
            data.Affiliation.ToSpriteName()
        );

        // 역할 아이콘
        _roleBinder.Bind(
            roleTypeIcon, 
            UIExtensions.ATLAS_COMMON, 
            data.Role.ToSpriteName(), 
            (img) => img.color = Color.black // 특정 스타일링은 남겨둘 수 있음
        );

        // 전술 역할
        _tacticalBinder.Bind(
            tacticalRoleIcon, 
            UIExtensions.ATLAS_COMMON, 
            data.TacticalRole.ToSpriteName()
        );

        // 로컬 변수 캡처 (람다)
        var myAttack = data.AttackType;
        var myDefense = data.DefenseType;
        
        // 공격 타입
        _attackBinder.Bind(
            attackTypeIcon, 
            UIExtensions.ATLAS_COMMON, 
            myAttack.ToCommonSpriteName(), 
            (img) => 
            {
                img.color = Color.white;
                // 확장 메서드 활용
                SetBackground(attackTypeBg, myAttack.GetThemeColor()); 
            }
        );

        // 방어 타입
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

    // 배경색
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

    // -----------------------
    // * - Atlas icon binding - *
    // -----------------------
    
    public void SetVisible(bool visible) => gameObject.SetActive(visible);

    private void OnDisable()
    {
        // 풀링/가상화 잔상 방지
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
    }
}
