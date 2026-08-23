/// <summary>
/// "N번째 턴은 어느 진영 차례인가"만 책임지는 규칙.
/// 페이즈(BanPhase/PickPhase)는 이 규칙을 몰라도 되고,
/// 나중에 스네이크 드래프트 등 다른 규칙으로 교체하고 싶을 때
/// 이 인터페이스의 구현체만 새로 만들면 됨 (OCP).
/// </summary>
public interface ITurnOrderRule
{
    /// <param name="turnIndex">0부터 시작하는 턴 번호 (페이즈 진입 시 0으로 리셋됨)</param>
    DraftSide GetSideForTurn(int turnIndex);
}

/// <summary>
/// 단순 교대 규칙. startingSide를 생략하면 기존과 동일하게 선공부터 교대하고,
/// DraftSide.Second를 넘기면 후공부터 교대한다 (후반전 이니셔티브 반전에 사용).
/// </summary>
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