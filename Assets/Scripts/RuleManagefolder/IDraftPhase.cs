using System.Collections.Generic;

public interface IDraftPhase
{
    string PhaseName { get; }
    
    bool IsComplete { get; }
    
    DraftSide CurrentSide { get; }
    
    void Enter();
    
    bool SubmitAction(DraftSide side, string characterId, out string error);
    
    IReadOnlyList<string> GetSelections(DraftSide side);
}