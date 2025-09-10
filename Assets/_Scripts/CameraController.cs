using UnityEngine;
using System.Collections;

public class CameraController : MonoBehaviour {
    private Vector3 lookAtPoint = new Vector3(0, 3, 0);
    private Vector3 defaultPosition = new Vector3(16f, 19.71f, -16f);
    private Vector3 defaultRotation = new Vector3(31.776f, -45f, 0f);
    private float currentYAngle = 0f;
    private bool isMoving = false;
    private float transitionDuration = 0.5f;

    private readonly Vector3[] positions = {
        new Vector3(0f, 16f, -25f),
        new Vector3(-25f, 16f, 0f),
        new Vector3(0f, 16f, 25f),
        new Vector3(25f, 16f, 0f)
    };
    private readonly Vector3[] rotations = {
        new Vector3(20f, 360f, 0f),
        new Vector3(20f, 90f, 0f),
        new Vector3(20f, 180f, 0f),
        new Vector3(20f, 270f, 0f) 
    };

    void Start() {
        ResetToDefault();
    }

    public void RotateLeft() {
        if (isMoving) return;
        currentYAngle -= 90f;
        if (currentYAngle < 0f) currentYAngle += 360f;
        StartCoroutine(MoveToPositionAndRotation());
    }

    public void RotateRight() {
        if (isMoving) return;
        currentYAngle += 90f;
        if (currentYAngle >= 360f) currentYAngle -= 360f;
        StartCoroutine(MoveToPositionAndRotation());
    }

    public void RotateBottom() {
        if (isMoving) return;
        currentYAngle += 180f;
        if (currentYAngle >= 360f) currentYAngle -= 360f;
        StartCoroutine(MoveToPositionAndRotation());
    }

    public void ResetToDefault() {
        if (isMoving) return;
        StartCoroutine(MoveToDefault());
    }

    private IEnumerator MoveToPositionAndRotation() {
        isMoving = true;
        currentYAngle = Mathf.Round(currentYAngle / 90f) * 90f;
        int index = ((int)currentYAngle % 360) / 90; 
        Vector3 targetPosition = positions[index];
        Quaternion targetRotation = Quaternion.Euler(rotations[index]);
        
        Vector3 startPosition = transform.position;
        Quaternion startRotation = transform.rotation;
        float time = 0f;

        while (time < transitionDuration) {
            time += Time.deltaTime;
            float t = time / transitionDuration;
            transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
            transform.LookAt(lookAtPoint);
            yield return null;
        }

        transform.position = targetPosition;
        transform.rotation = targetRotation;
        transform.LookAt(lookAtPoint);
        isMoving = false;
    }

    private IEnumerator MoveToDefault() {
        isMoving = true;
        Vector3 startPosition = transform.position;
        Quaternion startRotation = transform.rotation;
        float time = 0f;

        while (time < transitionDuration) {
            time += Time.deltaTime;
            float t = time / transitionDuration;
            transform.position = Vector3.Lerp(startPosition, defaultPosition, t);
            transform.rotation = Quaternion.Slerp(startRotation, Quaternion.Euler(defaultRotation), t);
            transform.LookAt(lookAtPoint);
            yield return null;
        }

        transform.position = defaultPosition;
        transform.rotation = Quaternion.Euler(defaultRotation);
        transform.LookAt(lookAtPoint);
        isMoving = false;
    }
}