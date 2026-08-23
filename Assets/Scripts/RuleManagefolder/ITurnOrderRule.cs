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
/// 선공, 후공, 선공, 후공 ... 단순 교대.
/// </summary>
public sealed class AlternatingTurnOrderRule : ITurnOrderRule
{
    public DraftSide GetSideForTurn(int turnIndex)
    {
        return (turnIndex % 2 == 0) ? DraftSide.First : DraftSide.Second;
    }
}