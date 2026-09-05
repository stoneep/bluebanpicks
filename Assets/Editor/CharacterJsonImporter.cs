#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// CSV → JSON 변환 에디터 툴
/// CharData.csv를 읽어 CharDatabaseDTO 형식의 characters.json 생성
/// 
/// CSV 헤더:
/// Id,DisplayName,Level,Rarity,Affiliation,TacticalRole,Role,Position,
/// AttackType,DefenseType,WeaponClass,Weapon,Armor,Accessory,Unique,Urban,Field,Indoor
/// </summary>
public static class CharacterJsonImporter
{
    [MenuItem("Tools/Characters/Convert CSV to JSON")]
    public static void ConvertCsvToJson()
    {
        string CsvPath = EditorUtility.OpenFilePanel("Select CharData CSV", "", "csv");
        string OutputJsonPath = EditorUtility.SaveFilePanel("Save CharactersData JSON", "Assets/Resources/Data", "charactersData", "json");

        // Escape
        if (string.IsNullOrEmpty(CsvPath)) return;
        string[] lines = File.ReadAllLines(CsvPath, Encoding.Default);
        if (lines.Length <= 1) return;
        
        try
        {
            var entries = ParseCsv(CsvPath);
            var root = new CharDatabaseRoot { characters = entries.ToArray() };
            
            // JsonUtility로 JSON 생성
            string json = JsonUtility.ToJson(root, prettyPrint: true);
            
            // 출력 디렉토리 확인
            var dir = Path.GetDirectoryName(OutputJsonPath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            
            File.WriteAllText(OutputJsonPath, json, Encoding.UTF8);
            
            AssetDatabase.Refresh();
            Debug.Log($"✅ CSV → JSON 변환 완료!\n" +
                      $"   입력: {CsvPath}\n" +
                      $"   출력: {OutputJsonPath}\n" +
                      $"   항목 수: {entries.Count}개");
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ CSV → JSON 변환 실패: {ex.Message}\n{ex.StackTrace}");
        }
    }

    private static List<CharEntry> ParseCsv(string path)
    {
        var lines = File.ReadAllLines(path, Encoding.UTF8);
        if (lines.Length < 2)
        {
            throw new Exception("CSV 파일이 비어있거나 헤더만 존재합니다.");
        }

        // BOM 제거 및 헤더 파싱
        var header = lines[0].TrimStart('\uFEFF').Split(',');
        var entries = new List<CharEntry>();

        for (int i = 1; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(line))
                continue; // 빈 줄 스킵

            try
            {
                var entry = ParseLine(line, header, i + 1);
                entries.Add(entry);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"⚠️ {i + 1}번 줄 파싱 실패: {ex.Message}\n   줄 내용: {line}");
            }
        }

        return entries;
    }

    private static CharEntry ParseLine(string line, string[] header, int lineNumber)
    {
        var values = SplitCsvLine(line);
        
        if (values.Length != header.Length)
        {
            throw new Exception(
                $"컬럼 개수 불일치 (예상: {header.Length}, 실제: {values.Length})");
        }

        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < header.Length; i++)
        {
            dict[header[i].Trim()] = values[i].Trim();
        }

        // CharEntry 생성
        var entry = new CharEntry
        {
            Id = GetRequired(dict, "Id", lineNumber),
            DisplayName = GetRequired(dict, "DisplayName", lineNumber),
            DisplayName_Kr = GetOptional(dict, "DisplayName_Kr"),
            Level = ParseInt(dict, "Level", lineNumber),
            Rarity = ParseInt(dict, "Rarity", lineNumber),
            
            Affiliation = GetOptional(dict, "Affiliation"),
            TacticalRole = GetOptional(dict, "TacticalRole"),
            Role = GetOptional(dict, "Role"),
            Position = GetOptional(dict, "Position"),
            AttackType = GetOptional(dict, "AttackType"),
            DefenseType = GetOptional(dict, "DefenseType"),
            WeaponClass = GetOptional(dict, "WeaponClass"),
            
            equip = new EquipDTO
            {
                weapon = GetOptional(dict, "Weapon"),
                armor = GetOptional(dict, "Armor"),
                accessory = GetOptional(dict, "Accessory"),
                unique = ParseBool(dict, "Unique")
            },
            
            Preferred = new TerrainDTO
            {
                Urban = ParseTerrainGrade(dict, "Urban", lineNumber),   // ← 변경
                Field = ParseTerrainGrade(dict, "Field", lineNumber),   // ← 변경
                Indoor = ParseTerrainGrade(dict, "Indoor", lineNumber)  // ← 변경
            }
        };

        return entry;
    }

    /// <summary>
    /// CSV 라인을 쉼표로 분리 (따옴표 처리 포함)
    /// </summary>
    private static string[] SplitCsvLine(string line)
    {
        var result = new List<string>();
        var current = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }
        
        result.Add(current.ToString());
        return result.ToArray();
    }

    private static string GetRequired(Dictionary<string, string> dict, string key, int lineNumber)
    {
        if (!dict.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
        {
            throw new Exception($"필수 컬럼 '{key}'이(가) 비어있습니다 (라인: {lineNumber})");
        }
        return value;
    }

    private static string GetOptional(Dictionary<string, string> dict, string key)
    {
        return dict.TryGetValue(key, out var value) ? value : string.Empty;
    }

    private static int ParseInt(Dictionary<string, string> dict, string key, int lineNumber)
    {
        var value = GetOptional(dict, key);
        if (string.IsNullOrWhiteSpace(value))
            return 0;

        if (int.TryParse(value, out int result))
            return result;

        throw new Exception(
            $"'{key}' 값을 정수로 변환할 수 없습니다: '{value}' (라인: {lineNumber})");
    }

    private static bool ParseBool(Dictionary<string, string> dict, string key)
    {
        var value = GetOptional(dict, key);
        if (string.IsNullOrWhiteSpace(value))
            return false;

        // "true", "1", "yes" 등을 true로 처리
        value = value.ToLowerInvariant();
        return value == "true" || value == "1" || value == "yes" || value == "y";
    }
    
    private static int ParseTerrainGrade(Dictionary<string, string> dict, string key, int lineNumber)
    {
        var value = GetOptional(dict, key);
        if (string.IsNullOrWhiteSpace(value))
            return 6; // 기본값: D (6)

        // 숫자 입력 (1~6) 처리
        if (int.TryParse(value, out int numResult))
        {
            if (numResult >= 1 && numResult <= 6)
                return numResult;
        
            throw new Exception($"'{key}' 값은 1~6 사이여야 합니다: '{value}' (라인: {lineNumber})");
        }

        // 알파벳 입력 (SS, S, A, B, C, D) 처리
        var gradeMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            { "SS", 1 },
            { "S",  2 },
            { "A",  3 },
            { "B",  4 },
            { "C",  5 },
            { "D",  6 }
        };

        if (gradeMap.TryGetValue(value, out int grade))
            return grade;

        throw new Exception(
            $"'{key}' 값을 지형 등급으로 변환할 수 없습니다: '{value}' (SS/S/A/B/C/D 또는 1~6) (라인: {lineNumber})");
    }
}
#endif
