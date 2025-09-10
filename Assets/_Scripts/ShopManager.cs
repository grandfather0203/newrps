using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.EventSystems;
using System; // Serializable for List

public class ShopManager : MonoBehaviour
{
    [Header("UI References")]
    public Transform content;
    public Transform[] categoryPanels;
    public GameObject buttonPrefab;
    public GameObject shopPanel;
    public GameObject detailPanel;
    public Image detailImage;
    public TMP_Text detailPriceText;
    public Button closeButton, confirmBuyButton;

    [Header("Prefabs List (Inspector Drag - Dynamic Size)")]
    [SerializeField] public List<GameObject> furniturePrefabs = new List<GameObject>();

    [Header("Managers")]
    public MoneyManager moneyManager;
    public InventoryManager inventoryManager;
    public HomeManager homeManager;

    private Queue<Button> buttonPool = new Queue<Button>();

    void Start()
    {
        if (categoryPanels == null || categoryPanels.Length != System.Enum.GetNames(typeof(FurnitureCategory)).Length) {
            Debug.LogError($"categoryPanels 배열 크기({categoryPanels?.Length ?? 0})가 FurnitureCategory enum({System.Enum.GetNames(typeof(FurnitureCategory)).Length})과 다릅니다!");
        }

        if (furniturePrefabs == null || furniturePrefabs.Count == 0) {
            Debug.LogError("Inspector에 가구 프리팹 리스트를 추가하세요! (furniturePrefabs) – Project에서 드래그하고 Size 변경");
        }
        else {
            Debug.Log($"furniturePrefabs 초기화 완료: {furniturePrefabs.Count}개 프리팹");
            foreach (var prefab in furniturePrefabs) {
                FurnitureData data = prefab.GetComponent<FurnitureData>();
                Furniture furniture = prefab.GetComponent<Furniture>();
                Debug.Log($"프리팹 이름: {prefab.name}, FurnitureData: {(data != null ? "있음" : "없음")}, objectId: {(furniture != null ? furniture.objectId : "null")}, thumbnailImage: {(data?.thumbnailImage != null ? data.thumbnailImage.name : "null")}");
            }
        }

        if (buttonPrefab == null) {
            Debug.LogError("buttonPrefab이 설정되지 않았습니다! Resources/Prefabs/UI/ShopButton.prefab 확인");
        }
        else {
            Image furnitureImage = buttonPrefab.transform.Find("FurnitureImage")?.GetComponent<Image>();
            Debug.Log($"buttonPrefab의 FurnitureImage: {(furnitureImage != null ? "있음" : "없음")}");
        }

        for (int i = 0; i < 50; i++)
        {
            GameObject btnObj = Instantiate(buttonPrefab, content);
            btnObj.SetActive(false);
            buttonPool.Enqueue(btnObj.GetComponent<Button>());
        }

        if (shopPanel != null) shopPanel.SetActive(false);
        else Debug.LogError("shopPanel이 설정되지 않았습니다!");
        if (detailPanel != null) detailPanel.SetActive(false);
        else Debug.LogError("detailPanel이 설정되지 않았습니다!");
        if (detailImage == null) Debug.LogError("detailImage가 설정되지 않았습니다!");
        if (detailPriceText == null) Debug.LogError("detailPriceText가 설정되지 않았습니다!");
        if (closeButton == null) Debug.LogError("closeButton이 설정되지 않았습니다!");
        if (confirmBuyButton == null) Debug.LogError("confirmBuyButton이 설정되지 않았습니다!");

        ClearAllCategoryPanels();
    }

    public void ToggleShopPanel()
    {
        if (shopPanel == null) {
            Debug.LogError("ToggleShopPanel 실패: shopPanel null");
            return;
        }
        shopPanel.SetActive(!shopPanel.activeSelf);
        if (shopPanel.activeSelf)
        {
            Debug.Log("ShopPanel 열림, RefreshShopButtons 호출");
            RefreshShopButtons();
            if (detailPanel != null) detailPanel.SetActive(false);
            else Debug.LogError("detailPanel null, 상세 패널 닫기 실패");
        }
    }

    private void ClearAllCategoryPanels()
    {
        if (categoryPanels == null) {
            Debug.LogError("categoryPanels null");
            return;
        }
        foreach (Transform catPanel in categoryPanels)
        {
            if (catPanel == null) {
                Debug.LogWarning("categoryPanels에 null 요소 있음");
                continue;
            }
            foreach (Transform child in catPanel)
            {
                if (child.gameObject.activeSelf)
                {
                    child.gameObject.SetActive(false);
                    Button btn = child.GetComponent<Button>();
                    if (btn != null) buttonPool.Enqueue(btn);
                }
            }
        }
    }

