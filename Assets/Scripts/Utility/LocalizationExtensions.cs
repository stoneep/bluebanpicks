using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class LocalizationExtensions
{
    public static string Localize(this string key)
    {
        if (LocalizationManager.Instance == null) return key;
        return LocalizationManager.Instance.Get(key);
    }
}

