using UnityEngine;

[RequireComponent(typeof(Terrain))]
public class TerrainGenerator : MonoBehaviour
{
    [Header("Terrain Settings")]
    public int terrainWidth = 20;
    public int terrainHeight = 20;
    public float terrainDepth = 5;

    [Header("Noise Settings")]
    public float scale = 20f;
    public int seed = 0;
    public Vector2 offset;

    [Header("Textures")]
    public TerrainLayer[] terrainLayers;

    [Header("Terrain Position")]
    public Vector3 terrainPosition = Vector3.zero;

    private Terrain terrain;

    void Start()
{
    transform.position = terrainPosition;
    
    terrain = GetComponent<Terrain>();

    TerrainData newTerrainData = new TerrainData();
    newTerrainData = GenerateTerrain(newTerrainData);

    terrain.terrainData = newTerrainData;

    ApplyTextures(newTerrainData);
}

    TerrainData GenerateTerrain(TerrainData terrainData)
    {
        terrainData.heightmapResolution = terrainWidth + 1;
        terrainData.size = new Vector3(terrainWidth, terrainDepth, terrainHeight);
        terrainData.SetHeights(0, 0, GenerateHeights());
        return terrainData;
    }

    float[,] GenerateHeights()
    {
        float[,] heights = new float[terrainWidth, terrainHeight];

        System.Random prng = new System.Random(seed);
        Vector2 seedOffset = new Vector2(prng.Next(-100000, 100000), prng.Next(-100000, 100000));
        Vector2 totalOffset = seedOffset + offset;

        int octaves = 4;
        float persistence = 0.5f;
        float lacunarity = 2.0f;

        for (int x = 0; x < terrainWidth; x++)
        {
            for (int y = 0; y < terrainHeight; y++)
            {
                float amplitude = 1f;
                float frequency = 1f;
                float noiseHeight = 0f;

                for (int i = 0; i < octaves; i++)
                {
                    float xCoord = ((float)x / terrainWidth * scale * frequency) + totalOffset.x;
                    float yCoord = ((float)y / terrainHeight * scale * frequency) + totalOffset.y;

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
}
