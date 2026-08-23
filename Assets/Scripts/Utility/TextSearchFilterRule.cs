using System;
using System.Collections.Generic;

// ─────────────────────────────────────────────
// TextSearchFilterRule.cs
// 여러 문자열 필드를 대상으로 하는 텍스트 검색 필터 규칙
//
// - 대소문자 무시 부분일치(Contains)
// - 검색어가 초성으로만 구성된 경우, 대상 필드의 초성과도 비교
//   (예: "ㅇㄹ" 입력 시 "아루" 매칭)
// - GenericFilterRule과 동일한 패턴: 필드 선택자를 생성자로 주입받아 재사용 가능
// ─────────────────────────────────────────────
public class TextSearchFilterRule<T> : IFilterRule<T>
{
    private readonly Func<T, IEnumerable<string>> _fieldsSelector;

    private string _term = string.Empty;
    private bool _isChosungQuery;

    /// <summary>현재 적용 중인 검색어 (트림된 상태)</summary>
    public string Term => _term;

    public TextSearchFilterRule(Func<T, IEnumerable<string>> fieldsSelector)
    {
        _fieldsSelector = fieldsSelector ?? throw new ArgumentNullException(nameof(fieldsSelector));
    }

    public void Set(string term)
    {
        _term = (term ?? string.Empty).Trim();
        _isChosungQuery = HangulUtils.IsChosungOnly(_term);
    }

    public void Clear() => Set(string.Empty);

    public bool IsSatisfiedBy(T item)
    {
        if (string.IsNullOrEmpty(_term)) return true;

        foreach (var field in _fieldsSelector(item))
        {
            if (string.IsNullOrEmpty(field)) continue;

            // 일반 부분일치 (영문/한글 표기 그대로)
            if (field.IndexOf(_term, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            // 초성 검색 모드일 때만 초성 비교 수행 (매 프레임 불필요한 변환 방지)
            if (_isChosungQuery)
            {
                var fieldChosung = HangulUtils.ExtractChosung(field);
                if (fieldChosung.IndexOf(_term, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
        }

        return false;
    }
    
    /// <summary>
    /// 검색어와의 관련도 점수 (낮을수록 관련도 높음)
    /// 0 = 정확히 일치, 1 = 시작 문자열 일치, 2 = 포함, int.MaxValue = 검색어 없거나 매칭 없음
    /// 초성 매칭은 텍스트 직접 매칭보다 후순위로 취급
    /// </summary>
    public int GetRelevance(T item)
    {
        if (string.IsNullOrEmpty(_term)) return int.MaxValue;

        int best = int.MaxValue;

        foreach (var field in _fieldsSelector(item))
        {
            if (string.IsNullOrEmpty(field)) continue;

            int score = ScoreField(field, _term);
            if (score < best) best = score;

            if (_isChosungQuery)
            {
                var chosungScore = ScoreField(HangulUtils.ExtractChosung(field), _term);
                if (chosungScore != int.MaxValue)
                    best = Math.Min(best, chosungScore + 10); // 초성 매칭은 한 단계 아래로
            }
        }

        return best;
    }

    private static int ScoreField(string field, string term)
    {
        if (string.Equals(field, term, StringComparison.OrdinalIgnoreCase)) return 0;
        if (field.StartsWith(term, StringComparison.OrdinalIgnoreCase)) return 1;
        if (field.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0) return 2;
        return int.MaxValue;
    }
}