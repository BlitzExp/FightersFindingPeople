using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Terrain))]
public class TerrainGenerator : MonoBehaviour
{
    [Header("Terrain Settings")]
    public int terrainWidth = 20;
    public int terrainHeight = 20;
    public float terrainDepth = 5;

    [Header("Noise Settings")]
    public float scale = 20f;
    //Checkbox fo
    public bool randomseed;
    public int seed = 0;
    public Vector2 offset;

    [Header("Textures")]
    public TerrainLayer[] terrainLayers;

    [Header("Terrain Position")]
    public Vector3 terrainPosition = Vector3.zero;

    [Header("Spawnable Objects")]
    public SpawnableObject[] spawnableObjects;

    private Terrain terrain;

    void Start()
    {
        if (randomseed)
            seed = Random.Range(0, 10000);

        transform.position = terrainPosition;

        terrain = GetComponent<Terrain>();

        TerrainData newTerrainData = new TerrainData();
        newTerrainData = GenerateTerrain(newTerrainData);

        terrain.terrainData = newTerrainData;

        ApplyTextures(newTerrainData);

        SpawnObjects(newTerrainData);
    }

    TerrainData GenerateTerrain(TerrainData terrainData)
    {
        int resolution = GetClosestValidResolution(Mathf.Max(terrainWidth, terrainHeight));
        terrainData.heightmapResolution = resolution;

        terrainData.size = new Vector3(terrainWidth, terrainDepth, terrainHeight);
        terrainData.SetHeights(0, 0, GenerateHeights(resolution));

        return terrainData;
    }


    float[,] GenerateHeights(int resolution)
    {
        float[,] heights = new float[resolution, resolution];

        System.Random prng = new System.Random(seed);
        Vector2 seedOffset = new Vector2(prng.Next(-100000, 100000), prng.Next(-100000, 100000));
        Vector2 totalOffset = seedOffset + offset;

        int octaves = 4;
        float persistence = 0.5f;
        float lacunarity = 2.0f;

        for (int x = 0; x < resolution; x++)
        {
            for (int y = 0; y < resolution; y++)
            {
                float amplitude = 1f;
                float frequency = 1f;
                float noiseHeight = 0f;

                float percentX = (float)x / (resolution - 1);
                float percentY = (float)y / (resolution - 1);

                for (int i = 0; i < octaves; i++)
                {
                    float xCoord = (percentX * scale * frequency) + totalOffset.x;
                    float yCoord = (percentY * scale * frequency) + totalOffset.y;

                    float perlinValue = Mathf.PerlinNoise(xCoord, yCoord);
                    noiseHeight += perlinValue * amplitude;

                    amplitude *= persistence;
                    frequency *= lacunarity;
                }

                heights[x, y] = noiseHeight;
            }
        }

        return heights;
    }


    void ApplyTextures(TerrainData terrainData)
    {
        if (terrainLayers.Length == 0)
        {
            Debug.LogWarning("No TerrainLayers assigned!");
            return;
        }

        terrainData.terrainLayers = terrainLayers;

        int alphamapWidth = terrainData.alphamapWidth;
        int alphamapHeight = terrainData.alphamapHeight;
        int numTextures = terrainLayers.Length;

        float[,,] alphamaps = new float[alphamapWidth, alphamapHeight, numTextures];

        for (int y = 0; y < alphamapHeight; y++)
        {
            for (int x = 0; x < alphamapWidth; x++)
            {
                float normX = x * 1.0f / alphamapWidth;
                float normY = y * 1.0f / alphamapHeight;

                float height = terrainData.GetInterpolatedHeight(normX, normY) / terrainDepth;

                float[] textureMix = new float[numTextures];

                for (int i = 0; i < numTextures; i++)
                {
                    if (i == 0)
                        textureMix[i] = Mathf.Clamp01(1 - height * 2);
                    else if (i == 1)
                        textureMix[i] = Mathf.Clamp01(height * 2);
                    
                }

                float total = 0;
                for (int i = 0; i < numTextures; i++) total += textureMix[i];
                for (int i = 0; i < numTextures; i++) textureMix[i] /= total;

                for (int i = 0; i < numTextures; i++)
                {
                    alphamaps[x, y, i] = textureMix[i];
                }
            }
        }

        terrainData.SetAlphamaps(0, 0, alphamaps);
    }

    int GetClosestValidResolution(int terrainSize)
    {
        int[] validResolutions = { 33, 65, 129, 257, 513, 1025, 2049, 4097 };

        for (int i = 0; i < validResolutions.Length; i++)
        {
            if (validResolutions[i] >= terrainSize + 1)
            {
                return validResolutions[i];
            }
        }

        return 4097;
    }

    void SpawnObjects(TerrainData terrainData)
    {
        // Si se quiere que la distancia mínima sea inclusiva a todos los prefabs
        // List<Vector3> placedPositions = new List<Vector3>();



        foreach (var obj in spawnableObjects)
        {
            //Generate numebr of spawns
            int count = Random.Range(obj.mincount, obj.maxcount);

            if (obj.prefab == null || count <= 0)
                continue;

            // Si se quiere que la distancia mínima aplique solamente a elementos del mismo prefab
            List<Vector3> placedPositions = new List<Vector3>();

            int attempts = 0;
            int spawned = 0;

            while (spawned < count && attempts < count * 10)
            {
                float posX = Random.Range(0f, terrainData.size.x);
                float posZ = Random.Range(0f, terrainData.size.z);
                float normX = posX / terrainData.size.x;
                float normZ = posZ / terrainData.size.z;
                float posY = terrainData.GetInterpolatedHeight(normX, normZ);

                float scale = Random.Range(obj.minscale, obj.maxscale);

                Vector3 worldPos = new Vector3(posX, posY, posZ) + terrain.transform.position;

                // Check distance from all previous objects
                bool tooClose = false;
                foreach (var placed in placedPositions)
                {
                    if (Vector3.Distance(worldPos, placed) < obj.minDistance)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (!tooClose)
                {
                    if (obj.name.Contains("Tree"))
                    {
                        Vector3 normal = terrainData.GetInterpolatedNormal(normX, normZ);
                        float slope = Vector3.Angle(normal, Vector3.up);
                        if (slope > 40)
                        {
                            attempts++;
                            continue;
                        }
                        GameObject spawnedObject = Instantiate(obj.prefab, worldPos, Quaternion.Euler(-90, Random.Range(0f, 360f), 0), this.transform);
                        spawnedObject.transform.localScale = Vector3.one * scale;
                    }
                    else 
                    {
                        GameObject spawnedObject = Instantiate(obj.prefab, worldPos, Quaternion.Euler(0, Random.Range(0f, 360f), 0), this.transform);
                        spawnedObject.transform.localScale = Vector3.one * scale;
                    }
                    placedPositions.Add(worldPos);
                    spawned++;
                }

                attempts++;
            }
        }
    }

}
