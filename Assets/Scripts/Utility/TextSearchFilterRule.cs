using System;
using System.Collections.Generic;

public class TextSearchFilterRule<T> : IFilterRule<T>
{
    private readonly Func<T, IEnumerable<string>> _fieldsSelector;

    private string _term = string.Empty;
    private bool _isChosungQuery;
    
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
            
            if (field.IndexOf(_term, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            
            if (_isChosungQuery)
            {
                var fieldChosung = HangulUtils.ExtractChosung(field);
                if (fieldChosung.IndexOf(_term, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
        }

        return false;
    }
    
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
                    best = Math.Min(best, chosungScore + 10);
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