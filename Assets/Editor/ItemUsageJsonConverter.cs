using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

public class ItemUsageJsonConverter : EditorWindow
{
    /*
    // =================================================================================
    // 기능 1: 보상 그룹 데이터 변환 (Reward Groups CSV -> JSON)
    // CSV 구조: GroupId | RewardItemId | Amount | DropRate
    // (GroupId가 비어있으면 윗줄의 그룹에 포함)
    // =================================================================================
    [MenuItem("Tools/Data/1. Convert Reward Groups (CSV to JSON)")]
    public static void ConvertRewardGroups()
    {
        string csvPath = EditorUtility.OpenFilePanel("Select RewardGroup CSV", "", "csv");
        if (string.IsNullOrEmpty(csvPath)) return;

        try
        {
            string csvContent = File.ReadAllText(csvPath, Encoding.Default);
            List<RewardGroup> groups = ParseRewardGroups(csvContent);

            SaveJson(groups, "RewardGroupData");
            Debug.Log($"[보상 그룹 변환 완료] 총 {groups.Count}개의 그룹 생성됨.");
        }
        catch (Exception e)
        {
            Debug.LogError($"오류 발생: {e.Message}");
        }
    }

    private static List<RewardGroup> ParseRewardGroups(string content)
    {
        var groupMap = new Dictionary<string, RewardGroup>();
        string[] lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        string currentGroupId = ""; // 빈 칸 처리를 위한 변수

        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i];
            if (string.IsNullOrWhiteSpace(line)) continue;
            string[] cols = line.Split(',');
            if (cols.Length < 4) continue;

            // 컬럼 매핑: 0:GroupId, 1:ItemId, 2:Amount, 3:Rate
            string cellGroupId = cols[0].Trim();
            string _itemId = cols[1].Trim();
            
            // Amount 파싱
            if (!int.TryParse(cols[2], out int _amount)) _amount = 1;
            
            // Rate 파싱
            if (!float.TryParse(cols[3], out float _rate)) _rate = 0;

            // 그룹 ID 채우기 (Fill Down)
            if (!string.IsNullOrEmpty(cellGroupId))
            {
                currentGroupId = cellGroupId;
            }

            if (string.IsNullOrEmpty(currentGroupId)) continue;

            // 1. 그룹 생성 또는 가져오기
            if (!groupMap.ContainsKey(currentGroupId))
            {
                groupMap.Add(currentGroupId, new RewardGroup { 
                    GroupId = currentGroupId, 
                    Rewards = new List<RewardItem>() 
                });
            }

            // 2. 리스트에 아이템 추가
            groupMap[currentGroupId].Rewards.Add(new RewardItem {
                Id = _itemId,
                Amount = _amount,
                DropRate = _rate
            });
        }

        return groupMap.Values.ToList();
    }


    // =================================================================================
    // 기능 2: 사용 규칙 데이터 변환 (Usage Rules CSV -> JSON)
    // CSV 구조: UseItemId | RewardType | TargetGroupId
    // =================================================================================
    [MenuItem("Tools/Data/2. Convert Usage Rules (CSV to JSON)")]
    public static void ConvertUsageRules()
    {
        string csvPath = EditorUtility.OpenFilePanel("Select UsageRule CSV", "", "csv");
        if (string.IsNullOrEmpty(csvPath)) return;

        try
        {
            string csvContent = File.ReadAllText(csvPath, Encoding.Default);
            List<UsageRule> rules = ParseUsageRules(csvContent);

            SaveJson(rules, "ItemUsageData");
            Debug.Log($"[사용 규칙 변환 완료] 총 {rules.Count}개의 규칙 생성됨.");
        }
        catch (Exception e)
        {
            Debug.LogError($"오류 발생: {e.Message}");
        }
    }

    private static List<UsageRule> ParseUsageRules(string content)
    {
        var rules = new List<UsageRule>();
        string[] lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i];
            if (string.IsNullOrWhiteSpace(line)) continue;
            string[] cols = line.Split(',');
            if (cols.Length < 3) continue;

            // 컬럼 매핑: 0:UseItemId, 1:Type, 2:TargetGroupId
            string _useItemId = cols[0].Trim();
            string _typeStr = cols[1].Trim();
            string _targetGroupId = cols[2].Trim();

            if (string.IsNullOrEmpty(_useItemId)) continue;

            // Enum 파싱
            if (!Enum.TryParse(_typeStr, out RewardType _type))
                _type = RewardType.SelectType;

            rules.Add(new UsageRule {
                Id = _useItemId,
                RewardType = _type,
                TargetGroupId = _targetGroupId
            });
        }
        return rules;
    }

    // [공통] JSON 저장 함수
    private static void SaveJson(object data, string defaultName)
    {
        var setting = new JsonSerializerSettings();
        setting.Formatting = Formatting.Indented;
        setting.Converters.Add(new StringEnumConverter());

        string jsonResult = JsonConvert.SerializeObject(data, setting);
        string savePath = EditorUtility.SaveFilePanel("Save JSON", "Assets/Resources/Data", defaultName, "json");

        if (!string.IsNullOrEmpty(savePath))
        {
            File.WriteAllText(savePath, jsonResult);
            AssetDatabase.Refresh();
        }
    }
    */
}