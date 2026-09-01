using System.Collections.Generic;

public interface IDraftFormat
{
    IReadOnlyList<DraftRoundConfig> Rounds { get; }
}