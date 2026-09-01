

public class CharacterFilterRules
{
    
    public readonly AffiliationFilterRule Affiliation = new();

    
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

    
    public readonly TextSearchFilterRule<CharacterViewData> Search
        = new(item => new[] { item.DisplayNameEn, item.DisplayNameKr});
    
    
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

    
    public void WriteTo(ref CharacterFilterContext context)
    {
        context.Affiliation = Affiliation.Current;
        context.TacticalRole = TacticalRole.Target;
        context.AttackType = Attack.Target;
        context.DefenseType = Defense.Target;
        context.SearchText = Search.Term;
    }

    
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