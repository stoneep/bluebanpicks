using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 시퀀스 범위를 벗어난 turnIndex를 어떻게 처리할지 정책.
/// - RepeatLast: 마지막 값을 계속 반복 (기본값, 안전한 fallback)
/// - Loop: 시퀀스를 처음부터 다시 순환
/// - Throw: 즉시 예외 (슬롯 수/시퀀스 길이가 안 맞는 설정 실수를 조기에 발견하고 싶을 때)
/// </summary>
public enum SequenceOverflowPolicy
{
    RepeatLast,
    Loop,
    Throw
}

/// <summary>
/// A,B,B,A,A,B 처럼 미리 정해진 순서를 그대로 따르는 규칙.
/// 스네이크 드래프트, 밴/픽 순서가 대칭이 아닌 커스텀 포맷 등에 사용.
/// AlternatingTurnOrderRule과 완전히 동일한 방식으로 교체 가능 (OCP).
/// </summary>
public sealed class SequenceTurnOrderRule : ITurnOrderRule
{
    private readonly IReadOnlyList<DraftSide> sequence;
    private readonly SequenceOverflowPolicy overflowPolicy;

    public int Length => sequence.Count;

    public SequenceTurnOrderRule(IEnumerable<DraftSide> sequence, SequenceOverflowPolicy overflowPolicy = SequenceOverflowPolicy.RepeatLast)
    {
        if (sequence == null)
            throw new ArgumentNullException(nameof(sequence));

        var list = sequence.ToArray();
        if (list.Length == 0)
            throw new ArgumentException("순서 시퀀스는 최소 1개 이상이어야 합니다.", nameof(sequence));

        this.sequence = list;
        this.overflowPolicy = overflowPolicy;
    }

    // 기존 호출부(params) 하위 호환용 오버로드
    public SequenceTurnOrderRule(params DraftSide[] sequence)
        : this((IEnumerable<DraftSide>)sequence, SequenceOverflowPolicy.RepeatLast)
    {
    }

    public DraftSide GetSideForTurn(int turnIndex)
    {
        if (turnIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(turnIndex));

        if (turnIndex < sequence.Count)
            return sequence[turnIndex];

        return overflowPolicy switch
        {
            SequenceOverflowPolicy.Loop => sequence[turnIndex % sequence.Count],
            SequenceOverflowPolicy.Throw => throw new InvalidOperationException(
                $"[SequenceTurnOrderRule] turnIndex({turnIndex})가 시퀀스 길이({sequence.Count})를 벗어났습니다. " +
                "슬롯 수와 시퀀스 길이가 일치하는지 확인하세요."),
            _ => sequence[sequence.Count - 1] // RepeatLast
        };
    }

    /// <summary>
    /// 이 시퀀스의 First/Second 개수가 실제 밴/픽 슬롯 수와 일치하는지 검증한다.
    /// DraftPhaseBase.Enter() 등 페이즈 시작 시점에 호출해 설정 실수를 조기에 잡는 용도.
    /// (선택사항 - 호출하지 않아도 GetSideForTurn은 overflowPolicy대로 안전하게 동작함)
    /// </summary>
    public void Validate(int firstSlotCount, int secondSlotCount)
    {
        int firstCount = sequence.Count(s => s == DraftSide.First);
        int secondCount = sequence.Count(s => s == DraftSide.Second);

        if (firstCount != firstSlotCount || secondCount != secondSlotCount)
        {
            throw new InvalidOperationException(
                $"[SequenceTurnOrderRule] 시퀀스 구성(First={firstCount}, Second={secondCount})이 " +
                $"슬롯 수(First={firstSlotCount}, Second={secondSlotCount})와 일치하지 않습니다.");
        }
    }

    /// <summary>
    /// 기획자/기획데이터 친화적인 문자열 생성자.
    /// 'A' -> DraftSide.First, 'B' -> DraftSide.Second (대소문자 무관)
    /// 예: SequenceTurnOrderRule.FromPattern("ABBAAB")
    /// </summary>
    public static SequenceTurnOrderRule FromPattern(string pattern, SequenceOverflowPolicy overflowPolicy = SequenceOverflowPolicy.RepeatLast)
    {
        if (string.IsNullOrEmpty(pattern))
            throw new ArgumentException("패턴 문자열이 비어 있습니다.", nameof(pattern));

        var sides = new DraftSide[pattern.Length];
        for (int i = 0; i < pattern.Length; i++)
        {
            char c = char.ToUpperInvariant(pattern[i]);
            if (c != 'A' && c != 'B')
                throw new ArgumentException($"패턴은 'A'/'B'만 허용합니다. (index {i}: '{pattern[i]}')", nameof(pattern));

            sides[i] = (c == 'A') ? DraftSide.First : DraftSide.Second;
        }
        return new SequenceTurnOrderRule(sides, overflowPolicy);
    }
}