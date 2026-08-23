using UnityEngine;

// 공격 타입용 팔레트 생성 기능
[CreateAssetMenu(menuName = "Game/UI/Color Profile - AttackType")]
public class AttackTypeColorProfile : EnumColorProfile<AttackType> { }

// 방어 타입용 팔레트 생성 기능
[CreateAssetMenu(menuName = "Game/UI/Color Profile - DefenseType")]
public class DefenseTypeColorProfile : EnumColorProfile<DefenseType> { }

// 전술 역할용 팔레트 생성 기능
[CreateAssetMenu(menuName = "Game/UI/Color Profile - TacticalRole")]
public class TacticalRoleColorProfile : EnumColorProfile<TacticalRole> { }