using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Extensions;
using UnityEngine;
using System.Linq;

public class MoneyManager : MonoBehaviour
{
    private int money;
    private string userId;

    void Start()
    {
        userId = FirebaseAuth.DefaultInstance.CurrentUser.UserId;
        LoadMoney();
    }

    public void SpendMoney(int spendAmount)
    {
        if (money >= spendAmount)
        {
            money -= spendAmount;
            SaveMoney();
        }
        else
        {
            Debug.Log("돈 부족: 현재 금액 " + money + ", 필요 금액 " + spendAmount);
        }
    }

    public void AddMoney(int addAmount)
    {
        money += addAmount;
        SaveMoney();
    }

    public int GetMoney()
    {
        return money;
    }

    private void LoadMoney()
    {
        FirebaseAuth auth = FirebaseAuth.DefaultInstance;
        if (auth.CurrentUser == null)
        {
            Debug.LogError("돈 로드 실패: 로그인이 필요함");
            return;
        }

        FirebaseFirestore db = FirebaseFirestore.DefaultInstance;
        db.Collection("couples").WhereEqualTo("User1", userId).GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (!task.IsCompletedSuccessfully)
            {
                Debug.LogError("커플 조회 실패: " + task.Exception);
                return;
            }

            var snapshot = task.Result;
            if (snapshot.Count == 0)
            {
                db.Collection("couples").WhereEqualTo("User2", userId).GetSnapshotAsync().ContinueWithOnMainThread(task2 =>
                {
                    if (task2.IsCompletedSuccessfully)
                    {
                        LoadMoneyJson(task2.Result);
                    }
                    else
                    {
                        Debug.LogError("커플 조회 실패: " + task2.Exception);
                    }
                });
            }
            else
            {
                LoadMoneyJson(snapshot);
            }
        });
    }

    private void LoadMoneyJson(QuerySnapshot snapshot)
    {
        if (snapshot.Count == 0)
        {
            Debug.Log("커플 연결되지 않음. 기본 금액 로드.");
            money = 0;
            return;
        }

        var coupleDoc = snapshot.Documents.FirstOrDefault();
        if (coupleDoc == null)
        {
            Debug.LogError("커플 문서가 없습니다.");
            return;
        }

        money = coupleDoc.GetValue<int>("money", 0); // 기본값 0
        Debug.Log("돈 로드 완료: " + money);
    }

    private void SaveMoney()
    {
        FirebaseAuth auth = FirebaseAuth.DefaultInstance;
        if (auth.CurrentUser == null)
        {
            Debug.LogError("돈 저장 실패: 로그인이 필요함");
            return;
        }

        FirebaseFirestore db = FirebaseFirestore.DefaultInstance;
        db.Collection("couples").WhereEqualTo("User1", userId).GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (!task.IsCompletedSuccessfully)
            {
                Debug.LogError("커플 조회 실패: " + task.Exception);
                return;
            }

            var snapshot = task.Result;
            if (snapshot.Count == 0)
            {
                db.Collection("couples").WhereEqualTo("User2", userId).GetSnapshotAsync().ContinueWithOnMainThread(task2 =>
                {
                    if (task2.IsCompletedSuccessfully)
                    {
                        SaveMoneyJson(task2.Result);
                    }
                    else
                    {
                        Debug.LogError("커플 조회 실패: " + task2.Exception);
                    }
                });
            }
            else
            {
                SaveMoneyJson(snapshot);
            }
        });
    }

    private void SaveMoneyJson(QuerySnapshot snapshot)
    {
        if (snapshot.Count == 0)
        {
            Debug.LogError("커플 연결되지 않음. 돈 저장 실패.");
            return;
        }

        var coupleDoc = snapshot.Documents.FirstOrDefault();
        if (coupleDoc == null)
        {
            Debug.LogError("커플 문서가 없습니다.");
            return;
        }

        string coupleId = coupleDoc.Id;
        FirebaseFirestore db = FirebaseFirestore.DefaultInstance;
        db.Collection("couples").Document(coupleId).UpdateAsync("money", money).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompletedSuccessfully)
            {
                Debug.Log("돈 저장 완료: " + money);
            }
            else
            {
                Debug.LogError("돈 저장 실패: " + task.Exception);
            }
        });
    }
    public void SetMoney(int money)
    {
        money = this.money;
    }
}