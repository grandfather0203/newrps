using Firebase;
using UnityEngine;

public class FirebaseInitializer : MonoBehaviour {
    void Start() {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task => {
            var dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available) {
                Debug.Log("Firebase 초기화 성공!");
            } else {
                Debug.LogError("Firebase 초기화 실패: " + dependencyStatus);
            }
        });
    }
}