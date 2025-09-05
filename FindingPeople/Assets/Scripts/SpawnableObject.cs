using UnityEngine;

[System.Serializable]
public class SpawnableObject
{
    public string name = "Object";
    public GameObject prefab;
    public int maxcount = 50;
    public int mincount = 0;
    public float  minscale = 0.1f;
    public float maxscale  = 1f;
    public float minDistance = 2f;
}
