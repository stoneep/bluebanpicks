using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

/// <summary>
/// 사용자의 인벤토리 보유량 데이터 (String ID 기반)
/// </summary>
[System.Serializable]
public class UserInventoryData
{
    /// <summary>
    /// 아이템 ID(string)와 보유량 매핑
    /// </summary>
    public Dictionary<string, int> items = new Dictionary<string, int>();
}

/// <summary>
/// 사용자 데이터 (String ID와 보유량 저장)
/// </summary>
[System.Serializable]
public class UserData
{
    public UserInventoryData inventory = new UserInventoryData();

    // ============================================
    // 아이템 관리
    // ============================================
    
    /// <summary>
    /// 아이템 추가
    /// </summary>
    public void AddItem(string itemId, int amount)
    {
        if (string.IsNullOrEmpty(itemId) || amount <= 0) return;

        if (inventory.items.ContainsKey(itemId))
            inventory.items[itemId] += amount;
        else
            inventory.items[itemId] = amount;
    }

    /// <summary>
    /// 아이템 제거
    /// </summary>
    public bool RemoveItem(string itemId, int amount)
    {
        if (string.IsNullOrEmpty(itemId) || amount <= 0) return false;
        
        if (!inventory.items.ContainsKey(itemId) || inventory.items[itemId] < amount)
            return false;

        inventory.items[itemId] -= amount;
        
        // 0개가 되면 Dictionary에서 제거
        if (inventory.items[itemId] <= 0)
            inventory.items.Remove(itemId);
            
        return true;
    }

    /// <summary>
    /// 아이템 보유량 설정
    /// </summary>
    public void SetItemAmount(string itemId, int amount)
    {
        if (string.IsNullOrEmpty(itemId)) return;
        
        if (amount <= 0)
        {
            inventory.items.Remove(itemId);
            return;
        }

        inventory.items[itemId] = amount;
    }

    /// <summary>
    /// 아이템 보유량 조회
    /// </summary>
    public int GetItemAmount(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return 0;
        return inventory.items.ContainsKey(itemId) ? inventory.items[itemId] : 0;
    }

    /// <summary>
    /// 아이템 보유 여부 확인
    /// </summary>
    public bool HasItem(string itemId, int requiredAmount = 1)
    {
        return GetItemAmount(itemId) >= requiredAmount;
    }

    // ============================================
    // 전체 데이터 조회
    // ============================================
    
    /// <summary>
    /// 전체 아이템 목록 복사본 반환
    /// </summary>
    public Dictionary<string, int> GetAllItems()
    {
        return new Dictionary<string, int>(inventory.items);
    }

    /// <summary>
    /// 전체 데이터 초기화
    /// </summary>
    public void Clear()
    {
        inventory.items.Clear();
    }
    
    
    // ============================================
    // 트랜잭션 지원
    // ============================================
    
    /// <summary>
    /// 현재 상태의 스냅샷 생성 (트랜잭션용)
    /// </summary>
    public UserDataSnapshot CreateSnapshot()
    {
        return new UserDataSnapshot
        {
            inventoryItems = new Dictionary<string, int>(inventory.items)
        };
    }

    /// <summary>
    /// 스냅샷으로부터 상태 복원
    /// </summary>
    public void RestoreFromSnapshot(UserDataSnapshot snapshot)
    {
        if (snapshot == null)
        {
            Debug.LogError("[UserData] 복원할 스냅샷이 null입니다.");
            return;
        }

        inventory.items.Clear();
        foreach (var item in snapshot.inventoryItems)
        {
            inventory.items[item.Key] = item.Value;
        }
    }
}

/// <summary>
/// 트랜잭션 롤백을 위한 UserData 스냅샷
/// </summary>
[System.Serializable]
public class UserDataSnapshot
{
    public Dictionary<string, int> inventoryItems;
}
