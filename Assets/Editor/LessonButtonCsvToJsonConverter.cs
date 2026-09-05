using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text;
using Newtonsoft.Json;
using System;

/// <summary>
/// Lesson 버튼 위치 CSV → JSON 변환기
/// CSV 빈 셀은 직전 행의 값을 계승
/// 
/// CSV 형식:
/// LessonEnum,LocationGroup,LocationName,ButtonId,PosX,PosY
/// SchaleOffice,A,시청각실,SchaleOfficeA01,138,-713
/// ,,,SchaleOfficeA02,255,-837        ← LessonEnum/Group/Name 계승
/// ,B,체육관,SchaleOfficeB01,500,-400  ← LessonEnum만 계승
/// </summary>
public class LessonButtonCsvToJsonConverter : EditorWindow
{
    /*
    [MenuItem("Tools/Data/Convert LessonButton CSV to JSON")]
    public static void ConvertLessonButtonCsv()
    {
        string csvPath = EditorUtility.OpenFilePanel("Select LessonButton CSV", "", "csv");
        if (string.IsNullOrEmpty(csvPath)) return;

        string[] lines = File.ReadAllLines(csvPath, Encoding.Default);
        if (lines.Length <= 1)
        {
            Debug.LogError("CSV 파일이 비어있거나 헤더만 있습니다.");
            return;
        }

        // 헤더 파싱
        string[] headers = SplitCsvLine(lines[0]);
        var headerMap = new Dictionary<string, int>();
        for (int i = 0; i < headers.Length; i++)
            headerMap[headers[i].Trim()] = i;

        // ── 빈 셀 계승을 위한 상태 변수 ──
        string currentLesson = "";
        string currentGroup = "";
        string currentLocationName = "";

        // 중간 구조: Lesson → Group → (LocationName, Buttons)
        var lessonMap = new Dictionary<string, Dictionary<string, (string locationName, List<LessonButtonData.ButtonEntry> buttons)>>();

        // 데이터 파싱 (2번째 줄부터)
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i];
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;

            string[] values = SplitCsvLine(line);

            try
            {
                // ── 빈 셀 계승 처리 ──
                string rawLesson = GetVal(values, headerMap, "LessonEnum");
                string rawGroup = GetVal(values, headerMap, "LocationGroup");
                string rawLocation = GetVal(values, headerMap, "LocationName");

                if (!string.IsNullOrEmpty(rawLesson)) currentLesson = rawLesson;
                if (!string.IsNullOrEmpty(rawGroup)) currentGroup = rawGroup;
                if (!string.IsNullOrEmpty(rawLocation)) currentLocationName = rawLocation;

                // 필수값 검증
                string buttonId = GetVal(values, headerMap, "ButtonId");
                if (string.IsNullOrEmpty(buttonId))
                {
                    Debug.LogWarning($"줄 {i + 1}: ButtonId가 비어있어 건너뜁니다.");
                    continue;
                }

                if (string.IsNullOrEmpty(currentLesson) || string.IsNullOrEmpty(currentGroup))
                {
                    Debug.LogWarning($"줄 {i + 1}: LessonEnum 또는 LocationGroup을 결정할 수 없습니다.");
                    continue;
                }

                // Enum 유효성 검증
                if (!Enum.TryParse<LessonEnum>(currentLesson, out _))
                {
                    Debug.LogWarning($"줄 {i + 1}: 알 수 없는 LessonEnum '{currentLesson}'");
                    continue;
                }
                if (!Enum.TryParse<LocationGroup>(currentGroup, out _))
                {
                    Debug.LogWarning($"줄 {i + 1}: 알 수 없는 LocationGroup '{currentGroup}'");
                    continue;
                }

                // 좌표 파싱
                float.TryParse(GetVal(values, headerMap, "PosX"), out float posX);
                float.TryParse(GetVal(values, headerMap, "PosY"), out float posY);

                // ── 중간 구조에 적재 ──
                if (!lessonMap.ContainsKey(currentLesson))
                    lessonMap[currentLesson] = new Dictionary<string, (string, List<LessonButtonData.ButtonEntry>)>();

                var groupMap = lessonMap[currentLesson];
                if (!groupMap.ContainsKey(currentGroup))
                    groupMap[currentGroup] = (currentLocationName, new List<LessonButtonData.ButtonEntry>());

                groupMap[currentGroup].buttons.Add(new LessonButtonData.ButtonEntry
                {
                    ButtonId = buttonId,
                    PosX = posX,
                    PosY = posY
                });
            }
            catch (Exception e)
            {
                Debug.LogError($"CSV 파싱 에러 (줄 {i + 1}): {e.Message}");
            }
        }

        // ── 최종 DTO 조립 ──
        var data = new LessonButtonData();

        foreach (var lessonKvp in lessonMap)
        {
            var lessonEntry = new LessonButtonData.LessonEntry { LessonType = lessonKvp.Key };

            foreach (var groupKvp in lessonKvp.Value)
            {
                lessonEntry.Groups.Add(new LessonButtonData.GroupEntry
                {
                    GroupType = groupKvp.Key,
                    LocationName = groupKvp.Value.locationName,
                    Buttons = groupKvp.Value.buttons
                });
            }

            data.Lessons.Add(lessonEntry);
        }

        // ── JSON 저장 ──
        string json = JsonConvert.SerializeObject(data, Formatting.Indented);

        string savePath = EditorUtility.SaveFilePanel(
            "Save LessonButton JSON", "Assets/Resources/Data", "LessonButtonData", "json");

        if (!string.IsNullOrEmpty(savePath))
        {
            File.WriteAllText(savePath, json, Encoding.UTF8);
            AssetDatabase.Refresh();

            // 통계 로그
            int totalButtons = 0;
            foreach (var l in data.Lessons)
                foreach (var g in l.Groups)
                    totalButtons += g.Buttons.Count;

            Debug.Log($"✅ LessonButton 변환 완료! " +
                      $"({data.Lessons.Count}개 Lesson, {totalButtons}개 버튼) → {savePath}");
        }
    }

    // ── 헬퍼 (기존 프로젝트 공용 패턴) ──

    private static string GetVal(string[] values, Dictionary<string, int> map, string col)
    {
        return (map.TryGetValue(col, out int i) && i < values.Length) ? values[i] : "";
    }

    private static string[] SplitCsvLine(string line)
    {
        string pattern = @",(?=(?:[^""]*""[^""]*"")*(?![^""]*""))";
        string[] values = Regex.Split(line, pattern);
        for (int i = 0; i < values.Length; i++)
            values[i] = values[i].Trim().Trim('"').Replace("\"\"", "\"");
        return values;
    }
    */
}
