using System;
using System.Collections.Generic;
using UnityEngine;

// Enum 키 -> Color 값 매핑 데이터만 저장함
public abstract class EnumColorProfile<T> : ScriptableObject where T : struct, Enum
{
    [Serializable]
    private struct Entry
    {
        public T Type;
        public Color Color;
    }

    [SerializeField] private List<Entry> settings;

    // Lazy Initialization
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