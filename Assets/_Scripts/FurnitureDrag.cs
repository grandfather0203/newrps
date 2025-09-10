using UnityEngine;

public class FurnitureDrag : MonoBehaviour {
    private bool isDragging = false;
    private Vector3 offset;
    private Renderer renderer;
    private HomeManager homeManager;
    private Furniture furniture;
    private float gridHalfSize = 5f; 
    private Color originalColor;
    private Vector3? wallNormal = null; 
    public float wallOffset = 12f;

    void Start() {
        renderer = GetComponent<Renderer>();
        if (renderer == null) renderer = GetComponentInChildren<Renderer>();
        originalColor = renderer.material.color;
        furniture = GetComponent<Furniture>();
        homeManager = FindObjectOfType<HomeManager>();
    }

    void Update() {
        if (!enabled) return;

        if (Input.touchCount > 0) {
            Touch touch = Input.GetTouch(0);
            Ray ray = Camera.main.ScreenPointToRay(touch.position);

            if (touch.phase == TouchPhase.Began && !isDragging) {
                if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, LayerMask.GetMask("Furniture"))) {
                    if (hit.transform == transform) {
                        isDragging = true;
                        offset = transform.position - hit.point;
                        if (furniture.furnitureType == FurnitureType.Wall) {
                            // 터치 위치에서 벽 감지
                            if (Physics.Raycast(ray, out RaycastHit wallHit, Mathf.Infinity, LayerMask.GetMask("Wall"))) {
                                wallNormal = wallHit.normal;
                                Vector3 newPos = wallHit.point - wallNormal.Value * wallOffset;
                                transform.position = newPos;
                                UpdateWallRotation();
                            }
                        }
                        homeManager.ShowConfirmButton(furniture.furnitureType);
                    }
                }
            } else if (touch.phase == TouchPhase.Moved && isDragging) {
                switch (furniture.furnitureType) {
                    case FurnitureType.Floor:
                        HandleFloorDrag(ray);
                        break;
                    case FurnitureType.Wall:
                        HandleWallDrag(ray);
                        break;
                    case FurnitureType.Prop:
                        HandlePropDrag(ray);
                        break;
                }
                bool canPlace = CanPlaceFurniture();
                renderer.material.color = canPlace ? Color.green : Color.red;
            } else if (touch.phase == TouchPhase.Ended) {
                isDragging = false;
            }
        }
    }

    void UpdateWallRotation() {
        if (wallNormal == null) return;
        if (Mathf.Abs(wallNormal.Value.z) > 0.9f) {
            transform.rotation = Quaternion.Euler(0, -wallNormal.Value.z > 0 ? 180 : 0, 0);
        } else if (Mathf.Abs(wallNormal.Value.x) > 0.9f) {
            transform.rotation = Quaternion.Euler(0, -wallNormal.Value.x > 0 ? 270 : 90, 0);
        }
    }

    void HandleFloorDrag(Ray ray) {
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, LayerMask.GetMask("Floor"))) {
            Vector3 newPos = hit.point + offset;
            newPos.y = transform.position.y;
            transform.position = newPos;
        }
    }

    void HandleWallDrag(Ray ray) {
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, LayerMask.GetMask("Wall", "Floor"))) {
            Vector3 newNormal = hit.normal;
            if (hit.transform.gameObject.layer == LayerMask.NameToLayer("Wall") && newNormal != wallNormal) {
                wallNormal = newNormal;
                UpdateWallRotation();
            }

            if (wallNormal == null) return;

            Vector3 newPos = hit.point + offset;
            if (Mathf.Abs(wallNormal.Value.z) > 0.9f) {
                newPos.z = wallNormal.Value.z > 0 ? gridHalfSize - wallOffset : -gridHalfSize + wallOffset;
            } else if (Mathf.Abs(wallNormal.Value.x) > 0.9f) {
                newPos.x = wallNormal.Value.x > 0 ? gridHalfSize - wallOffset : -gridHalfSize + wallOffset;
            }
            transform.position = newPos;
        }
    }

    void HandlePropDrag(Ray ray) {
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, LayerMask.GetMask("Floor", "Furniture"))) {
            Vector3 newPos = hit.point + offset;
            Furniture hitFurniture = hit.transform.GetComponent<Furniture>();
            if (hitFurniture != null && hitFurniture.canBeShelf) {
                newPos.y = hit.transform.position.y + hit.transform.localScale.y / 2 + offset.y;
            } else {
                newPos.y = transform.position.y;
            }
            transform.position = newPos;
        }
    }

    public bool CanPlaceFurniture() {
        Vector3 pos = transform.position;
        if (furniture.furnitureType != FurnitureType.Wall) {
            if (Mathf.Abs(pos.x) > gridHalfSize || Mathf.Abs(pos.z) > gridHalfSize) {
                return false;
            }
        }

        Collider collider = GetComponent<Collider>();
        if (collider == null) return false;
        Bounds bounds = collider.bounds;
        Collider[] colliders = Physics.OverlapBox(bounds.center, bounds.extents, Quaternion.identity, LayerMask.GetMask("Furniture"));
        foreach (var col in colliders) {
            if (col.gameObject != gameObject) {
                return false;
            }
        }
        return true;
    }

    public void OnPlaced() {
        isDragging = false;
        enabled = false;
        renderer.material.color = originalColor;
        wallNormal = null;
    }
}