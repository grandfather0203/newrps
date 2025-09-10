using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Extensions;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Linq;
using System.Collections.Generic;

public class HomeManager : MonoBehaviour {
    public GridRenderer gridRenderer;
    public Canvas decoCanvas;
    public Button decorModeButton;
    public Button doneButton;
    public Button inventoryButton;
    public GameObject inventoryPanel;
    public Transform inventoryPanelContent;
    public GameObject inventoryButtonPrefab;
    public GameObject notificationText;
    public Button saveButton;
    public Button confirmButton;
    public Button leftRotateButton;
    public Button rightRotateButton;
    public Button bottomRotateButton;
    public Button defaultButton;
    public Button shopButton;
    public ShopManager shopManager;
    public InventoryManager inventoryManager;
    public MoneyManager moneyManager;
    private bool isDecorMode = false;
    private Furniture selectedFurniture;
    private CameraController cameraController;
    private Queue<Button> inventoryButtonPool = new Queue<Button>();

    void Start() {
        decorModeButton.onClick.AddListener(ToggleDecorMode);
        doneButton.onClick.AddListener(ToggleDecorMode);
        inventoryButton.onClick.AddListener(ToggleInventoryPanel);
        saveButton.onClick.AddListener(SaveScene);
        // leftRotateButton.onClick.AddListener(() => cameraController.RotateLeft());
        // rightRotateButton.onClick.AddListener(() => cameraController.RotateRight());
        // bottomRotateButton.onClick.AddListener(() => cameraController.RotateBottom());
        // defaultButton.onClick.AddListener(() => cameraController.ResetToDefault());
        shopButton.onClick.AddListener(() => shopManager.ToggleShopPanel());
        decoCanvas.gameObject.SetActive(false);
        notificationText.SetActive(false);
        confirmButton.gameObject.SetActive(false);
        cameraController = Camera.main.GetComponent<CameraController>();
        
        for (int i = 0; i < 50; i++)
        {
            GameObject btnObj = Instantiate(inventoryButtonPrefab, inventoryPanelContent);
            btnObj.SetActive(false);
            inventoryButtonPool.Enqueue(btnObj.GetComponent<Button>());
        }

        LoadScene();
    }

