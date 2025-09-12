using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Terrain))]
[RequireComponent(typeof(TerrainCollider))]
public class TerrainGenerator : MonoBehaviour
{
    [Header("Terrain Settings")]
    public int terrainWidth = 20;
    public int terrainHeight = 20;
    public float terrainDepth = 5;

    [Header("Noise Settings")]
    public float scale = 20f;
    public bool randomseed;
    public int seed = 0;
    public Vector2 offset;

    [Header("Textures")]
    public TerrainLayer[] terrainLayers;

    private Vector3 terrainPosition = Vector3.zero;

    [Header("Spawnable Objects")]
    public SpawnableObject[] spawnableObjects;

    private personclass[] personsToSpawn;
    private Terrain terrain;

    public int personsCount = 0;
    public int objectivesCount = 0;

    [Header("Persons Spawn Settings")]
    public float personMinDistance = 5f; // distancia mínima entre personas

    [SerializeField] public TerrainCollider terrainCol;

    // Reference to the Terrain component
    void Awake()
    {
        terrain = GetComponent<Terrain>();
        if (terrainCol == null)
            terrainCol = GetComponent<TerrainCollider>();
    }

    // Set the posiciont value form the GameManager script
    public void SetTerrainPosition(Vector3 pos)
    {
        terrainPosition = pos;
    }

    // Set the persons to spawn from the GameManager script
    public void SetPersonsToSpawn(personclass[] persons)
    {
        personsToSpawn = persons;
    }

    //Starts the terrain generation after all the values are obtained
    public void StartGeneration()
    {
        // Generates a random terrain seed
        if (randomseed)
            seed = Random.Range(0, 10000);

        terrain = GetComponent<Terrain>();

        TerrainData newTerrainData = new TerrainData();
        newTerrainData = GenerateTerrain(newTerrainData);

        terrain.terrainData = newTerrainData;

        // Makes the terrain collider match the generated terrain
        if (terrainCol == null) terrainCol = GetComponent<TerrainCollider>();
        if (terrainCol == null) terrainCol = gameObject.AddComponent<TerrainCollider>();
        terrainCol.terrainData = newTerrainData;

        transform.position = terrainPosition - new Vector3(terrainWidth / 2f, 0, terrainHeight / 2f);

        ApplyTextures(newTerrainData);
        SpawnPersons(newTerrainData);
        SpawnObjects(newTerrainData);
    }

    // Generates the terrain heights using Perlin noise
    TerrainData GenerateTerrain(TerrainData terrainData)
    {
        int resolution = GetClosestValidResolution(Mathf.Max(terrainWidth, terrainHeight));
        terrainData.heightmapResolution = resolution;

        terrainData.size = new Vector3(terrainWidth, terrainDepth, terrainHeight);
        terrainData.SetHeights(0, 0, GenerateHeights(resolution));

        return terrainData;
    }

