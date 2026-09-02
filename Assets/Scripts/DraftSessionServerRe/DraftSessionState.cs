/// <summary>대기실(설정 편집) → 로딩 대기 → 진행 중 → 종료, 세션의 큰 흐름.</summary>
public enum DraftSessionState
{
    Lobby,

    /// <summary>밴픽씬 로드 완료 ~ 실제 드래프트 시작 전, UI 로딩 유예 시간(기본 15초) 동안의 상태.</summary>
    Loading,

    InProgress,
    Completed
}