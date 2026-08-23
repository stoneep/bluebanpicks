using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 밴픽 포맷(라운드별 슬롯 수 + 턴 순서)의 "에디터 프리셋 템플릿".
///
/// 실제 실행 중인 드래프트의 소스오브트루스는 더 이상 이 SO가 아니다.
/// 대기실에서 이 프리셋을 불러와 DraftFormatData(런타임 데이터)로 복제한 뒤
/// 그 복제본을 수정/네트워크 전송하고, RuleManager에는 그 복제본을 넘긴다.
/// (에셋을 런타임에 직접 고치면 플레이 모드 종료 후에도 값이 남거나
///  여러 매치가 같은 에셋을 공유하며 충돌할 수 있으므로 절대 금지)
/// </summary>
[CreateAssetMenu(menuName = "Config/DraftFormat", fileName = "DraftFormat")]
public class DraftFormatSO : ScriptableObject, IDraftFormat
{
    [Header("라운드 목록 (순서대로 진행됨: 전반전, 후반전, ...)")]
    [SerializeField] private List<DraftRoundConfig> rounds = new();

    public IReadOnlyList<DraftRoundConfig> Rounds => rounds;

    /// <summary>이 프리셋을 대기실에서 편집 가능한 런타임 복사본으로 변환.</summary>
    public DraftFormatData ToRuntimeData() => DraftFormatData.FromPreset(this);
}