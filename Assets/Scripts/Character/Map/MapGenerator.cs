using UnityEngine;
using System.Collections.Generic;

namespace Character3C.Map
{
    /// <summary>
    /// 地图生成器
    /// 程序化生成2.5D地图
    /// </summary>
    public class MapGenerator : MonoBehaviour
    {
        [Header("地图引用")]
        [SerializeField] private MapGrid mapGrid;

        [Header("地块预制体")]
        [SerializeField] private GameObject groundPrefab;
        [SerializeField] private GameObject wallPrefab;
        [SerializeField] private GameObject waterPrefab;
        [SerializeField] private GameObject grassPrefab;
        [SerializeField] private GameObject roadPrefab;

        [Header("生成设置")]
        [SerializeField] private int seed = 0;
        [SerializeField] private bool useRandomSeed = true;

        [Header("地形噪声")]
        [SerializeField] private float noiseScale = 10f;
        [SerializeField] private float waterThreshold = 0.3f;
        [SerializeField] private float grassThreshold = 0.6f;

        [Header("障碍物")]
        [SerializeField] private float wallDensity = 0.1f;
        [SerializeField] private int minWallGroupSize = 2;
        [SerializeField] private int maxWallGroupSize = 5;

        private Transform tilesParent;

        private void Awake()
        {
            if (mapGrid == null)
            {
                mapGrid = GetComponent<MapGrid>();
            }

            // 创建地块容器
            tilesParent = new GameObject("Tiles").transform;
            tilesParent.SetParent(transform);
        }

        /// <summary>
        /// 生成地图
        /// </summary>
        public void GenerateMap()
        {
            if (mapGrid == null)
            {
                Debug.LogError("MapGrid 未设置！");
                return;
            }

            // 清空现有地图
            ClearMap();

            // 设置随机种子
            if (useRandomSeed)
            {
                seed = Random.Range(0, 10000);
            }
            Random.InitState(seed);

            // 生成地形
            GenerateTerrain();

            // 生成障碍物
            GenerateObstacles();

            // 生成地块对象
            InstantiateTiles();

            Debug.Log($"地图生成完成 - 种子: {seed}");
        }

        /// <summary>
        /// 生成地形
        /// </summary>
        private void GenerateTerrain()
        {
            float offsetX = Random.Range(0f, 1000f);
            float offsetZ = Random.Range(0f, 1000f);

            for (int x = 0; x < mapGrid.Width; x++)
            {
                for (int z = 0; z < mapGrid.Height; z++)
                {
                    Vector2Int gridPos = new Vector2Int(x, z);
                    GridCell cell = mapGrid.GetCell(gridPos);

                    if (cell == null) continue;

                    // 使用Perlin噪声生成地形
                    float noiseValue = Mathf.PerlinNoise(
                        (x + offsetX) / noiseScale,
                        (z + offsetZ) / noiseScale
                    );

                    // 根据噪声值决定地块类型
                    if (noiseValue < waterThreshold)
                    {
                        cell.tileType = TileType.Water;
                        cell.isWalkable = false;
                    }
                    else if (noiseValue < grassThreshold)
                    {
                        cell.tileType = TileType.Grass;
                        cell.isWalkable = true;
                    }
                    else
                    {
                        cell.tileType = TileType.Ground;
                        cell.isWalkable = true;
                    }
                }
            }
        }

        /// <summary>
        /// 生成障碍物
        /// </summary>
        private void GenerateObstacles()
        {
            int wallCount = Mathf.FloorToInt(mapGrid.Width * mapGrid.Height * wallDensity);

            for (int i = 0; i < wallCount; i++)
            {
                int x = Random.Range(1, mapGrid.Width - 1);
                int z = Random.Range(1, mapGrid.Height - 1);
                Vector2Int gridPos = new Vector2Int(x, z);

                GridCell cell = mapGrid.GetCell(gridPos);
                if (cell != null && cell.tileType == TileType.Ground)
                {
                    // 生成墙壁群组
                    int groupSize = Random.Range(minWallGroupSize, maxWallGroupSize + 1);
                    GenerateWallGroup(gridPos, groupSize);
                }
            }
        }

