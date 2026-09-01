using System.Collections.Generic;
using UnityEngine;










[CreateAssetMenu(menuName = "Config/DraftFormat", fileName = "DraftFormat")]
public class DraftFormatSO : ScriptableObject, IDraftFormat
{
    [Header("라운드 목록 (순서대로 진행됨: 전반전, 후반전, ...)")]
    [SerializeField] private List<DraftRoundConfig> rounds = new();

    public IReadOnlyList<DraftRoundConfig> Rounds => rounds;

    
    public DraftFormatData ToRuntimeData() => DraftFormatData.FromPreset(this);
}