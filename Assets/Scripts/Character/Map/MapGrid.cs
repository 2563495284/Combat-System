using UnityEngine;
using System.Collections.Generic;

namespace Character3C.Map
{
    /// <summary>
    /// 地图网格单元
    /// </summary>
    [System.Serializable]
    public class GridCell
    {
        public Vector2Int gridPosition;
        public Vector3 worldPosition;
        public TileType tileType;
        public bool isWalkable = true;
        public int height = 0; // 高度层级
        public GameObject tileObject;

        public GridCell(Vector2Int gridPos, Vector3 worldPos)
        {
            gridPosition = gridPos;
            worldPosition = worldPos;
            tileType = TileType.Ground;
        }
    }

    /// <summary>
    /// 地块类型
    /// </summary>
    public enum TileType
    {
        Ground,     // 普通地面
        Wall,       // 墙壁
        Water,      // 水面
        Obstacle,   // 障碍物
        Grass,      // 草地
        Road,       // 道路
        Bridge,     // 桥
        Hole,       // 坑洞
    }

    /// <summary>
    /// 地图网格系统
    /// 管理2.5D地图的网格数据
    /// </summary>
    public class MapGrid : MonoBehaviour
    {
        [Header("网格设置")]
        [SerializeField] private int width = 50;
        [SerializeField] private int height = 50;
        [SerializeField] private float cellSize = 1f;
        [SerializeField] private Vector3 origin = Vector3.zero;

        [Header("可视化")]
        [SerializeField] private bool showGrid = true;
        [SerializeField] private Color gridColor = new Color(0, 1, 0, 0.3f);

        // 网格数据
        private GridCell[,] grid;
        private Dictionary<Vector2Int, GridCell> cellLookup;

        public int Width => width;
        public int Height => height;
        public float CellSize => cellSize;
        public Vector3 Origin => origin;

        private void Awake()
        {
            InitializeGrid();
        }

        /// <summary>
        /// 初始化网格
        /// </summary>
        public void InitializeGrid()
        {
            grid = new GridCell[width, height];
            cellLookup = new Dictionary<Vector2Int, GridCell>();

            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < height; z++)
                {
                    Vector2Int gridPos = new Vector2Int(x, z);
                    Vector3 worldPos = GridToWorldPosition(gridPos);

                    GridCell cell = new GridCell(gridPos, worldPos);
                    grid[x, z] = cell;
                    cellLookup[gridPos] = cell;
                }
            }

            Debug.Log($"地图网格初始化完成: {width}x{height}");
        }

        /// <summary>
        /// 网格坐标转世界坐标
        /// </summary>
        public Vector3 GridToWorldPosition(Vector2Int gridPos)
        {
            return origin + new Vector3(gridPos.x * cellSize, 0, gridPos.y * cellSize);
        }

        /// <summary>
        /// 世界坐标转网格坐标
        /// </summary>
        public Vector2Int WorldToGridPosition(Vector3 worldPos)
        {
            Vector3 relative = worldPos - origin;
            int x = Mathf.FloorToInt(relative.x / cellSize);
            int z = Mathf.FloorToInt(relative.z / cellSize);
            return new Vector2Int(x, z);
        }

        /// <summary>
        /// 获取网格单元
        /// </summary>
        public GridCell GetCell(Vector2Int gridPos)
        {
            if (IsValidGridPosition(gridPos))
            {
                return grid[gridPos.x, gridPos.y];
            }
            return null;
        }

        /// <summary>
        /// 获取网格单元（通过世界坐标）
        /// </summary>
        public GridCell GetCellFromWorld(Vector3 worldPos)
        {
            Vector2Int gridPos = WorldToGridPosition(worldPos);
            return GetCell(gridPos);
        }

        /// <summary>
        /// 设置地块类型
        /// </summary>
        public void SetTileType(Vector2Int gridPos, TileType type)
        {
            GridCell cell = GetCell(gridPos);
            if (cell != null)
            {
                cell.tileType = type;
                cell.isWalkable = IsWalkableTileType(type);
            }
        }

        /// <summary>
        /// 检查地块类型是否可行走
        /// </summary>
        private bool IsWalkableTileType(TileType type)
        {
            switch (type)
            {
                case TileType.Wall:
                case TileType.Water:
                case TileType.Hole:
                    return false;
                default:
                    return true;
            }
        }

        /// <summary>
        /// 检查网格位置是否有效
        /// </summary>
        public bool IsValidGridPosition(Vector2Int gridPos)
        {
            return gridPos.x >= 0 && gridPos.x < width &&
                   gridPos.y >= 0 && gridPos.y < height;
        }

        /// <summary>
        /// 检查位置是否可行走
        /// </summary>
        public bool IsWalkable(Vector2Int gridPos)
        {
            GridCell cell = GetCell(gridPos);
            return cell != null && cell.isWalkable;
        }

        /// <summary>
        /// 获取相邻单元格
        /// </summary>
        public List<GridCell> GetNeighbors(Vector2Int gridPos, bool includeDiagonal = false)
        {
            List<GridCell> neighbors = new List<GridCell>();

            // 四个方向
            Vector2Int[] directions = new Vector2Int[]
            {
                new Vector2Int(0, 1),   // 上
                new Vector2Int(1, 0),   // 右
                new Vector2Int(0, -1),  // 下
                new Vector2Int(-1, 0),  // 左
            };

            foreach (var dir in directions)
            {
                Vector2Int neighborPos = gridPos + dir;
                GridCell neighbor = GetCell(neighborPos);
                if (neighbor != null)
                {
                    neighbors.Add(neighbor);
                }
            }

            // 对角线方向
            if (includeDiagonal)
            {
                Vector2Int[] diagonalDirections = new Vector2Int[]
                {
                    new Vector2Int(1, 1),   // 右上
                    new Vector2Int(1, -1),  // 右下
                    new Vector2Int(-1, -1), // 左下
                    new Vector2Int(-1, 1),  // 左上
                };

                foreach (var dir in diagonalDirections)
                {
                    Vector2Int neighborPos = gridPos + dir;
                    GridCell neighbor = GetCell(neighborPos);
                    if (neighbor != null)
                    {
                        neighbors.Add(neighbor);
                    }
                }
            }

            return neighbors;
        }

        /// <summary>
        /// 清空网格
        /// </summary>
        public void Clear()
        {
            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < height; z++)
                {
                    GridCell cell = grid[x, z];
                    if (cell != null && cell.tileObject != null)
                    {
                        Destroy(cell.tileObject);
                    }
                }
            }

            InitializeGrid();
        }

        private void OnDrawGizmos()
        {
            if (!showGrid) return;

            Gizmos.color = gridColor;

            // 绘制网格线
            for (int x = 0; x <= width; x++)
            {
                Vector3 start = origin + new Vector3(x * cellSize, 0, 0);
                Vector3 end = origin + new Vector3(x * cellSize, 0, height * cellSize);
                Gizmos.DrawLine(start, end);
            }

            for (int z = 0; z <= height; z++)
            {
                Vector3 start = origin + new Vector3(0, 0, z * cellSize);
                Vector3 end = origin + new Vector3(width * cellSize, 0, z * cellSize);
                Gizmos.DrawLine(start, end);
            }
        }
    }
}

