using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Extensions;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class InventoryManager : MonoBehaviour
{
    [Header("Initial Inventory List (Inspector)")]
    public string[] initialInventory; // Inspector에서 objectId 드래그 (e.g., "bathtub_01")

    private List<string> inventory = new List<string>();
    private string userId;

    void Start()
    {
        userId = FirebaseAuth.DefaultInstance.CurrentUser?.UserId;
        Debug.Log($"InventoryManager Start: userId = {userId}");
        if (string.IsNullOrEmpty(userId)) {
            Debug.LogError("인벤토리 로드 실패: 로그인이 필요함");
            return;
        }

        // Inspector 리스트로 초기화
        if (initialInventory != null && initialInventory.Length > 0) {
            inventory.AddRange(initialInventory);
            Debug.Log($"Inspector initialInventory 로드: 크기 = {initialInventory.Length}, 내용: [{string.Join(", ", initialInventory)}]");
        } else {
            Debug.LogWarning("Inspector initialInventory 비어 있음 – Firebase 로드만 사용");
        }

        LoadInventory();
    }

    public void AddToInventory(string itemId)
    {
        Debug.Log($"AddToInventory: {itemId}, 현재 inventory 크기: {inventory.Count}");
        inventory.Add(itemId);
        SaveInventory();
    }

    public void RemoveFromInventory(string itemId)
    {
        Debug.Log($"RemoveFromInventory: {itemId}, 현재 inventory 크기: {inventory.Count}");
        inventory.Remove(itemId);
        SaveInventory();
    }

    public void RemoveFromInventoryAt(int index)
    {
        if (index >= 0 && index < inventory.Count)
        {
            Debug.Log($"RemoveFromInventoryAt: index {index}, item {inventory[index]}, 현재 크기: {inventory.Count}");
            inventory.RemoveAt(index);
            SaveInventory();
        } else {
            Debug.LogWarning($"RemoveFromInventoryAt 실패: index {index} 유효 범위 초과, inventory 크기: {inventory.Count}");
        }
    }

    public List<string> GetInventory()
    {
        Debug.Log($"GetInventory 호출: inventory 크기 = {inventory.Count}, 내용: [{string.Join(", ", inventory)}]");
        return inventory;
    }

    private void LoadInventory()
    {
        FirebaseAuth auth = FirebaseAuth.DefaultInstance;
        if (auth.CurrentUser == null)
        {
            Debug.LogError("인벤토리 로드 실패: 로그인이 필요함");
            return;
        }

        Debug.Log("LoadInventory 시작: User1 쿼리 실행");
        FirebaseFirestore db = FirebaseFirestore.DefaultInstance;
        db.Collection("couples").WhereEqualTo("User1", userId).GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (!task.IsCompletedSuccessfully)
            {
                Debug.LogError("커플 조회 실패 (User1): " + task.Exception);
                return;
            }

            var snapshot = task.Result;
            Debug.Log($"User1 쿼리 결과: snapshot.Count = {snapshot.Count}");
            if (snapshot.Count == 0)
            {
                Debug.Log("User1 없음, User2 쿼리 실행");
                db.Collection("couples").WhereEqualTo("User2", userId).GetSnapshotAsync().ContinueWithOnMainThread(task2 =>
                {
                    if (task2.IsCompletedSuccessfully)
                    {
                        Debug.Log($"User2 쿼리 결과: snapshot.Count = {task2.Result.Count}");
                        LoadInventoryJson(task2.Result);
                    }
                    else
                    {
                        Debug.LogError("커플 조회 실패 (User2): " + task2.Exception);
                    }
                });
            }
            else
            {
                LoadInventoryJson(snapshot);
            }
        });
    }

    private void LoadInventoryJson(QuerySnapshot snapshot)
    {
        Debug.Log($"LoadInventoryJson 호출: snapshot.Count = {snapshot.Count}");
        if (snapshot.Count == 0)
        {
            Debug.Log("커플 연결되지 않음. 기본 인벤토리 로드 (Inspector 리스트 유지).");
            return; 
        }

        var coupleDoc = snapshot.Documents.FirstOrDefault();
        if (coupleDoc == null)
        {
            Debug.LogError("커플 문서가 없습니다. Inspector 리스트 유지.");
            return;
        }

        Debug.Log($"coupleDoc.Id = {coupleDoc.Id}");
        var loadedInventory = coupleDoc.GetValue<List<string>>("inventory");
        Debug.Log($"Firestore에서 inventory 로드: {(loadedInventory != null ? $"크기 {loadedInventory.Count}, 내용 [{string.Join(", ", loadedInventory)}]" : "null (Inspector 리스트 유지)")}");
        inventory = loadedInventory ?? inventory; 
        Debug.Log("인벤토리 로드 완료: 최종 크기 = " + inventory.Count);
    }

    private void SaveInventory()
    {
        FirebaseAuth auth = FirebaseAuth.DefaultInstance;
        if (auth.CurrentUser == null)
        {
            Debug.LogError("인벤토리 저장 실패: 로그인이 필요함");
            return;
        }

        Debug.Log($"SaveInventory 시작: 저장할 inventory 크기 = {inventory.Count}, 내용: [{string.Join(", ", inventory)}]");
        FirebaseFirestore db = FirebaseFirestore.DefaultInstance;
        db.Collection("couples").WhereEqualTo("User1", userId).GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (!task.IsCompletedSuccessfully)
            {
                Debug.LogError("커플 조회 실패 (User1): " + task.Exception);
                return;
            }

            var snapshot = task.Result;
            Debug.Log($"User1 쿼리 저장 결과: snapshot.Count = {snapshot.Count}");
            if (snapshot.Count == 0)
            {
                Debug.Log("User1 없음, User2 쿼리 저장 실행");
                db.Collection("couples").WhereEqualTo("User2", userId).GetSnapshotAsync().ContinueWithOnMainThread(task2 =>
                {
                    if (task2.IsCompletedSuccessfully)
                    {
                        Debug.Log($"User2 쿼리 저장 결과: snapshot.Count = {task2.Result.Count}");
                        SaveInventoryJson(task2.Result);
                    }
                    else
                    {
                        Debug.LogError("커플 조회 실패 (User2): " + task2.Exception);
                    }
                });
            }
            else
            {
                SaveInventoryJson(snapshot);
            }
        });
    }

    private void SaveInventoryJson(QuerySnapshot snapshot)
    {
        Debug.Log($"SaveInventoryJson 호출: snapshot.Count = {snapshot.Count}");
        if (snapshot.Count == 0)
        {
            Debug.LogError("커플 연결되지 않음. 인벤토리 저장 실패.");
            return;
        }

        var coupleDoc = snapshot.Documents.FirstOrDefault();
        if (coupleDoc == null)
        {
            Debug.LogError("커플 문서가 없습니다.");
            return;
        }

        string coupleId = coupleDoc.Id;
        Debug.Log($"저장 대상 coupleId = {coupleId}, inventory = [{string.Join(", ", inventory)}]");
        FirebaseFirestore db = FirebaseFirestore.DefaultInstance;
        db.Collection("couples").Document(coupleId).UpdateAsync("inventory", inventory).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompletedSuccessfully)
            {
                Debug.Log("인벤토리 저장 완료");
            }
            else
            {
                Debug.LogError("인벤토리 저장 실패: " + task.Exception);
            }
        });
    }
}