using UnityEngine;

/// <summary>
/// 모든 아틀라스 주소를 중앙에서 관리하는 설정 클래스
/// ItemDataEnum.cs의 Description을 기반으로 체계적으로 정리됨
/// 
/// 핵심 원칙:
/// 1. SubItemCategory 우선 조회 (세부 분류)
/// 2. MainItemCategory는 폴백 (대분류)
/// 3. ITEM_COMMON은 범용 폴백 (Report, 화폐, 레어리티 배경 등)
/// 
/// 사용 예:
/// string atlasKey = AtlasAddressConfig.GetItemAtlas(itemData);
/// Sprite sprite = UIIconAtlasService.Instance.GetSprite(atlasKey, spriteName);
/// </summary>
public static class AtlasAddressConfig
{
    // ===== 캐릭터 관련 =====
    public const string AFFILIATION = "atlas/icon_affiliation";     // 학원 로고
    
    // ===== 전투 타입 =====
    public const string COMBAT_TYPE = "atlas/icon_combatType";      // 공격/방어 타입 (공통 아이콘 + 색상)
    public const string COMBAT = "atlas/icon_combat";               // Role, TacticalRole
    
    // ===== 아이템 아틀라스 =====
    public const string ITEM_COMMON = "atlas/icon_common";          
    // 공통: 화폐(Currency), 레어리티 배경(frame_rarity_*), 
    //       Report, 탭 아이콘(tab_*), EventCurrency
    
  
    
    /// <summary>
    /// 사용 가능한 모든 아틀라스 키 목록 (전체 프리로드용)
    /// </summary>
    public static string[] GetAllAtlasKeys()
    {
        return new[]
        {
            // 캐릭터
            AFFILIATION,
            COMBAT,
            COMBAT_TYPE,
            
            // 아이템
            ITEM_COMMON
        };
    }
    
    /// <summary>
    /// 필수 아틀라스만 반환 (빠른 시작용)
    /// </summary>
    public static string[] GetCoreAtlasKeys()
    {
        return new[]
        {
            AFFILIATION,
            COMBAT,
            COMBAT_TYPE,
            ITEM_COMMON
        };
    }
}
