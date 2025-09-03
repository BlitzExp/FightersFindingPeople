using UnityEngine;

[System.Serializable]
public class SpawnableObject
{
    public string name = "Object";
    public GameObject prefab;
    public int count = 50;
    public float minDistance = 2f;
}