    // Generates a height map using Perlin noise
    float[,] GenerateHeights(int resolution)
    {
        float[,] heights = new float[resolution, resolution];

        System.Random prng = new System.Random(seed);
        Vector2 seedOffset = new Vector2(prng.Next(-100000, 100000), prng.Next(-100000, 100000));
        Vector2 totalOffset = seedOffset + offset;

        int octaves = 4;
        float persistence = 0.5f;
        float lacunarity = 2.0f;

        float maxPossible = 0f;
        float amp = 1f;
        for (int i = 0; i < octaves; i++) { maxPossible += amp; amp *= persistence; }

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

                heights[x, y] = Mathf.Clamp01(noiseHeight / maxPossible);
            }
        }

        return heights;
    }

    // Applies textures to the terrain based on height
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
                float normX = (float)x / (alphamapWidth - 1);
                float normY = (float)y / (alphamapHeight - 1);

                float height = terrainData.GetInterpolatedHeight(normX, normY) / terrainDepth;

                float[] textureMix = new float[numTextures];

                for (int i = 0; i < numTextures; i++)
                {
                    if (i == 0)
                        textureMix[i] = Mathf.Clamp01(1 - height * 2);
                    else if (i == 1)
                        textureMix[i] = Mathf.Clamp01(height * 2);
                    else
                        textureMix[i] = 0f;
                }

                float total = 0f;
                for (int i = 0; i < numTextures; i++) total += textureMix[i];
                if (total == 0f) textureMix[0] = 1f;

                for (int i = 0; i < numTextures; i++)
                {
                    alphamaps[x, y, i] = textureMix[i];
                }
            }
        }

        terrainData.SetAlphamaps(0, 0, alphamaps);
    }

    // Returns the closest valid terrain resolution
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

    // Spawns objects like trees and rocks on the terrain
    void SpawnObjects(TerrainData terrainData)
    {
        foreach (var obj in spawnableObjects)
        {
            int count = Random.Range(obj.mincount, obj.maxcount);

            if (obj.prefab == null || count <= 0)
                continue;

            List<Vector3> placedPositions = new List<Vector3>();

            int attempts = 0;
            int spawned = 0;

            while (spawned < count && attempts < count * 10)
            {
                float posX = Random.Range(0f, terrainData.size.x);
                float posZ = Random.Range(0f, terrainData.size.z);

                Vector3 worldPos = new Vector3(posX, 0f, posZ) + terrain.transform.position;

                float posY = terrain.SampleHeight(worldPos);
                worldPos.y = posY;

                float scale = Random.Range(obj.minscale, obj.maxscale);

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
                        float normX = (worldPos.x - terrain.transform.position.x) / terrainData.size.x;
                        float normZ = (worldPos.z - terrain.transform.position.z) / terrainData.size.z;
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

    // Spawns persons on the terrain
    void SpawnPersons(TerrainData terrainData)
    {
        if (personsToSpawn == null || personsToSpawn.Length == 0)
        {
            Debug.LogWarning("No persons to spawn assigned!");
            return;
        }

        List<Vector3> placedPositions = new List<Vector3>();
        int spawnedPersons = 0;

        // spawn objective persons
        foreach (var person in personsToSpawn)
        {
            if (person == null || person.prefab == null) continue;
            if (person.isObjective)
            {
                Vector3 spawnPos = GetPositionWithinRadius(terrainData, 20f, placedPositions);
                float safeY = terrain.SampleHeight(spawnPos);
                spawnPos.y = safeY;

                GameObject spawnedPerson = Instantiate(person.prefab, spawnPos, Quaternion.identity, this.transform);
                spawnedPerson.transform.localRotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);

                Collider col = spawnedPerson.GetComponent<Collider>();
                if (col != null)
                {
                    float halfHeight = col.bounds.extents.y;
                    spawnedPerson.transform.position += Vector3.up * (halfHeight + 0.01f);
                }

                placedPositions.Add(spawnPos);
                spawnedPersons++;
                objectivesCount++;
            }
        }

        // spawn non-objective persons
        int attempts = 0;
        while (spawnedPersons < personsCount && attempts < personsCount * 20)
        {
            var nonObjective = personsToSpawn[Random.Range(0, personsToSpawn.Length)];
            if (nonObjective == null || nonObjective.prefab == null || nonObjective.isObjective)
            {
                attempts++;
                continue;
            }

            Vector3 spawnPos = GetPositionWithinRadius(terrainData, 20f, placedPositions);

            float safeY = terrain.SampleHeight(spawnPos);
            spawnPos.y = safeY;

            GameObject spawnedPerson = Instantiate(nonObjective.prefab, spawnPos, Quaternion.identity, this.transform);
            spawnedPerson.transform.localRotation = Quaternion.Euler(-90, Random.Range(0f, 360f), 0);

            Collider col2 = spawnedPerson.GetComponent<Collider>();
            float extraHeight = (col2 != null) ? col2.bounds.extents.y + 0.01f : 0f;

            spawnedPerson.transform.position = new Vector3(spawnPos.x, safeY + extraHeight, spawnPos.z);

            placedPositions.Add(spawnPos);
            spawnedPersons++;
            attempts++;
        }

        Debug.Log($"Spawned {spawnedPersons} persons. Objectives: {objectivesCount}");
    }

    // Gets a random position within radius
    Vector3 GetRandomPositionOnTerrainWithinRadius(TerrainData terrainData, float radius)
    {
        // center of the terrain
        Vector3 terrainOrigin = terrain.transform.position;
        Vector3 center = terrainOrigin + new Vector3(terrainData.size.x / 2f, 0f, terrainData.size.z / 2f);

        Vector2 randomCircle = Random.insideUnitCircle * radius;
        float posX = center.x + randomCircle.x;
        float posZ = center.z + randomCircle.y;

        Vector3 worldPos = new Vector3(posX, 0f, posZ);

        float posY = terrain.SampleHeight(worldPos);
        return new Vector3(posX, posY + 0.01f, posZ);
    }

    // Ensures separation between persons
    Vector3 GetPositionWithinRadius(TerrainData terrainData, float radius, List<Vector3> placedPositions)
    {
        int maxTries = 50;

        for (int i = 0; i < maxTries; i++)
        {
            Vector3 candidate = GetRandomPositionOnTerrainWithinRadius(terrainData, radius);

            bool tooClose = false;
            foreach (var placed in placedPositions)
            {
                if (Vector3.Distance(candidate, placed) < personMinDistance)
                {
                    tooClose = true;
                    break;
                }
            }

            if (!tooClose)
                return candidate;
        }

        // fallback
        return GetRandomPositionOnTerrainWithinRadius(terrainData, radius);
    }
}
