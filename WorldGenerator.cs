using System.Collections.Generic;
using UnityEngine;

public class WorldGenerator : MonoBehaviour
{
    [Header("Основні параметри")]
    public int chunkWidth = 32;
    public int platformHeight = 5;
    public int groundThickness = 6;
    public int yOffset = -5;

    [Header("Гравець та блоки")]
    public GameObject topBlockPrefab;
    public GameObject bottomBlockPrefab;
    public GameObject platformPrefab;
    public Transform player;
    public int renderDistance = 3;

    [Header("Прогалини")]
    public float holeChance = 0.05f;
    public int fixedHoleLength = 4;

    [Header("Лава в прогалинах")]
    public GameObject lavaPrefab;
    public float lavaYOffset = -1f;

    [Header("Декорації")]
    public GameObject torchPrefab;
    public GameObject statuePrefab;
    public GameObject flowerPrefab;
    public GameObject skullPrefab;
    public float decorationChance = 0.1f;
    public int maxDecorationsPerChunk = 4;

    [Header("Замкові гори")]
    [Range(0f, 1f)]
    public float castleHillChance = 0.1f;
    public GameObject slopeUpPrefab;
    public GameObject slopeDownPrefab;

    [Header("Вороги")]
    public GameObject enemyPrefab;
    public float enemySpawnChance = 0.15f;
    public int maxEnemiesPerChunk = 3;

    private Dictionary<Vector2Int, GameObject> spawnedChunks = new Dictionary<Vector2Int, GameObject>();
    private bool playerSpawned = false;

    private bool inHole = false;
    private int holeRemaining = 0;
    private int holeStartX = -1;

    void Update()
    {
        Vector2Int currentChunk = GetChunkCoordFromPosition(player.position);
        for (int x = -renderDistance; x <= renderDistance; x++)
        {
            Vector2Int chunkCoord = new Vector2Int(currentChunk.x + x, 0);
            if (!spawnedChunks.ContainsKey(chunkCoord))
            {
                GameObject chunk = GenerateChunk(chunkCoord);
                spawnedChunks.Add(chunkCoord, chunk);
            }
        }
    }

    Vector2Int GetChunkCoordFromPosition(Vector3 pos)
    {
        int x = Mathf.FloorToInt(pos.x / chunkWidth);
        return new Vector2Int(x, 0);
    }

