using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text;
using Newtonsoft.Json; // Newtonsoft.Json 패키지 필요
using System;

public class CsvToJsonConverter : EditorWindow
{
    /*
    [MenuItem("Tools/Data/Convert Item CSV to JSON")]
    public static void ConvertCsvToJson()
    {
        // 1. CSV 파일 선택
        string csvPath = EditorUtility.OpenFilePanel("Select Item CSV", "", "csv");
        if (string.IsNullOrEmpty(csvPath)) return;

        // 2. CSV 파일 읽기
        //string[] lines = File.ReadAllLines(csvPath);
        string[] lines = File.ReadAllLines(csvPath, Encoding.Default);
        if (lines.Length <= 1)
        {
            Debug.LogError("CSV 파일이 비어있거나 헤더만 있습니다.");
            return;
        }

        // 3. 헤더 파싱 (열 순서가 바뀌어도 동작하도록 인덱스 찾기)
        string[] headers = SplitCsvLine(lines[0]);
        Dictionary<string, int> headerMap = new Dictionary<string, int>();
        for (int i = 0; i < headers.Length; i++)
        {
            headerMap[headers[i].Trim()] = i;
        }

        List<ItemData> itemList = new List<ItemData>();

        // 4. 데이터 파싱 (2번째 줄부터 시작)
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i];
            if (string.IsNullOrWhiteSpace(line)) continue;

            string[] values = SplitCsvLine(line);
            
            try
            {
                ItemData item = new ItemData();

                // ID (필수)
                item.Id = GetValue(values, headerMap, "Id");
                
                // Enums (오타 방지를 위해 TryParse 사용 권장)
                string mainCatStr = GetValue(values, headerMap, "MainCategory");
                string subCatStr = GetValue(values, headerMap, "SubCategory");
                string rarityStr = GetValue(values, headerMap, "Rarity");

                if (Enum.TryParse(mainCatStr, out MainItemCategory mainCat)) item.MainCategory = mainCat;
                else Debug.LogWarning($"줄 {i+1}: 알 수 없는 MainCategory '{mainCatStr}'");

                if (Enum.TryParse(subCatStr, out SubItemCategory subCat)) item.SubCategory = subCat;
                else item.SubCategory = SubItemCategory.None; // 실패시 None

                if (Enum.TryParse(rarityStr, out ItemGrade grade)) item.Rarity = grade;
                else item.Rarity = ItemGrade.N; // 기본값

                // Strings
                item.NameKey = GetValue(values, headerMap, "Name");
                item.DescriptionKey = GetValue(values, headerMap, "Description");

                // Numbers (long)
                string maxStackStr = GetValue(values, headerMap, "MaxStack");
                if (long.TryParse(maxStackStr, out long maxStack)) item.MaxStack = maxStack;
                else item.MaxStack = 9999; // 파싱 실패 시 기본값

                itemList.Add(item);
            }
            catch (Exception e)
            {
                Debug.LogError($"CSV 파싱 에러 (줄 {i + 1}): {e.Message}");
            }
        }

        // 5. JSON 변환 및 저장
        string json = JsonConvert.SerializeObject(itemList, Formatting.Indented);
        
        string savePath = EditorUtility.SaveFilePanel("Save ItemData JSON", "Assets/Resources/Data", "ItemData", "json");
        if (!string.IsNullOrEmpty(savePath))
        {
            File.WriteAllLines(savePath, new [] { json });
            AssetDatabase.Refresh();
            Debug.Log($"✅ JSON 변환 완료! ({itemList.Count}개 아이템) -> {savePath}");
        }
    }

    // 엑셀 CSV 특유의 따옴표 처리 (예: "설명에, 쉼표가, 있어요")
    private static string[] SplitCsvLine(string line)
    {
        // 정규표현식: 쉼표로 나누되, 따옴표 안의 쉼표는 무시
        string pattern = @",(?=(?:[^""]*""[^""]*"")*(?![^""]*""))";
        string[] values = Regex.Split(line, pattern);

        for (int i = 0; i < values.Length; i++)
        {
            // 앞뒤 따옴표 제거 및 엑셀 이스케이프 문자("") 처리
            values[i] = values[i].Trim().Trim('"').Replace("\"\"", "\"");
        }
        return values;
    }

    private static string GetValue(string[] values, Dictionary<string, int> map, string columnName)
    {
        if (map.TryGetValue(columnName, out int index) && index < values.Length)
        {
            return values[index];
        }
        return "";
    }
 */   
}