using System;
using System.Collections.Generic;
using System.Linq;

public enum SequenceOverflowPolicy
{
    RepeatLast,
    Loop,
    Throw
}

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
            _ => sequence[sequence.Count - 1]
        };
    }
    
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