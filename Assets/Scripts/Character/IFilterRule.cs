
using System;
using System.Collections.Generic;

public interface IFilterRule<T>
{
    bool IsSatisfiedBy(T item);
}

public class AffiliationFilterRule : IFilterRule<CharacterViewData>
{
    private readonly FilterSet<Affiliation> filterSet = new();
    public Affiliation? Current { get; private set; }

    public void Set(Affiliation? value)
    {
        Current = value;
        filterSet.Clear();
        if (value.HasValue)
            filterSet.SetSingle(value.Value);
    }

    public bool IsSatisfiedBy(CharacterViewData item)
    {
        return filterSet.IsAll || filterSet.Contains(item.Affiliation);
    }
}

public class GenericFilterRule<TItem, TValue> : IFilterRule<TItem> where TValue : struct
{
    private readonly Func<TItem, TValue> _selector;
    
    public TValue? Target { get; set; }

    public GenericFilterRule(Func<TItem, TValue> selector)
    {
        _selector = selector;
    }

    public bool IsSatisfiedBy(TItem item)
    {
        return !Target.HasValue || EqualityComparer<TValue>.Default.Equals(Target.Value, _selector(item));
    }
}
