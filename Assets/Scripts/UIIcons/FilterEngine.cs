using System;
using System.Collections.Generic;

public sealed class FilterEngine<TData>
{
    private readonly List<TData> all = new();
    private readonly List<TData> view = new();
    private readonly IFilteredListView<TData> listView;
    private readonly Func<TData, bool> pass;
    private Comparison<TData> sortComparison;
    
    public FilterEngine(IFilteredListView<TData> listView, Func<TData, bool> pass)
    {
        this.listView = listView;
        this.pass = pass;
    }
    
    public void SetSort(Comparison<TData> comparison) => sortComparison = comparison;
    
    public void SetAll(List<TData> items)
    {
        all.Clear();
        if (items != null) all.AddRange(items);
    }

    public void Rebuild(bool jumpToTop)
    {
        view.Clear();
        for (int i = 0; i < all.Count; i++)
        {
            var d = all[i];
            if (pass == null || pass(d)) view.Add(d);
        }
        
        if (sortComparison != null)
            view.Sort(sortComparison);

        listView.SetData(view);
        listView.Refresh(jumpToTop);
    }
}