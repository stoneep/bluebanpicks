using System.Collections.Generic;

/// <summary>
/// RuleManager가 드래프트를 구성하기 위해 필요로 하는 최소 인터페이스.
///
/// 이전에는 RuleManager가 DraftFormatSO(ScriptableObject)에 직접 의존해서
/// "에디터에서 미리 구워둔 에셋"으로만 드래프트를 시작할 수 있었다.
/// 이제는 이 인터페이스만 의존하므로:
///  - DraftFormatSO (에디터 프리셋/템플릿)
///  - DraftFormatData (대기실 UI에서 조립하고, 나중에 네트워크로 직렬화해서 보낼 런타임 데이터)
/// 둘 다 동일하게 RuleManager에 꽂아 쓸 수 있다.
/// </summary>
public interface IDraftFormat
{
    /// <summary>
    /// 순서대로 진행될 라운드 목록 (전반전, 후반전, ... N라운드까지 확장 가능).
    /// RuleManager는 이 순서 그대로 Ban→Pick 페이즈 쌍을 라운드마다 만든다.
    /// </summary>
    IReadOnlyList<DraftRoundConfig> Rounds { get; }
}