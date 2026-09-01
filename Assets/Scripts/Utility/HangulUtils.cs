using System;
using System.Text;

public static class HangulUtils
{
    private static readonly char[] Chosung =
    {
        'ㄱ','ㄲ','ㄴ','ㄷ','ㄸ','ㄹ','ㅁ','ㅂ','ㅃ','ㅅ',
        'ㅆ','ㅇ','ㅈ','ㅉ','ㅊ','ㅋ','ㅌ','ㅍ','ㅎ'
    };

    private const int HangulBase = 0xAC00;
    private const int HangulLast = 0xD7A3;
    private const int JungCount = 21;
    private const int JongCount = 28;
    
    public static bool IsHangulSyllable(char c) => c >= HangulBase && c <= HangulLast;
    
    public static bool IsChosungJamo(char c) => Array.IndexOf(Chosung, c) >= 0;
    
    public static bool IsChosungOnly(string text)
    {
        if (string.IsNullOrEmpty(text)) return false;

        bool hasAny = false;
        foreach (var c in text)
        {
            if (char.IsWhiteSpace(c)) continue;
            if (!IsChosungJamo(c)) return false;
            hasAny = true;
        }
        return hasAny;
    }
    
    public static string ExtractChosung(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;

        var sb = new StringBuilder(text.Length);
        foreach (var c in text)
        {
            if (IsHangulSyllable(c))
            {
                int offset = c - HangulBase;
                int chosungIndex = offset / (JungCount * JongCount);
                sb.Append(Chosung[chosungIndex]);
            }
            else
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }
}
