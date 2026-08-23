using System;
using System.Collections.Generic;
using System.Linq;

public sealed class CharacterFilterState
{
    private readonly List<IFilterRule<CharacterViewData>> rules = new();
    private TextSearchFilterRule<CharacterViewData> searchRule;
    
    public CharacterSortType SortType = CharacterSortType.ByAffiliation;
    public SortOrder Order = SortOrder.Descending;
    
    public event Action OnStateChanged;
    public void AddRule(IFilterRule<CharacterViewData> rule)
    {
        rules.Add(rule);
        if (rule is TextSearchFilterRule<CharacterViewData> textRule) // ★ 추가
            searchRule = textRule;
    }

    public bool Pass(CharacterViewData c) => rules.All(r => r.IsSatisfiedBy(c));

    public void NotifyChanged() => OnStateChanged?.Invoke();
    
    public Comparison<CharacterViewData> GetComparison()
    {
        bool useRelevance = searchRule != null && !string.IsNullOrEmpty(searchRule.Term); // ★ 추가

        return (a, b) =>
        {
            if (useRelevance) // ★ 추가
            {
                int relA = searchRule.GetRelevance(a);
                int relB = searchRule.GetRelevance(b);
                if (relA != relB) return relA.CompareTo(relB); // 관련도 높은 순, Order 영향 안 받음
            }

            int result = CompareBySelectedType(a, b);
            
            if (result == 0 && SortType != CharacterSortType.ByLevel)
                result = b.Level.CompareTo(a.Level);

            if (result == 0)
                result = string.Compare(a.Id, b.Id, StringComparison.Ordinal);

            return (Order == SortOrder.Ascending) ? result : -result;
        };
    }

    private int CompareBySelectedType(CharacterViewData a, CharacterViewData b)
    {
        return SortType switch
        {
            CharacterSortType.ByRarity => a.Rarity.CompareTo(b.Rarity),
            CharacterSortType.ByLevel => a.Level.CompareTo(b.Level),
            CharacterSortType.ByName => string.Compare(a.DisplayName, b.DisplayName, StringComparison.CurrentCulture),
            CharacterSortType.ByAffiliation => a.Affiliation.CompareTo(b.Affiliation),
            CharacterSortType.ByTacticalRole => a.TacticalRole.CompareTo(b.TacticalRole),
            _ => 0
        };
    }
}