    GameObject GenerateChunk(Vector2Int chunkCoord)
    {
        GameObject chunkObj = new GameObject("Chunk " + chunkCoord.x);
        chunkObj.transform.position = new Vector3(chunkCoord.x * chunkWidth, 0, 0);

        int xGlobal = chunkCoord.x * chunkWidth;
        List<int> topXPositions = new List<int>();
        HashSet<int> holeEdges = new HashSet<int>();

        int x = 0;
        while (x < chunkWidth)
        {
            bool createGround = true;

            if (!inHole && Random.value < holeChance && x <= chunkWidth - fixedHoleLength)
            {
                inHole = true;
                holeRemaining = fixedHoleLength;
                holeStartX = xGlobal + x;
            }

            if (inHole)
            {
                createGround = false;

                if (holeRemaining == fixedHoleLength) holeEdges.Add(xGlobal + x - 1);
                if (holeRemaining == 1) holeEdges.Add(xGlobal + x + 1);

                if (holeRemaining == fixedHoleLength / 2 && platformPrefab != null)
                {
                    float platformX = holeStartX + fixedHoleLength / 2f - 0.5f;
                    Vector3 platformPos = new Vector3(platformX, platformHeight + yOffset + 1, 0);
                    GameObject platform = Instantiate(platformPrefab, platformPos, Quaternion.identity);
                    platform.transform.parent = chunkObj.transform;
                }

                if (lavaPrefab != null)
                {
                    Vector3 lavaPos = new Vector3(xGlobal + x + 0.1f, platformHeight + yOffset + lavaYOffset, -1);
                    GameObject lava = Instantiate(lavaPrefab, lavaPos, Quaternion.identity);
                    lava.transform.parent = chunkObj.transform;

                    for (int y = 0; y < groundThickness + 2; y++)
                    {
                        Vector3 solidPos = new Vector3(xGlobal + x, platformHeight + yOffset - y - 2, 0);
                        GameObject solidBlock = Instantiate(bottomBlockPrefab, solidPos, Quaternion.identity);
                        solidBlock.transform.parent = chunkObj.transform;
                    }
                }

                holeRemaining--;
                if (holeRemaining <= 0) inHole = false;
            }

            if (createGround && Random.value < castleHillChance && chunkWidth - x >= 20)
            {
                int hillHeight = Random.Range(3, 5);
                int hillLength = Random.Range(9, 17);

                int totalHillLength = hillHeight * 2 + hillLength;
                if (x + totalHillLength > chunkWidth - 1) break;

                Vector3 startTop = new Vector3(xGlobal + x, platformHeight + yOffset, 0);
                GameObject startTopBlock = Instantiate(topBlockPrefab, startTop, Quaternion.identity);
                startTopBlock.transform.parent = chunkObj.transform;
                for (int y = 0; y < groundThickness; y++)
                {
                    Vector3 pos = new Vector3(xGlobal + x, platformHeight + yOffset - y - 1, 0);
                    GameObject block = Instantiate(bottomBlockPrefab, pos, Quaternion.identity);
                    block.transform.parent = chunkObj.transform;
                }
                topXPositions.Add(x);
                x++;

                for (int i = 1; i <= hillHeight; i++, x++)
                {
                    Vector3 pos = new Vector3(xGlobal + x, platformHeight + yOffset + i, 0);
                    GameObject slope = Instantiate(slopeUpPrefab, pos, Quaternion.identity);
                    slope.transform.parent = chunkObj.transform;

                    for (int y = 0; y < groundThickness + 2; y++)
                    {
                        Vector3 bpos = new Vector3(xGlobal + x, platformHeight + yOffset + i - y - 1, 0);
                        GameObject bottom = Instantiate(bottomBlockPrefab, bpos, Quaternion.identity);
                        bottom.transform.parent = chunkObj.transform;
                    }
                }

                for (int i = 0; i < hillLength; i++, x++)
                {
                    Vector3 tpos = new Vector3(xGlobal + x, platformHeight + yOffset + hillHeight, 0);
                    GameObject top = Instantiate(topBlockPrefab, tpos, Quaternion.identity);
                    top.transform.parent = chunkObj.transform;

                    for (int y = 0; y < groundThickness + hillHeight; y++)
                    {
                        Vector3 bpos = new Vector3(xGlobal + x, platformHeight + yOffset + hillHeight - y - 1, 0);
                        GameObject bottom = Instantiate(bottomBlockPrefab, bpos, Quaternion.identity);
                        bottom.transform.parent = chunkObj.transform;
                    }
                }

                for (int i = hillHeight; i >= 1; i--, x++)
                {
                    Vector3 pos = new Vector3(xGlobal + x, platformHeight + yOffset + i, 0);
                    GameObject slope = Instantiate(slopeDownPrefab, pos, Quaternion.identity);
                    slope.transform.parent = chunkObj.transform;

                    for (int y = 0; y < groundThickness + 2; y++)
                    {
                        Vector3 bpos = new Vector3(xGlobal + x, platformHeight + yOffset + i - y - 1, 0);
                        GameObject bottom = Instantiate(bottomBlockPrefab, bpos, Quaternion.identity);
                        bottom.transform.parent = chunkObj.transform;
                    }
                }

                continue;
            }

            if (createGround)
            {
                Vector3 topPos = new Vector3(xGlobal + x, platformHeight + yOffset, 0);
                GameObject topBlock = Instantiate(topBlockPrefab, topPos, Quaternion.identity);
                topBlock.transform.parent = chunkObj.transform;

                for (int y = 0; y < groundThickness; y++)
                {
                    Vector3 pos = new Vector3(xGlobal + x, platformHeight + yOffset - y - 1, 0);
                    GameObject block = Instantiate(bottomBlockPrefab, pos, Quaternion.identity);
                    block.transform.parent = chunkObj.transform;
                }

                topXPositions.Add(x);

                if (!playerSpawned && chunkCoord.x == 0 && x > chunkWidth / 4)
                {
                    player.position = new Vector3(xGlobal + x, platformHeight + yOffset + 1.5f, 0);
                    playerSpawned = true;
                }
            }

            x++;
        }

        // Декорації
        int decorationsPlaced = 0;
        int lastDecorationX = -1;
        string lastDecorationType = "";
        int repeatCount = 0;

        while (decorationsPlaced < maxDecorationsPerChunk && topXPositions.Count > 0)
        {
            int index = Random.Range(0, topXPositions.Count);
            int localX = topXPositions[index];
            int posX = xGlobal + localX;
            topXPositions.RemoveAt(index);

            if (holeEdges.Contains(posX)) continue;
            if (Mathf.Abs(posX - lastDecorationX) < 6) continue;

            if (Random.value < decorationChance)
            {
                string decoType = "";
                GameObject decoPrefab = null;
                float rand = Random.value;

                if (rand < 0.25f && torchPrefab != null)
                {
                    decoPrefab = torchPrefab;
                    decoType = "torch";
                }
                else if (rand < 0.5f && statuePrefab != null)
                {
                    decoPrefab = statuePrefab;
                    decoType = "statue";
                }
                else if (rand < 0.75f && flowerPrefab != null)
                {
                    decoPrefab = flowerPrefab;
                    decoType = "flower";
                }
                else if (skullPrefab != null)
                {
                    decoPrefab = skullPrefab;
                    decoType = "skull";
                }

                if (decoPrefab != null)
                {
                    if (decoType == lastDecorationType)
                    {
                        repeatCount++;
                        if (repeatCount > 2) continue;
                    }
                    else
                    {
                        repeatCount = 1;
                        lastDecorationType = decoType;
                    }

                    Vector3 decoPos = new Vector3(posX + 0.5f, platformHeight + yOffset + 0.3f, 1);
                    GameObject deco = Instantiate(decoPrefab, decoPos, Quaternion.identity);
                    deco.transform.parent = chunkObj.transform;
                    lastDecorationX = posX;
                    decorationsPlaced++;
                }
            }
        }

        
        int enemiesSpawned = 0;
        List<int> potentialEnemySpots = new List<int>(topXPositions);
        List<Vector3> enemyPositions = new List<Vector3>();

        while (enemiesSpawned < maxEnemiesPerChunk && potentialEnemySpots.Count > 0)
        {
            int index = Random.Range(0, potentialEnemySpots.Count);
            int localX = potentialEnemySpots[index];
            potentialEnemySpots.RemoveAt(index);

            int posX = xGlobal + localX;
            if (holeEdges.Contains(posX)) continue;

            if (Random.value < enemySpawnChance && enemyPrefab != null)
            {
                Vector3 enemyPos = new Vector3(posX + 0.5f, platformHeight + yOffset + 1, 0);

                // Перевірка на мінімальну відстань
                bool tooClose = false;
                foreach (var pos in enemyPositions)
                {
                    if (Vector3.Distance(pos, enemyPos) < 50f)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (tooClose) continue;

                GameObject enemy = Instantiate(enemyPrefab, enemyPos, Quaternion.identity);
                enemy.transform.parent = chunkObj.transform;
                enemyPositions.Add(enemyPos);
                enemiesSpawned++;
            }
        }


        return chunkObj;
    }
}
