using System.Collections.Generic;

/// <summary>
/// 밴 페이즈 / 픽 페이즈가 공통으로 구현하는 인터페이스.
/// RuleManager는 구체 타입(BanPhase, PickPhase)을 몰라도
/// 이 인터페이스만으로 진행을 제어할 수 있음.
/// </summary>
public interface IDraftPhase
{
    /// <summary>UI 표시/로그용 이름 (예: "Ban", "Pick")</summary>
    string PhaseName { get; }

    /// <summary>이번 페이즈에서 양측 슬롯이 모두 채워졌는지</summary>
    bool IsComplete { get; }

    /// <summary>지금 행동해야 하는 진영. IsComplete면 의미 없음.</summary>
    DraftSide CurrentSide { get; }

    /// <summary>페이즈 시작 시 1회 호출. 턴 카운터를 초기화하고 첫 차례를 정한다.</summary>
    void Enter();

    /// <summary>
    /// 지정한 진영이 characterId를 선택(밴/픽)한다.
    /// 차례가 아니거나, 슬롯이 이미 다 찼거나, 페이즈가 끝났으면 실패.
    /// </summary>
    bool SubmitAction(DraftSide side, string characterId, out string error);

    /// <summary>해당 진영이 지금까지 고른 캐릭터 목록 (읽기 전용)</summary>
    IReadOnlyList<string> GetSelections(DraftSide side);
}