using System;
using System.Text;

// ─────────────────────────────────────────────
// HangulUtils.cs
// 한글 초성 검색 지원 유틸리티
// 예: "아루" 검색 시 "ㅇㄹ" 로도 매칭되도록
// ─────────────────────────────────────────────
public static class HangulUtils
{
    private static readonly char[] Chosung =
    {
        'ㄱ','ㄲ','ㄴ','ㄷ','ㄸ','ㄹ','ㅁ','ㅂ','ㅃ','ㅅ',
        'ㅆ','ㅇ','ㅈ','ㅉ','ㅊ','ㅋ','ㅌ','ㅍ','ㅎ'
    };

    private const int HangulBase = 0xAC00; // '가'
    private const int HangulLast = 0xD7A3; // '힣'
    private const int JungCount = 21;
    private const int JongCount = 28;

    /// <summary>완성형 한글 음절 문자인지 (예: '아', '루')</summary>
    public static bool IsHangulSyllable(char c) => c >= HangulBase && c <= HangulLast;

    /// <summary>초성 자음 문자인지 (예: 'ㄱ', 'ㅇ')</summary>
    public static bool IsChosungJamo(char c) => Array.IndexOf(Chosung, c) >= 0;

    /// <summary>
    /// 문자열이 (공백을 제외하고) 초성 자음으로만 이루어져 있는지 판별.
    /// 검색어를 "초성 검색 모드"로 취급할지 결정하는 데 사용.
    /// </summary>
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

    /// <summary>
    /// 문자열에서 한글 음절만 초성으로 치환. 한글이 아닌 문자(영문, 숫자 등)는 그대로 통과.
    /// 예: "아루나" -> "ㅇㄹㄴ", "Aru" -> "Aru"
    /// </summary>
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