    public void RefreshShopButtons()
    {
        if (furniturePrefabs == null || furniturePrefabs.Count == 0) {
            Debug.LogWarning("가구 프리팹 리스트가 비어 있습니다!");
            return;
        }

        ClearAllCategoryPanels();

        var groupedItems = furniturePrefabs.Select(prefab => prefab.GetComponent<FurnitureData>())
            .Where(data => data != null)
            .GroupBy(data => (int)data.category);

        Debug.Log($"그룹화된 아이템 수: {groupedItems.Count()} 카테고리");

        for (int catIndex = 0; catIndex < categoryPanels.Length; catIndex++)
        {
            Transform catPanel = categoryPanels[catIndex];
            if (catPanel == null) {
                Debug.LogWarning($"categoryPanels[{catIndex}] null");
                continue;
            }

            var categoryItems = groupedItems.FirstOrDefault(g => g.Key == catIndex)?.ToList() ?? new List<FurnitureData>();
            Debug.Log($"카테고리 {catIndex} ({(FurnitureCategory)catIndex}): {categoryItems.Count}개 아이템");

            foreach (FurnitureData item in categoryItems)
            {
                if (item == null) {
                    Debug.LogWarning("FurnitureData null");
                    continue;
                }

                Button btn;
                if (buttonPool.Count > 0)
                {
                    GameObject pooledObj = buttonPool.Dequeue().gameObject;
                    pooledObj.transform.SetParent(catPanel);
                    btn = pooledObj.GetComponent<Button>();
                    Debug.Log($"풀링 버튼 사용: {item.itemName}");
                }
                else
                {
                    GameObject newBtnObj = Instantiate(buttonPrefab, catPanel);
                    btn = newBtnObj.GetComponent<Button>();
                    Debug.Log($"새 버튼 생성: {item.itemName}");
                }

                btn.gameObject.SetActive(true);

                Image furnitureImage = btn.transform.Find("FurnitureImage")?.GetComponent<Image>();
                if (furnitureImage != null && item.thumbnailImage != null) {
                    furnitureImage.sprite = item.thumbnailImage;
                    Debug.Log($"버튼 이미지 설정: {item.itemName}, sprite: {item.thumbnailImage.name}");
                } else {
                    Debug.LogWarning($"버튼 이미지 설정 실패: {item.itemName}, FurnitureImage {(furnitureImage == null ? "null" : "OK")}, thumbnailImage {(item.thumbnailImage == null ? "null" : "OK")}");
                }

                TMP_Text btnText = btn.GetComponentInChildren<TMP_Text>();
                if (btnText != null) {
                    btnText.text = $"{item.itemName}\n{item.price}원";
                    Debug.Log($"버튼 텍스트 설정: {item.itemName}");
                } else {
                    Debug.LogWarning($"버튼 텍스트 설정 실패: {item.itemName}");
                }

                btn.onClick.RemoveAllListeners();
                FurnitureData currentItem = item; // 로컬 변수로 클로저 캡처 고정
                Furniture furniture = item.GetComponent<Furniture>();
                string objectId = furniture != null ? furniture.objectId : "null";
                GameObject prefab = furniturePrefabs.FirstOrDefault(p => p.GetComponent<FurnitureData>() == item);
                string prefabName = prefab != null ? prefab.name : "null";
                Debug.Log($"버튼 이벤트 등록: item = {currentItem.itemName}, objectId = {objectId}, 프리팹 이름 = {prefabName}");
                btn.onClick.AddListener(() => ShowDetailPanel(currentItem, objectId, prefabName));
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(catPanel as RectTransform);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(content as RectTransform);
    }

    void ShowDetailPanel(FurnitureData item, string objectId, string prefabName)
    {
        if (detailPanel == null || item == null) {
            Debug.LogError($"ShowDetailPanel 실패: detailPanel {(detailPanel == null ? "null" : "OK")}, item {(item == null ? "null" : item.itemName)}");
            return;
        }

        Debug.Log($"ShowDetailPanel 호출: item = {item.itemName}, objectId = {objectId}, 프리팹 이름 = {prefabName}");
        detailPanel.SetActive(true);

        if (detailImage != null && item.thumbnailImage != null) {
            detailImage.sprite = item.thumbnailImage;
            Debug.Log($"상세 이미지 설정: {item.itemName}, sprite: {item.thumbnailImage.name}");
        } else {
            Debug.LogWarning($"상세 이미지 설정 실패: {item.itemName}, detailImage {(detailImage == null ? "null" : "OK")}, thumbnailImage {(item.thumbnailImage == null ? "null" : "OK")}");
        }

        if (detailPriceText != null) {
            detailPriceText.text = $"{item.price}G";
            Debug.Log($"상세 가격 설정: {item.price}G");
        } else {
            Debug.LogWarning("detailPriceText null");
        }

        if (closeButton != null) {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() => detailPanel.SetActive(false));
        } else {
            Debug.LogWarning("closeButton null");
        }

        if (confirmBuyButton != null) {
            confirmBuyButton.onClick.RemoveAllListeners();
            confirmBuyButton.onClick.AddListener(() => BuyItem(item, objectId, prefabName));
        } else {
            Debug.LogWarning("confirmBuyButton null");
        }
    }

    public void BuyItem(FurnitureData item, string objectId, string prefabName)
    {
        if (item == null || moneyManager == null || inventoryManager == null) {
            Debug.LogError($"BuyItem 실패: item {(item == null ? "null" : item.itemName)}, moneyManager {(moneyManager == null ? "null" : "OK")}, inventoryManager {(inventoryManager == null ? "null" : "OK")}");
            return;
        }

        Debug.Log($"BuyItem 호출: item = {item.itemName}, objectId = {objectId}, 프리팹 이름 = {prefabName}");
        if (moneyManager.GetMoney() >= item.price)
        {
            moneyManager.SpendMoney(item.price);
            inventoryManager.AddToInventory(objectId);
            if (homeManager != null) {
                homeManager.ShowNotification($"구매 완료! {item.itemName}이(가) 인벤토리에 추가됐어요.", 2f);
                Debug.Log($"구매 성공: item = {item.itemName}, objectId = {objectId}, 프리팹 이름 = {prefabName}, 잔고: {moneyManager.GetMoney()}");
            }
            if (detailPanel != null) detailPanel.SetActive(false);
        }
        else
        {
            if (homeManager != null) {
                homeManager.ShowNotification($"돈이 부족해요! 현재 잔고: {moneyManager.GetMoney()}", 2f);
                Debug.LogWarning($"구매 실패: item = {item.itemName}, objectId = {objectId}, 프리팹 이름 = {prefabName}, 부족 금액: {item.price - moneyManager.GetMoney()}");
            }
        }
    }
}