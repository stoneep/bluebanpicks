using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class EnumColorProfile<T> : ScriptableObject where T : struct, Enum
{
    [Serializable]
    private struct Entry
    {
        public T Type;
        public Color Color;
    }

    [SerializeField] private List<Entry> settings;
    private Dictionary<T, Color> map;

    public Color GetColor(T type, Color defaultColor)
    {
        if (map == null)
        {
            map = new Dictionary<T, Color>();
            if (settings != null)
            {
                foreach (var entry in settings)
                {
                    if (!map.ContainsKey(entry.Type))
                        map.Add(entry.Type, entry.Color);
                }
            }
        }

        return map.TryGetValue(type, out var color) ? color : defaultColor;
    }
}