        /// <summary>
        /// 生成墙壁群组
        /// </summary>
        private void GenerateWallGroup(Vector2Int startPos, int size)
        {
            List<Vector2Int> group = new List<Vector2Int> { startPos };

            for (int i = 1; i < size; i++)
            {
                if (group.Count == 0) break;

                // 从已有的墙壁中随机选择一个
                Vector2Int current = group[Random.Range(0, group.Count)];

                // 尝试在相邻位置添加墙壁
                Vector2Int[] directions = new Vector2Int[]
                {
                    new Vector2Int(1, 0),
                    new Vector2Int(-1, 0),
                    new Vector2Int(0, 1),
                    new Vector2Int(0, -1)
                };

                Vector2Int dir = directions[Random.Range(0, directions.Length)];
                Vector2Int newPos = current + dir;

                GridCell cell = mapGrid.GetCell(newPos);
                if (cell != null && cell.tileType == TileType.Ground)
                {
                    cell.tileType = TileType.Wall;
                    cell.isWalkable = false;
                    group.Add(newPos);
                }
            }
        }

        /// <summary>
        /// 实例化地块对象
        /// </summary>
        private void InstantiateTiles()
        {
            // 计算单元格中心偏移（预制体锚点通常在中心）
            Vector3 centerOffset = new Vector3(mapGrid.CellSize * 0.5f, 0, mapGrid.CellSize * 0.5f);

            for (int x = 0; x < mapGrid.Width; x++)
            {
                for (int z = 0; z < mapGrid.Height; z++)
                {
                    Vector2Int gridPos = new Vector2Int(x, z);
                    GridCell cell = mapGrid.GetCell(gridPos);

                    if (cell == null) continue;

                    GameObject prefab = GetPrefabForTileType(cell.tileType);
                    if (prefab != null)
                    {
                        // 使用网格单元中心位置实例化预制体
                        Vector3 spawnPosition = cell.worldPosition + centerOffset;
                        GameObject tile = Instantiate(prefab, spawnPosition, Quaternion.identity, tilesParent);
                        tile.name = $"Tile_{x}_{z}_{cell.tileType}";
                        cell.tileObject = tile;
                    }
                }
            }
        }

        /// <summary>
        /// 根据地块类型获取预制体
        /// </summary>
        private GameObject GetPrefabForTileType(TileType type)
        {
            switch (type)
            {
                case TileType.Ground:
                    return groundPrefab;
                case TileType.Wall:
                    return wallPrefab;
                case TileType.Water:
                    return waterPrefab;
                case TileType.Grass:
                    return grassPrefab;
                case TileType.Road:
                    return roadPrefab;
                default:
                    return groundPrefab;
            }
        }

        /// <summary>
        /// 清空地图
        /// </summary>
        private void ClearMap()
        {
            if (tilesParent != null)
            {
                foreach (Transform child in tilesParent)
                {
                    Destroy(child.gameObject);
                }
            }
        }

        /// <summary>
        /// 生成简单测试地图
        /// </summary>
        public void GenerateTestMap()
        {
            ClearMap();

            // 创建一个简单的棋盘格测试地图
            for (int x = 0; x < mapGrid.Width; x++)
            {
                for (int z = 0; z < mapGrid.Height; z++)
                {
                    Vector2Int gridPos = new Vector2Int(x, z);
                    GridCell cell = mapGrid.GetCell(gridPos);

                    if (cell == null) continue;

                    // 棋盘格图案
                    bool isEven = (x + z) % 2 == 0;
                    cell.tileType = isEven ? TileType.Ground : TileType.Grass;
                    cell.isWalkable = true;

                    // 边界墙壁
                    if (x == 0 || x == mapGrid.Width - 1 || z == 0 || z == mapGrid.Height - 1)
                    {
                        cell.tileType = TileType.Wall;
                        cell.isWalkable = false;
                    }
                }
            }

            InstantiateTiles();
            Debug.Log("测试地图生成完成");
        }
    }
}

