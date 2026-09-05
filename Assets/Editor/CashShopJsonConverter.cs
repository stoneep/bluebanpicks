using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

public class CashShopJsonConverter : EditorWindow
{
    /*
    [MenuItem("Tools/Data/Convert CashShop CSV to JSON")]
    public static void ConvertCsvToJson()
    {
        // 1. CSV File Open
        string csvPath = EditorUtility.OpenFilePanel("Select Item CSV", "", "csv");
        
        // Escape
        if (string.IsNullOrEmpty(csvPath)) return;

        // 한글 깨짐 방지를 위해 Encoding.Default 사용
        string[] lines = File.ReadAllLines(csvPath, Encoding.Default);
        if (lines.Length <= 1) return;
        
        try
        {
            // 2. CSV 파일 내용 읽기
            string csvContent = File.ReadAllText(csvPath);

            // 3. 파싱 로직 수행 (CSV 문자열 -> DTO 객체)
            CashShopDataDTO dataDto = ParseCsvContent(csvContent);

            if (dataDto == null || dataDto.products.Count == 0)
            {
                Debug.LogWarning("파싱된 데이터가 없습니다. CSV 형식을 확인해주세요.");
                return;
            }

            // 4. JSON으로 변환 (Newtonsoft.Json 사용)
            var settings = new JsonSerializerSettings();
            settings.Formatting = Formatting.Indented; // 들여쓰기
            settings.Converters.Add(new StringEnumConverter()); // Enum을 문자열("Hot")로 저장

            string jsonResult = JsonConvert.SerializeObject(dataDto, settings);

            // 5. 저장할 경로 선택 창 열기
            string savePath = EditorUtility.SaveFilePanel("Save CashShopProducts JSON", "Assets/Resources/Data", "CashShopProducts", "json");

            if (string.IsNullOrEmpty(savePath)) return;

            // 6. 파일 쓰기
            File.WriteAllText(savePath, jsonResult);
            
            // 7. 유니티 에디터 새로고침 (파일 생성 즉시 반영)
            AssetDatabase.Refresh();

            Debug.Log($"변환 성공! 파일이 저장되었습니다: {savePath}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"변환 중 오류 발생: {ex.Message}");
        }
    }

    // --- [내부 파싱 로직] ---
    private static CashShopDataDTO ParseCsvContent(string content)
    {
        CashShopDataDTO dto = new CashShopDataDTO();
        dto.products = new List<CashShopProduct>();

        // 줄바꿈으로 행 분리
        string[] lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        // 첫 줄(헤더) 건너뛰고 1부터 시작
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i];
            if (string.IsNullOrWhiteSpace(line)) continue;

            // 쉼표로 분리 (주의: 데이터 내용 안에 쉼표가 있으면 안 됨)
            string[] cols = line.Split(',');

            // 최소 컬럼 수 확인 (여기선 10개)
            if (cols.Length < 10) continue;

            try
            {
                CashShopProduct product = new CashShopProduct();

                // CSV 순서: ShopCategory, ProductId, ProductName, Price, Rewards, Icon, Desc, Tag, IsLimited, EndDate, Limit
                if (Enum.TryParse(cols[0].Trim(), out CashShopList parsedCategory))
                {
                    // 성공 시: 파싱된 값을 할당
                    product.ShopCategory = parsedCategory;
                }
                else
                {
                    // 실패 시: 기본값 설정 또는 에러 로그
                    // 방법 1: 가장 기본적인 값(예: 0번 인덱스)으로 설정
                    product.ShopCategory = CashShopList.SpecialPackage;
                    
                    // 방법 3: 로그를 남겨서 데이터 오류 확인
                    Debug.LogError($"잘못된 카테고리 값입니다: {cols[0]}");
                }

                product.ProductId = cols[1].Trim();
                product.ProductName = cols[2].Trim();
                product.Price = int.TryParse(cols[3], out int p) ? p : 0;
                
                // Rewards 파싱 (id:qty|id:qty)
                product.Rewards = ParseRewards(cols[4]);

                product.IconAddress = cols[5].Trim();
                product.description = cols[6].Trim();

                // Enum 파싱 (String -> Enum)
                if (Enum.TryParse(cols[7].Trim(), out MarketingTag tag))
                    product.TagType = tag;
                else
                    product.TagType = MarketingTag.None;

                // Boolean 파싱
                string boolStr = cols[8].Trim().ToLower();
                product.isLimited = (boolStr == "true" || boolStr == "1");

                product.endDate = cols[9].Trim();
                product.purchaseLimit = int.TryParse(cols[10], out int limit) ? limit : 0;
                product.isFeatured = (cols[11].Trim() == "1");
                product.displayOrder = int.TryParse(cols[12].Trim(), out int d) ? d : 0;
                dto.products.Add(product);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Line {i}] 파싱 에러: {ex.Message}");
            }
        }

        return dto;
    }

    private static List<PackageReward> ParseRewards(string raw)
    {
        List<PackageReward> list = new List<PackageReward>();
        if (string.IsNullOrWhiteSpace(raw)) return list;

        string[] items = raw.Split('|');
        foreach (var item in items)
        {
            string[] parts = item.Split(':');
            if (parts.Length == 2)
            {
                list.Add(new PackageReward
                {
                    ItemId = parts[0].Trim(),
                    quantity = int.TryParse(parts[1], out int q) ? q : 0
                });
            }
        }
        return list;
    }
    */
}