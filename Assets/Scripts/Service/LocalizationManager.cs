using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public enum Language { KR = 1, EN = 2 } // CSV 열(Column) 인덱스와 일치시킴

public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private Language currentLanguage = Language.KR;
    [SerializeField] private string csvPath = "Data/Localization";

    // Key: 번역키, Value: [KR문장, EN문장] 배열
    private readonly Dictionary<string, string[]> _table = new();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); //
            LoadLocalizationCsv();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void LoadLocalizationCsv()
    {
        TextAsset csvFile = Resources.Load<TextAsset>(csvPath); //
        if (csvFile == null)
        {
            Debug.LogError($"❌ [LocalizationManager] CSV를 찾을 수 없습니다: {csvPath}");
            return;
        }

        string[] lines = csvFile.text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        
        // CSV 파싱용 정규표현식 (따옴표 안의 쉼표 무시)
        string pattern = @",(?=(?:[^""]*""[^""]*"")*(?![^""]*""))";

        for (int i = 1; i < lines.Length; i++) // 헤더 제외
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            string[] values = Regex.Split(lines[i], pattern);
            if (values.Length < 3) continue;

            string key = values[0].Trim().Trim('"');
            string kr = values[1].Trim().Trim('"').Replace("\"\"", "\"");
            string en = values[2].Trim().Trim('"').Replace("\"\"", "\"");

            _table[key] = new[] { kr, en };
        }

        Debug.Log($"✅ [LocalizationManager] {_table.Count}개의 번역 데이터 로드 완료!");
    }

    public string Get(string key)
    {
        if (string.IsNullOrEmpty(key)) return "";

        if (_table.TryGetValue(key, out string[] translations))
        {
            // Language enum 값을 인덱스로 사용하여 텍스트 반환
            int index = (int)currentLanguage - 1;
            return translations[index];
        }

        return key; // 키가 없으면 키 그대로 반환 (버그 확인용)
    }

    // 언어 변경 기능
    public void SetLanguage(Language lang)
    {
        currentLanguage = lang;
        // 필요 시 UI 전체 갱신 이벤트 호출 가능
    }
}