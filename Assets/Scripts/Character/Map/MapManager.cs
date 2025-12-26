using UnityEngine;
using System.Collections.Generic;

namespace Character3C.Map
{
    /// <summary>
    /// 地图管理器
    /// 统一管理地图系统
    /// </summary>
    public class MapManager : MonoBehaviour
    {
        private static MapManager instance;
        public static MapManager Instance => instance;

        [Header("地图组件")]
        [SerializeField] private MapGrid mapGrid;
        [SerializeField] private MapGenerator mapGenerator;

        [Header("自动生成")]
        [SerializeField] private bool generateOnStart = true;
        [SerializeField] private bool useTestMap = false;

        private void Awake()
        {
            // 单例模式
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;

            // 获取组件
            if (mapGrid == null)
                mapGrid = GetComponent<MapGrid>();
            if (mapGenerator == null)
                mapGenerator = GetComponent<MapGenerator>();
        }

        private void Start()
        {
            if (generateOnStart)
            {
                if (useTestMap)
                {
                    GenerateTestMap();
                }
                else
                {
                    GenerateMap();
                }
            }
        }

        /// <summary>
        /// 生成地图
        /// </summary>
        public void GenerateMap()
        {
            if (mapGenerator != null)
            {
                mapGenerator.GenerateMap();
            }
            else
            {
                Debug.LogWarning("MapGenerator 未设置");
            }
        }

        /// <summary>
        /// 生成测试地图
        /// </summary>
        public void GenerateTestMap()
        {
            if (mapGenerator != null)
            {
                mapGenerator.GenerateTestMap();
            }
            else
            {
                Debug.LogWarning("MapGenerator 未设置");
            }
        }

        /// <summary>
        /// 获取地图网格
        /// </summary>
        public MapGrid GetMapGrid()
        {
            return mapGrid;
        }

        /// <summary>
        /// 检查位置是否可行走
        /// </summary>
        public bool IsWalkable(Vector3 worldPosition)
        {
            if (mapGrid == null) return true;

            Vector2Int gridPos = mapGrid.WorldToGridPosition(worldPosition);
            return mapGrid.IsWalkable(gridPos);
        }

        /// <summary>
        /// 获取最近的可行走位置
        /// </summary>
        public Vector3 GetNearestWalkablePosition(Vector3 worldPosition, float searchRadius = 5f)
        {
            if (mapGrid == null) return worldPosition;

            Vector2Int startGridPos = mapGrid.WorldToGridPosition(worldPosition);

            // 如果当前位置可行走，直接返回
            if (mapGrid.IsWalkable(startGridPos))
            {
                return mapGrid.GridToWorldPosition(startGridPos);
            }

            // 螺旋搜索最近的可行走位置
            int maxSearchDistance = Mathf.CeilToInt(searchRadius / mapGrid.CellSize);

            for (int distance = 1; distance <= maxSearchDistance; distance++)
            {
                for (int dx = -distance; dx <= distance; dx++)
                {
                    for (int dz = -distance; dz <= distance; dz++)
                    {
                        if (Mathf.Abs(dx) != distance && Mathf.Abs(dz) != distance)
                            continue;

                        Vector2Int checkPos = startGridPos + new Vector2Int(dx, dz);
                        if (mapGrid.IsWalkable(checkPos))
                        {
                            return mapGrid.GridToWorldPosition(checkPos);
                        }
                    }
                }
            }

            // 如果没找到，返回原位置
            return worldPosition;
        }

        /// <summary>
        /// 获取路径（简单A*寻路）
        /// </summary>
        public List<Vector3> FindPath(Vector3 startWorld, Vector3 endWorld)
        {
            if (mapGrid == null)
                return new List<Vector3> { startWorld, endWorld };

            Vector2Int startGrid = mapGrid.WorldToGridPosition(startWorld);
            Vector2Int endGrid = mapGrid.WorldToGridPosition(endWorld);

            List<Vector2Int> gridPath = AStarPathfinding(startGrid, endGrid);

            // 转换为世界坐标
            List<Vector3> worldPath = new List<Vector3>();
            foreach (var gridPos in gridPath)
            {
                worldPath.Add(mapGrid.GridToWorldPosition(gridPos));
            }

            return worldPath;
        }

        /// <summary>
        /// A* 寻路算法
        /// </summary>
        private List<Vector2Int> AStarPathfinding(Vector2Int start, Vector2Int end)
        {
            List<Vector2Int> path = new List<Vector2Int>();

            if (!mapGrid.IsValidGridPosition(start) || !mapGrid.IsValidGridPosition(end))
                return path;

            Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();
            Dictionary<Vector2Int, float> gScore = new Dictionary<Vector2Int, float>();
            Dictionary<Vector2Int, float> fScore = new Dictionary<Vector2Int, float>();

            List<Vector2Int> openSet = new List<Vector2Int> { start };
            gScore[start] = 0;
            fScore[start] = Heuristic(start, end);

            while (openSet.Count > 0)
            {
                // 找到fScore最小的节点
                Vector2Int current = openSet[0];
                float minF = fScore[current];
                foreach (var pos in openSet)
                {
                    if (fScore.ContainsKey(pos) && fScore[pos] < minF)
                    {
                        current = pos;
                        minF = fScore[pos];
                    }
                }

                if (current == end)
                {
                    // 重建路径
                    return ReconstructPath(cameFrom, current);
                }

                openSet.Remove(current);

                // 检查邻居
                foreach (var neighbor in mapGrid.GetNeighbors(current, false))
                {
                    if (!neighbor.isWalkable)
                        continue;

                    Vector2Int neighborPos = neighbor.gridPosition;
                    float tentativeGScore = gScore[current] + 1;

                    if (!gScore.ContainsKey(neighborPos) || tentativeGScore < gScore[neighborPos])
                    {
                        cameFrom[neighborPos] = current;
                        gScore[neighborPos] = tentativeGScore;
                        fScore[neighborPos] = tentativeGScore + Heuristic(neighborPos, end);

                        if (!openSet.Contains(neighborPos))
                        {
                            openSet.Add(neighborPos);
                        }
                    }
                }
            }

            return path; // 没找到路径
        }

        /// <summary>
        /// 启发式函数（曼哈顿距离）
        /// </summary>
        private float Heuristic(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }

        /// <summary>
        /// 重建路径
        /// </summary>
        private List<Vector2Int> ReconstructPath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int current)
        {
            List<Vector2Int> path = new List<Vector2Int> { current };

            while (cameFrom.ContainsKey(current))
            {
                current = cameFrom[current];
                path.Insert(0, current);
            }

            return path;
        }
    }
}

