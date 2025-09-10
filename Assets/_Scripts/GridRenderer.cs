using UnityEngine;

public class GridRenderer : MonoBehaviour {
    public int gridSize = 10; // 10x10 격자
    public float cellSize = 1f; // 1m x 1m
    public float lineWidth = 0.05f; // 라인 두께
    public Color lineColor = new Color(1, 1, 1, 0.5f); // 흰색, 투명도 50%

    private LineRenderer[] lines;
    private CanvasGroup canvasGroup;
    private float fadeDuration = 0.5f;
    private float fadeTimer;
    private bool isFadingIn;

    void Start() {
        canvasGroup = gameObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0; // 초기 투명
        GenerateGrid();
    }

    void Update() {
        if (fadeTimer > 0) {
            fadeTimer -= Time.deltaTime;
            float t = 1f - fadeTimer / fadeDuration;
            canvasGroup.alpha = isFadingIn ? t : 1f - t;
            if (fadeTimer <= 0) canvasGroup.alpha = isFadingIn ? 1f : 0f;
        }
    }

    void GenerateGrid() {
        lines = new LineRenderer[gridSize * 2 + 2]; // x축 11개, z축 11개
        float halfSize = gridSize * cellSize / 2f;

        // x축 라인 (수평)
        for (int i = 0; i <= gridSize; i++) {
            GameObject lineObj = new GameObject("GridLineX_" + i);
            lineObj.transform.SetParent(transform);
            LineRenderer line = lineObj.AddComponent<LineRenderer>();
            line.positionCount = 2;
            float z = -halfSize + i * cellSize;
            line.SetPositions(new Vector3[] {
                new Vector3(-halfSize, 0.01f, z),
                new Vector3(halfSize, 0.01f, z)
            });
            line.startWidth = lineWidth;
            line.endWidth = lineWidth;
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.startColor = lineColor;
            line.endColor = lineColor;
            lines[i] = line;
        }

        // z축 라인 (수직)
        for (int i = 0; i <= gridSize; i++) {
            GameObject lineObj = new GameObject("GridLineZ_" + i);
            lineObj.transform.SetParent(transform);
            LineRenderer line = lineObj.AddComponent<LineRenderer>();
            line.positionCount = 2;
            float x = -halfSize + i * cellSize;
            line.SetPositions(new Vector3[] {
                new Vector3(x, 0.01f, -halfSize),
                new Vector3(x, 0.01f, halfSize)
            });
            line.startWidth = lineWidth;
            line.endWidth = lineWidth;
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.startColor = lineColor;
            line.endColor = lineColor;
            lines[gridSize + 1 + i] = line;
        }
    }

    public void FadeIn(float duration) {
        fadeDuration = duration;
        fadeTimer = duration;
        isFadingIn = true;
    }

    public void FadeOut(float duration) {
        fadeDuration = duration;
        fadeTimer = duration;
        isFadingIn = false;
    }
}