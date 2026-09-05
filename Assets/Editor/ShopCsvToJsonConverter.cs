using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text;
using Newtonsoft.Json;
using System;

public class ShopCsvToJsonConverter : EditorWindow
{
    /*
    [MenuItem("Tools/Data/Convert Shop CSV to JSON")]
    public static void ConvertShopCsv()
    {
        string csvPath = EditorUtility.OpenFilePanel("Select Shop CSV", "", "csv");
        if (string.IsNullOrEmpty(csvPath)) return;

        // 한글 깨짐 방지를 위해 Encoding.Default 사용
        string[] lines = File.ReadAllLines(csvPath, Encoding.Default);
        if (lines.Length <= 1) return;

        string[] headers = SplitCsvLine(lines[0]);
        Dictionary<string, int> h = new Dictionary<string, int>();
        for (int i = 0; i < headers.Length; i++) h[headers[i].Trim()] = i;

        List<ShopProduct> productList = new List<ShopProduct>();

        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i];
            if (string.IsNullOrWhiteSpace(line)) continue;
            string[] values = SplitCsvLine(line);

            try
            {
                ShopProduct product = new ShopProduct();

                // ShopType (Enum)
                if (Enum.TryParse(GetVal(values, h, "ShopType"), out ShopSaleList sType)) 
                    product.ShopType = sType;

                product.ItemId = GetVal(values, h, "ItemId");
                
                // ResetType (Enum)
                if (Enum.TryParse(GetVal(values, h, "ResetType"), out ShopResetType rType)) 
                    product.ResetType = rType;
                // ⭐ [추가] 수량 파싱 (CSV 헤더가 "Amount"라고 가정)
                // 값이 없거나 0이면 기본값 1을 유지하도록 로직 작성
                int quantity = 0;
                if (int.TryParse(GetVal(values, h, "Amount"), out quantity) && quantity > 0)
                {
                    product.ProductQuantity = quantity;
                }
                else
                {
                    product.ProductQuantity = 1; // 기본값
                }
                int.TryParse(GetVal(values, h, "Limit"), out product.BuyLimit);
                int.TryParse(GetVal(values, h, "Sort"), out product.SortOrder);

                // ⭐ [추가] 할인율과 태그 파싱
                int.TryParse(GetVal(values, h, "Discount"), out product.DiscountRate);
                if (Enum.TryParse(GetVal(values, h, "Tag"), out MarketingTag tag))
                {
                    product.TagType = tag;
                }
                
                // ⭐ 핵심 수정: Currency 열의 값을 string ID로 바로 사용함
                // ⭐ 핵심 수정: Currency 열의 값을 가져올 때 매핑 함수를 통과시킴
                // ✅ Currency를 변환하지 않고 원본 별칭 그대로 저장
                product.CurrencyAlias = GetVal(values, h, "Currency");
    
                // ✅ PriceRules 파싱 (별칭 그대로 전달)
                string rulesStr = GetVal(values, h, "PriceRules");
                product.PriceRules = ParsePriceRules(rulesStr, product.CurrencyAlias);
    
                productList.Add(product);
            }
            catch (Exception e)
            {
                Debug.LogError($"CSV 파싱 에러 (줄 {i + 1}): {e.Message}");
            }
        }

        // 5. JSON 저장
        string json = JsonConvert.SerializeObject(productList, Formatting.Indented);
        string savePath = EditorUtility.SaveFilePanel("Save ShopData JSON", "Assets/Resources/Data", "ShopData", "json");
        
        if (!string.IsNullOrEmpty(savePath))
        {
            File.WriteAllLines(savePath, new [] { json }, Encoding.UTF8);
            AssetDatabase.Refresh();
            Debug.Log($"✅ 상점 데이터 변환 완료! ({productList.Count}개)");
        }
    }

    // "3:5000|7:8000" 또는 "3:Gold:5000" 형태 대응
    private static List<PriceRule> ParsePriceRules(string rawRule, string defaultCurrencyAlias)
    {
        List<PriceRule> rules = new List<PriceRule>();
        if (string.IsNullOrWhiteSpace(rawRule)) return rules;

        string[] steps = rawRule.Split('|');
        foreach (string step in steps)
        {
            string[] parts = step.Trim().Split(':');
            if (parts.Length < 2) continue;

            PriceRule rule = new PriceRule();
            int.TryParse(parts[0], out rule.Count);

            string currencyAlias = defaultCurrencyAlias;
            long amount = 0;

            if (parts.Length == 3)
            {
                // ✅ 별칭 그대로 사용 (변환하지 않음!)
                currencyAlias = parts[1].Trim();
                long.TryParse(parts[2], out amount);
            }
            else
            {
                long.TryParse(parts[1], out amount);
            }

            // ✅ 별칭을 그대로 CurrencyId에 저장
            rule.Price = new Price { CurrencyId = currencyAlias, Amount = amount };
            rules.Add(rule);
        }
        return rules;
    }
    
    private static string GetVal(string[] values, Dictionary<string, int> map, string col)
    {
        return (map.TryGetValue(col, out int i) && i < values.Length) ? values[i] : "";
    }
    
    // ========================================================================
    // 2. ⭐ [신규] ShopGroupData 변환 (로테이션 알맹이)
    // ========================================================================
    [MenuItem("Tools/Data/Convert ShopGroup CSV to JSON")]
    public static void ConvertShopGroupCsv()
    {
        string csvPath = EditorUtility.OpenFilePanel("Select ShopGroup CSV", "", "csv");
        if (string.IsNullOrEmpty(csvPath)) return;

        string[] lines = File.ReadAllLines(csvPath, Encoding.Default);
        if (lines.Length <= 1) return;

        // 헤더 파싱
        string[] headers = SplitCsvLine(lines[0]);
        Dictionary<string, int> h = new Dictionary<string, int>();
        for (int i = 0; i < headers.Length; i++) h[headers[i].Trim()] = i;

        List<ShopGroupEntry> groupList = new List<ShopGroupEntry>();

        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i];
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;
            
            string[] values = SplitCsvLine(line);

            try
            {
                ShopGroupEntry entry = new ShopGroupEntry();

                // CSV 컬럼 매핑 (엑셀 헤더 이름과 맞춰주세요)
                entry.GroupId = GetVal(values, h, "GroupId");
                entry.TargetItemId = GetVal(values, h, "TargetItemId");
                
                // 수량, 제한, 가중치 파싱
                int.TryParse(GetVal(values, h, "Quantity"), out entry.TargetItemQuantity);
                int.TryParse(GetVal(values, h, "Limit"), out entry.PurchaseLimit);
                int.TryParse(GetVal(values, h, "Weight"), out entry.Weight);

                // 가격 규칙 (문자열 그대로 저장 -> 나중에 Helper가 파싱)
                entry.PriceRulesStr = GetVal(values, h, "PriceRules");

                groupList.Add(entry);
            }
            catch (Exception e)
            {
                Debug.LogError($"Group CSV 파싱 에러 ({i + 1}줄): {e.Message}");
            }
        }

        // JSON 저장
        string json = JsonConvert.SerializeObject(groupList, Formatting.Indented);
        string savePath = EditorUtility.SaveFilePanel("Save ShopGroupData JSON", "Assets/Resources/Data", "ShopGroupData", "json");
        
        if (!string.IsNullOrEmpty(savePath))
        {
            File.WriteAllLines(savePath, new [] { json }, Encoding.UTF8);
            AssetDatabase.Refresh();
            Debug.Log($"✅ ShopGroupData 변환 완료! ({groupList.Count}개 항목)");
        }
    }

    // --- 헬퍼 함수들 (기존과 공유) ---

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
