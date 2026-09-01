
public interface ITurnOrderRule
{
    DraftSide GetSideForTurn(int turnIndex);
}

public sealed class AlternatingTurnOrderRule : ITurnOrderRule
{
    private readonly DraftSide startingSide;

    public AlternatingTurnOrderRule(DraftSide startingSide = DraftSide.First)
    {
        this.startingSide = startingSide;
    }

    public DraftSide GetSideForTurn(int turnIndex)
    {
        bool isStarterTurn = turnIndex % 2 == 0;
        if (isStarterTurn) return startingSide;
        return startingSide == DraftSide.First ? DraftSide.Second : DraftSide.First;
    }
}