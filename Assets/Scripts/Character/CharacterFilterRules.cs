
// 필터 규칙 관리
public class CharacterFilterRules
{
    // affiliation 필터 전용 클래스
    public readonly AffiliationFilterRule Affiliation = new();

    // 그외 GenericFilterRule 람다식
    public readonly GenericFilterRule<CharacterViewData, TacticalRole> TacticalRole 
        = new(item => item.TacticalRole);

    public readonly GenericFilterRule<CharacterViewData, Role> Role 
        = new(item => item.Role);
    public readonly GenericFilterRule<CharacterViewData, AttackType> Attack 
        = new(item => item.AttackType);
    public readonly GenericFilterRule<CharacterViewData, DefenseType> Defense 
        = new(item => item.DefenseType);
    public readonly GenericFilterRule<CharacterViewData, Position> Position 
        = new(item => item.Position);

    // 이름 검색 (영문/한글 표기 + Id 대상, 초성 검색 지원)
    public readonly TextSearchFilterRule<CharacterViewData> Search
        = new(item => new[] { item.DisplayNameEn, item.DisplayNameKr, item.Id });
    
    // Context(데이터 박스)를 받아서 모든 규칙에 적용
    public void Apply(CharacterFilterContext context)
    {
        Affiliation.Set(context.Affiliation);
        Role.Target = context.Role;
        Attack.Target = context.AttackType;
        Defense.Target = context.DefenseType;
        Position.Target = context.Position;
        TacticalRole.Target = context.TacticalRole;
        Search.Set(context.SearchText);
    }

    // 현재 규칙들의 상태를 Context에 채워 넣기 (스냅샷 찍기)
    public void WriteTo(ref CharacterFilterContext context)
    {
        context.Affiliation = Affiliation.Current;
        context.TacticalRole = TacticalRole.Target;
        context.AttackType = Attack.Target;
        context.DefenseType = Defense.Target;
        context.SearchText = Search.Term;
    }

    // 모든 규칙을 필터 상태(State)에 등록하는 헬퍼 함수
    public void RegisterTo(CharacterFilterState state)
    {
        state.AddRule(Affiliation);
        state.AddRule(TacticalRole);
        state.AddRule(Role);
        state.AddRule(Attack);
        state.AddRule(Defense);
        state.AddRule(Position);
        state.AddRule(Search);
    }
}