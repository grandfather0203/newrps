using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Extensions;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class GameDataInit : MonoBehaviour
{
    [Header("Managers")]
    public InventoryManager inventoryManager;
    public MoneyManager moneyManager;
    public HomeManager homeManager;

    private string userId;

    void Start()
    {
        userId = FirebaseAuth.DefaultInstance.CurrentUser?.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            Debug.LogError("GameDataInit Start: 로그인이 필요함");
        }
    }

    public async void ResetAllData()
    {
        if (string.IsNullOrEmpty(userId))
        {
            Debug.LogError("ResetAllData 실패: 로그인이 필요함");
            return;
        }

        Debug.Log("ResetAllData 시작: 인벤토리, 돈, 씬 데이터 초기화");

        // 로컬 상태 초기화
        if (inventoryManager != null)
        {
            inventoryManager.GetInventory().Clear();
            Debug.Log("로컬 인벤토리 초기화: inventory 크기 = 0");
        }
        else
        {
            Debug.LogError("inventoryManager null");
        }

        if (moneyManager != null)
        {
            moneyManager.SetMoney(0);
            Debug.Log("로컬 돈 초기화: money = 0");
        }
        else
        {
            Debug.LogError("moneyManager null");
        }

        if (homeManager != null)
        {
            foreach (var furniture in FindObjectsOfType<Furniture>())
            {
                Destroy(furniture.gameObject);
            }
            Debug.Log("로컬 씬 초기화: 모든 Furniture 오브젝트 삭제");
        }
        else
        {
            Debug.LogError("homeManager null");
        }

        // Firebase 초기화
        FirebaseFirestore db = FirebaseFirestore.DefaultInstance;
        QuerySnapshot snapshot = null;

        try
        {
            snapshot = await db.Collection("couples").WhereEqualTo("User1", userId).GetSnapshotAsync();
        }
        catch (System.Exception e)
        {
            Debug.LogError("커플 조회 실패 (User1): " + e.Message);
        }

        if (snapshot == null || snapshot.Count == 0)
        {
            try
            {
                snapshot = await db.Collection("couples").WhereEqualTo("User2", userId).GetSnapshotAsync();
            }
            catch (System.Exception e)
            {
                Debug.LogError("커플 조회 실패 (User2): " + e.Message);
                return;
            }
        }

        if (snapshot == null || snapshot.Count == 0)
        {
            Debug.LogError("커플 문서 없음: 초기화 실패");
            return;
        }

        var coupleDoc = snapshot.Documents.FirstOrDefault();
        if (coupleDoc == null)
        {
            Debug.LogError("커플 문서가 없습니다.");
            return;
        }

        string coupleId = coupleDoc.Id;
        Debug.Log($"초기화 대상 coupleId = {coupleId}");

        // Firebase 데이터 초기화
        Dictionary<string, object> resetData = new Dictionary<string, object>
        {
            { "inventory", new List<string>() },
            { "money", 0 },
            { "SharedSceneJson", JsonUtility.ToJson(new SceneData()) }
        };

        try
        {
            await db.Collection("couples").Document(coupleId).UpdateAsync(resetData);
            Debug.Log("Firebase 데이터 초기화 완료: inventory = [], money = 0, SharedSceneJson = {}");
        }
        catch (System.Exception e)
        {
            Debug.LogError("Firebase 데이터 초기화 실패: " + e.Message);
        }

        Debug.Log("ResetAllData 완료");
    }
}