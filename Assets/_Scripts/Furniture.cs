using UnityEngine;

public class Furniture : MonoBehaviour {
    public string objectId;
    public FurnitureType furnitureType; // 배치용
    public bool canBeShelf;

    void Start()
    {
        if (string.IsNullOrEmpty(objectId)) {
            objectId = gameObject.name;
            Debug.Log($"Furniture Start: objectId 자동 설정 - 프리팹 이름: {objectId}");
        } else {
            Debug.Log($"Furniture Start: objectId 이미 설정됨: {objectId}");
        }
    }

    public ObjectData GetObjectData() {
        return new ObjectData {
            objectId = objectId,
            position = transform.position,
            rotation = transform.rotation,
            scale = transform.localScale,
            furnitureType = furnitureType.ToString(),
            canBeShelf = canBeShelf
        };
    }

    public void SetFromObjectData(ObjectData data) {
        transform.position = data.position;
        transform.rotation = data.rotation;
        transform.localScale = data.scale;
        furnitureType = (FurnitureType)System.Enum.Parse(typeof(FurnitureType), data.furnitureType);
        canBeShelf = data.canBeShelf;
    }
}