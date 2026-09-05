using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Text.RegularExpressions;

/// <summary>
/// EquipmentBadge CSV → JSON 변환기
/// - 장비 아이템의 뱃지 확장 데이터만 변환
/// - Category_Equipment 아이템만 해당
/// </summary>
public class EquipmentBadgeJsonConverter : EditorWindow
{
    /*
    [MenuItem("Tools/Data/Convert EquipmentBadge CSV to JSON")]
    public static void ConvertCsvToJson()
    {
        // 1. CSV 파일 선택
        string csvPath = EditorUtility.OpenFilePanel("Select EquipmentBadge CSV", "", "csv");
        if (string.IsNullOrEmpty(csvPath)) return;

        try
        {
            // 2. CSV 읽기
            string[] lines = File.ReadAllLines(csvPath, Encoding.Default);
            if (lines.Length <= 1)
            {
                Debug.LogError("CSV 파일이 비어있거나 헤더만 있습니다.");
                return;
            }

            // 3. 헤더 파싱
            string[] headers = SplitCsvLine(lines[0]);
            Dictionary<string, int> headerMap = new Dictionary<string, int>();
            for (int i = 0; i < headers.Length; i++)
            {
                headerMap[headers[i].Trim()] = i;
            }

            // 4. 데이터 파싱
            EquipmentBadgeData badgeData = new EquipmentBadgeData();

            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;

                string[] values = SplitCsvLine(line);

                try
                {
                    EquipmentBadge badge = new EquipmentBadge();

                    // 필수 필드
                    badge.ItemId = GetValue(values, headerMap, "ItemId");
                    
                    if (string.IsNullOrEmpty(badge.ItemId))
                    {
                        Debug.LogWarning($"줄 {i+1}: ItemId가 비어있음 - 스킵");
                        continue;
                    }

                    // 옵션 필드
                    badge.BadgeIconPath = GetValue(values, headerMap, "BadgeIcon");
                    badge.BadgeText = GetValue(values, headerMap, "BadgeText");

                    // 둘 다 비어있으면 의미 없으므로 스킵
                    if (string.IsNullOrEmpty(badge.BadgeIconPath) && 
                        string.IsNullOrEmpty(badge.BadgeText))
                    {
                        Debug.LogWarning($"줄 {i+1}: 뱃지 정보가 없음 (ItemId: {badge.ItemId}) - 스킵");
                        continue;
                    }

                    badgeData.badges.Add(badge);
                }
                catch (Exception e)
                {
                    Debug.LogError($"CSV 파싱 에러 (줄 {i + 1}): {e.Message}");
                }
            }

            // 5. JSON 변환
            var settings = new JsonSerializerSettings();
            settings.Formatting = Formatting.Indented;
            settings.Converters.Add(new StringEnumConverter());

            string json = JsonConvert.SerializeObject(badgeData, settings);

            // 6. 저장
            string savePath = EditorUtility.SaveFilePanel(
                "Save EquipmentBadge JSON", 
                "Assets/Resources/Data", 
                "EquipmentBadge", 
                "json"
            );

            if (!string.IsNullOrEmpty(savePath))
            {
                File.WriteAllText(savePath, json);
                AssetDatabase.Refresh();
                Debug.Log($"✅ JSON 변환 완료! ({badgeData.badges.Count}개 뱃지) -> {savePath}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"변환 중 오류 발생: {ex.Message}");
        }
    }

    // CSV 라인 파싱 (쉼표 처리)
    private static string[] SplitCsvLine(string line)
    {
        string pattern = @",(?=(?:[^""]*""[^""]*"")*(?![^""]*""))";
        string[] values = Regex.Split(line, pattern);

        for (int i = 0; i < values.Length; i++)
        {
            values[i] = values[i].Trim().Trim('"').Replace("\"\"", "\"");
        }
        return values;
    }

    // 컬럼 값 가져오기
    private static string GetValue(string[] values, Dictionary<string, int> map, string columnName)
    {
        if (map.TryGetValue(columnName, out int index) && index < values.Length)
        {
            return values[index].Trim();
        }
        return "";
    }
    */
}
