using UnityEngine;

[RequireComponent(typeof(Terrain))]
public class TerrainGenerator : MonoBehaviour
{
    [Header("Terrain Settings")]
    public int terrainWidth = 20;
    public int terrainHeight = 20;
    public int terrainDepth = 5;

    [Header("Noise Settings")]
    public float scale = 20f;
    public int seed = 0;
    public Vector2 offset;

    private Terrain terrain;

    void Start()
    {
        terrain = GetComponent<Terrain>();
        terrain.terrainData = GenerateTerrain(terrain.terrainData);
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

}