    void Update() {
        if (isDecorMode && Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began) {
            if (selectedFurniture != null) return;

            Ray ray = Camera.main.ScreenPointToRay(Input.GetTouch(0).position);
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, LayerMask.GetMask("Furniture"))) {
                Furniture furniture = hit.transform.GetComponent<Furniture>();
                if (furniture != null) {
                    SelectFurniture(furniture);
                }
            }
        }
    }

    void ToggleDecorMode() {
        isDecorMode = !isDecorMode;
        gridRenderer.gameObject.SetActive(isDecorMode);
        decorModeButton.gameObject.SetActive(!isDecorMode);
        decoCanvas.gameObject.SetActive(isDecorMode);
        DeselectFurniture();

        Animator inventoryButtonAnimator = inventoryButton.GetComponent<Animator>();
        if (inventoryButtonAnimator != null) {
            inventoryButtonAnimator.SetBool("isEnabled", !isDecorMode);
        }

        if (isDecorMode) {
            gridRenderer.FadeIn(1f);
            ShowNotification("꾸미기 모드 시작", 2f);
        } else {
            gridRenderer.FadeOut(1f);
            ShowNotification("꾸미기 모드 종료", 2f);
        }
    }

    void ToggleInventoryPanel() {
        Animator inventoryButtonAnimator = inventoryButton.GetComponent<Animator>();
        if (inventoryButtonAnimator != null) {
            bool currentState = inventoryButtonAnimator.GetBool("isEnabled");
            inventoryButtonAnimator.SetBool("isEnabled", !currentState);
        }

        if (inventoryPanel.activeSelf) {
            Debug.Log("인벤토리 패널 열림, inventory.Count = " + inventoryManager.GetInventory().Count);
            foreach (Transform child in inventoryPanelContent) {
                if (child.gameObject.activeSelf) {
                    child.gameObject.SetActive(false);
                    inventoryButtonPool.Enqueue(child.GetComponent<Button>());
                }
            }

            var inventory = inventoryManager.GetInventory();
            Debug.Log($"인벤토리 내용: [{string.Join(", ", inventory)}]");
            for (int i = 0; i < inventory.Count; i++) {
                string objectId = inventory[i];
                // Resources.Load 대신 ShopManager.furniturePrefabs에서 검색
                GameObject prefab = shopManager.furniturePrefabs.FirstOrDefault(p => p.GetComponent<Furniture>()?.objectId == objectId);
                if (prefab == null) {
                    Debug.LogError($"프리팹 찾기 실패: objectId = {objectId}, ShopManager.furniturePrefabs에서 매칭 없음");
                    continue;
                }
                FurnitureData item = prefab.GetComponent<FurnitureData>();
                if (item == null) {
                    Debug.LogWarning($"FurnitureData 없음: {objectId}");
                    continue;
                }

                Button btn = inventoryButtonPool.Count > 0 ? inventoryButtonPool.Dequeue() : Instantiate(inventoryButtonPrefab, inventoryPanelContent).GetComponent<Button>();
                btn.gameObject.SetActive(true);
                Image furnitureImage = btn.transform.Find("FurnitureImage")?.GetComponent<Image>();
                if (furnitureImage != null && item.thumbnailImage != null) {
                    furnitureImage.sprite = item.thumbnailImage;
                    Debug.Log($"인벤토리 버튼 이미지 설정: {item.itemName}, sprite: {item.thumbnailImage.name}");
                } else {
                    Debug.LogWarning($"인벤토리 버튼 이미지 설정 실패: {item.itemName}, FurnitureImage {(furnitureImage == null ? "null" : "OK")}, thumbnailImage {(item.thumbnailImage == null ? "null" : "OK")}");
                }
                int index = i;
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => PlaceFromInventory(index));
            }

            Debug.Log("인벤토리 버튼 생성 완료, 총 " + inventory.Count + "개");
            LayoutRebuilder.ForceRebuildLayoutImmediate(inventoryPanelContent as RectTransform);
        }
    }

    void PlaceFromInventory(int inventoryIndex) {
        string objectId = inventoryManager.GetInventory()[inventoryIndex];
        GameObject prefab = shopManager.furniturePrefabs.FirstOrDefault(p => p.GetComponent<Furniture>()?.objectId == objectId);
        if (prefab == null) {
            Debug.LogError($"프리팹 찾기 실패: objectId = {objectId}, ShopManager.furniturePrefabs에서 매칭 없음");
            return;
        }

        if (!isDecorMode) ToggleDecorMode();
        GameObject obj = Instantiate(prefab, Vector3.zero, Quaternion.identity);
        Furniture furniture = obj.GetComponent<Furniture>();
        if (furniture != null) {
            SelectFurniture(furniture);
            inventoryManager.RemoveFromInventoryAt(inventoryIndex);
            SaveScene();
        } else {
            Debug.LogError($"Furniture 컴포넌트 없음: {objectId}");
            Destroy(obj);
        }
    }

    void SelectFurniture(Furniture furniture) {
        if (selectedFurniture != furniture) {
            DeselectFurniture();
            selectedFurniture = furniture;
            FurnitureDrag drag = furniture.GetComponent<FurnitureDrag>();
            if (drag == null) drag = furniture.gameObject.AddComponent<FurnitureDrag>();
            drag.enabled = true;
            ShowConfirmButton(furniture.furnitureType);
        }
    }

    void DeselectFurniture() {
        if (selectedFurniture != null) {
            FurnitureDrag drag = selectedFurniture.GetComponent<FurnitureDrag>();
            if (drag != null) {
                drag.OnPlaced();
                drag.enabled = false;
            }
            confirmButton.gameObject.SetActive(false);
            selectedFurniture = null;
        }
    }

    public void ShowConfirmButton(FurnitureType furnitureType) {
        confirmButton.gameObject.SetActive(true);
        StartCoroutine(FadeCanvasGroup(confirmButton.GetComponent<CanvasGroup>(), 1f, 0.3f));
        confirmButton.onClick.RemoveAllListeners();
        confirmButton.onClick.AddListener(() => {
            if (selectedFurniture != null) {
                FurnitureDrag drag = selectedFurniture.GetComponent<FurnitureDrag>();
                if (drag != null && drag.CanPlaceFurniture()) {
                    drag.OnPlaced();
                    SaveScene();
                    string message = furnitureType == FurnitureType.Floor ? "바닥에 배치됨" :
                                    furnitureType == FurnitureType.Wall ? "벽에 배치됨" :
                                    "선반 또는 바닥에 배치됨";
                    ShowNotification(message, 2f);
                    DeselectFurniture();
                } else {
                    ShowNotification("설치 불가능한 위치입니다.", 2f);
                }
            }
        });
        StartCoroutine(UpdateConfirmButtonInteractability());
    }

    private IEnumerator UpdateConfirmButtonInteractability() {
        while (selectedFurniture != null) {
            FurnitureDrag drag = selectedFurniture.GetComponent<FurnitureDrag>();
            confirmButton.interactable = drag != null && drag.CanPlaceFurniture();
            yield return null;
        }
    }

    public void ShowNotification(string message, float duration) {
        TMP_Text text = notificationText.GetComponent<TMP_Text>();
        text.text = message;
        CanvasGroup cg = notificationText.GetComponent<CanvasGroup>();
        cg.alpha = 0;
        notificationText.SetActive(true);
        StartCoroutine(FadeNotification(cg, duration));
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float targetAlpha, float duration, System.Action onComplete = null) {
        float startAlpha = cg.alpha;
        float time = 0;
        while (time < duration) {
            time += Time.deltaTime;
            cg.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / duration);
            yield return null;
        }
        cg.alpha = targetAlpha;
        onComplete?.Invoke();
    }

    private IEnumerator FadeNotification(CanvasGroup cg, float duration) {
        yield return StartCoroutine(FadeCanvasGroup(cg, 1f, 0.3f));
        yield return new WaitForSeconds(duration);
        yield return StartCoroutine(FadeCanvasGroup(cg, 0f, 0.3f));
        notificationText.SetActive(false);
    }

    void LoadScene() {
        FirebaseAuth auth = FirebaseAuth.DefaultInstance;
        if (auth.CurrentUser == null) {
            Debug.LogError("로드 실패: 로그인이 필요함");
            return;
        }

        string userId = auth.CurrentUser.UserId;
        FirebaseFirestore db = FirebaseFirestore.DefaultInstance;

        db.Collection("couples").WhereEqualTo("User1", userId).GetSnapshotAsync().ContinueWithOnMainThread(task => {
            if (!task.IsCompletedSuccessfully) {
                Debug.LogError("커플 조회 실패: " + task.Exception);
                return;
            }

            var snapshot = task.Result;
            if (snapshot.Count == 0) {
                db.Collection("couples").WhereEqualTo("User2", userId).GetSnapshotAsync().ContinueWithOnMainThread(task2 => {
                    if (task2.IsCompletedSuccessfully) {
                        LoadSceneJson(task2.Result);
                    } else {
                        Debug.LogError("커플 조회 실패: " + task2.Exception);
                    }
                });
            } else {
                LoadSceneJson(snapshot);
            }
        });
    }

    void LoadSceneJson(QuerySnapshot snapshot) {
        if (snapshot.Count == 0) {
            Debug.Log("커플 연결되지 않음. 기본 씬 로드.");
            return;
        }

        var coupleDoc = snapshot.Documents.FirstOrDefault();
        if (coupleDoc == null) {
            Debug.LogError("커플 문서가 없습니다.");
            return;
        }

        string json = coupleDoc.GetValue<string>("SharedSceneJson");
        if (string.IsNullOrEmpty(json)) {
            Debug.Log("공유된 씬 데이터 없음. 기본 씬 로드.");
            return;
        }

        SceneData scene = JsonUtility.FromJson<SceneData>(json);
        if (scene == null || scene.objects == null) {
            Debug.LogError("JSON 파싱 실패");
            return;
        }

        foreach (var furniture in FindObjectsOfType<Furniture>()) {
            Destroy(furniture.gameObject);
        }

        foreach (var objData in scene.objects) {
            // Resources.Load 대신 리스트에서 검색
            GameObject prefab = shopManager.furniturePrefabs.FirstOrDefault(p => p.GetComponent<Furniture>()?.objectId == objData.objectId);
            if (prefab == null) {
                Debug.LogError($"프리팹 로드 실패: {objData.objectId}, ShopManager.furniturePrefabs에서 매칭 없음");
                continue;
            }

            GameObject furnitureObj = Instantiate(prefab);
            Furniture furniture = furnitureObj.GetComponent<Furniture>();
            furniture.objectId = objData.objectId;
            furniture.SetFromObjectData(objData);
            furnitureObj.AddComponent<FurnitureDrag>().enabled = false;
        }
        Debug.Log("씬 로드 완료");
    }

    void SaveScene() {
        FirebaseAuth auth = FirebaseAuth.DefaultInstance;
        if (auth.CurrentUser == null) {
            Debug.LogError("저장 실패: 로그인이 필요함");
            return;
        }

        string userId = auth.CurrentUser.UserId;
        FirebaseFirestore db = FirebaseFirestore.DefaultInstance;

        db.Collection("couples").WhereEqualTo("User1", userId).GetSnapshotAsync().ContinueWithOnMainThread(task => {
            if (!task.IsCompletedSuccessfully) {
                Debug.LogError("커플 조회 실패: " + task.Exception);
                return;
            }

            var snapshot = task.Result;
            if (snapshot.Count == 0) {
                db.Collection("couples").WhereEqualTo("User2", userId).GetSnapshotAsync().ContinueWithOnMainThread(task2 => {
                    if (task2.IsCompletedSuccessfully) {
                        SaveSceneJson(task2.Result);
                    } else {
                        Debug.LogError("커플 조회 실패: " + task2.Exception);
                    }
                });
            } else {
                SaveSceneJson(snapshot);
            }
        });
    }

    void SaveSceneJson(QuerySnapshot snapshot) {
        if (snapshot.Count == 0) {
            Debug.LogError("커플 연결되지 않음. 저장 실패.");
            return;
        }

        var coupleDoc = snapshot.Documents.FirstOrDefault();
        if (coupleDoc == null) {
            Debug.LogError("커플 문서가 없습니다.");
            return;
        }

        string coupleId = coupleDoc.Id;

        SceneData scene = new SceneData();
        Furniture[] furnitures = FindObjectsOfType<Furniture>();
        foreach (var furniture in furnitures) {
            scene.objects.Add(furniture.GetObjectData());
        }

        string json = JsonUtility.ToJson(scene);
        FirebaseFirestore db = FirebaseFirestore.DefaultInstance;
        db.Collection("couples").Document(coupleId).UpdateAsync("SharedSceneJson", json).ContinueWithOnMainThread(task => {
            if (task.IsCompletedSuccessfully) {
                Debug.Log("씬 저장 완료");
            } else {
                Debug.LogError("씬 저장 실패: " + task.Exception);
            }
        });
    }
}