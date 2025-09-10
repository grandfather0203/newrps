using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ObjectData {
    public string objectId;
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 scale;
    public string furnitureType;
    public bool canBeShelf;
}

[System.Serializable]
public class SceneData {
    public List<ObjectData> objects = new List<ObjectData>();
}