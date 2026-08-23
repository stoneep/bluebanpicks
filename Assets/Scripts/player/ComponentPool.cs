using System.Collections.Generic;
using UnityEngine;

public sealed class ComponentPool<T> where T : Component
{
    private readonly T prefab;
    private readonly Transform parent;
    private readonly List<T> all = new();
    private int activeCount;

    public IReadOnlyList<T> All => all;

    public ComponentPool(T prefab, Transform parent)
    {
        this.prefab = prefab;
        this.parent = parent;
    }

    public void Ensure(int count)
    {
        while (all.Count < count)
        {
            var inst = Object.Instantiate(prefab, parent);
            inst.gameObject.SetActive(false);
            all.Add(inst);
        }
    }

    public T GetAt(int index)
    {
        if (index < 0 || index >= all.Count) return null;
        if (index >= activeCount) activeCount = index + 1;
        var item = all[index];
        if (!item.gameObject.activeSelf) item.gameObject.SetActive(true);
        return item;
    }

    public void ReleaseUnusedFrom(int fromIndex)
    {
        for (int i = fromIndex; i < activeCount; i++)
        {
            if (i >= 0 && i < all.Count)
                all[i].gameObject.SetActive(false);
        }
        activeCount = Mathf.Clamp(fromIndex, 0, all.Count);
    }

    public void ReleaseAll()
    {
        ReleaseUnusedFrom(0);
    }
}

public static class UIRectTransformUtil
{
    public static void SetTopLeftAnchored(RectTransform rt, Vector2 size)
    {
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.sizeDelta = size;
    }
}