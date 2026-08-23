using System.Collections.Generic;

public sealed class FilterSet<T>
{
    private readonly HashSet<T> set = new();

    // 비어있으면 ALL
    public bool IsAll => set.Count == 0;

    public void Clear() => set.Clear();

    public void SetSingle(T value)
    {
        set.Clear();
        set.Add(value);
    }

    public void Toggle(T value)
    {
        if (!set.Add(value))
            set.Remove(value);
    }

    public bool Contains(T value) => set.Contains(value);

    public IReadOnlyCollection<T> Items => set;
}