using UnityEngine;
using Character3C.Map;
using System.Collections.Generic;

namespace Character3C
{
    /// <summary>
    /// 2.5D 角色系统使用示例
    /// 演示完整的2.5D游戏场景设置
    /// </summary>
    public class Character25DExample : MonoBehaviour
    {
        [Header("自动设置场景")]
        [SerializeField] private bool autoSetupScene = true;

        [Header("角色设置")]
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private Vector3 playerSpawnPosition = new Vector3(5, 1, 5);

        [Header("地图设置")]
        // [SerializeField] private int mapWidth = 30;
        // [SerializeField] private int mapHeight = 30;
        [SerializeField] private bool useTestMap = true;

        [Header("调试")]
        [SerializeField] private bool enableDebugKeys = true;
        [SerializeField] private bool showPathfinding = false;

        private Player25DCharacter player;
        private MapManager mapManager;
        private Vector3 pathfindingTarget;
        private List<Vector3> currentPath;

        private void Start()
        {
            if (autoSetupScene)
            {
                SetupCompleteScene();
            }
        }

        /// <summary>
        /// 自动设置完整场景
        /// </summary>
        private void SetupCompleteScene()
        {
            Debug.Log("=== 开始设置2.5D场景 ===");

            // 1. 设置地图
            SetupMap();

            // 2. 设置玩家
            SetupPlayer();

            // 3. 设置相机
            SetupCamera();

            // 4. 设置光照
            SetupLighting();

            Debug.Log("=== 场景设置完成 ===");
            Debug.Log("控制:");
            Debug.Log("  移动: WASD");
            Debug.Log("  跳跃: Space");
            Debug.Log("\n调试按键:");
            Debug.Log("  F1: 重新生成地图");
            Debug.Log("  F2: 切换测试地图");
            Debug.Log("  F3: 显示/隐藏网格");
            Debug.Log("  鼠标右键: 设置寻路目标（演示）");
        }

        /// <summary>
        /// 设置地图
        /// </summary>
        private void SetupMap()
        {
            // 查找或创建地图管理器
            mapManager = MapManager.Instance;

            if (mapManager == null)
            {
                GameObject mapObj = new GameObject("MapSystem");

                // 添加组件
                MapGrid grid = mapObj.AddComponent<MapGrid>();
                MapGenerator generator = mapObj.AddComponent<MapGenerator>();
                mapManager = mapObj.AddComponent<MapManager>();

                // 配置网格
                // 通过反射设置私有字段（仅用于示例，生产环境应该公开属性）
                Debug.Log("地图系统已创建，请在Inspector中配置地块预制体");
            }

            Debug.Log("✓ 地图设置完成");
        }

        /// <summary>
        /// 设置玩家
        /// </summary>
        private void SetupPlayer()
        {
            if (playerPrefab != null)
            {
                GameObject playerObj = Instantiate(playerPrefab, playerSpawnPosition, Quaternion.identity);
                player = playerObj.GetComponent<Player25DCharacter>();
            }
            else
            {
                // 手动创建玩家
                GameObject playerObj = new GameObject("Player");
                playerObj.transform.position = playerSpawnPosition;

                // 添加碰撞体
                Rigidbody rb = playerObj.AddComponent<Rigidbody>();
                rb.useGravity = false;
                rb.constraints = RigidbodyConstraints.FreezeRotation;

                CapsuleCollider col = playerObj.AddComponent<CapsuleCollider>();
                col.height = 2f;
                col.radius = 0.5f;

                // 添加角色组件
                player = playerObj.AddComponent<Player25DCharacter>();
                playerObj.AddComponent<InputController>();

                // 创建Sprite子对象
                GameObject spriteObj = new GameObject("Sprite");
                spriteObj.transform.SetParent(playerObj.transform);
                spriteObj.transform.localPosition = Vector3.zero;

                SpriteRenderer sr = spriteObj.AddComponent<SpriteRenderer>();
                spriteObj.AddComponent<Billboard>();

                // 创建简单的占位符Sprite
                Texture2D tex = new Texture2D(32, 64);
                Color[] pixels = new Color[32 * 64];
                for (int i = 0; i < pixels.Length; i++)
                {
                    pixels[i] = Color.cyan;
                }
                tex.SetPixels(pixels);
                tex.Apply();

                Sprite sprite = Sprite.Create(tex, new Rect(0, 0, 32, 64), new Vector2(0.5f, 0));
                sr.sprite = sprite;

                Debug.Log("玩家已创建（使用占位符Sprite）");
            }

            Debug.Log("✓ 玩家设置完成");
        }

        /// <summary>
        /// 设置相机
        /// </summary>
        private void SetupCamera()
        {
            Camera25DController camController = FindFirstObjectByType<Camera25DController>();

            if (camController == null && Camera.main != null)
            {
                camController = Camera.main.gameObject.AddComponent<Camera25DController>();

                // 配置相机
                Camera cam = Camera.main;
                cam.orthographic = true;
                cam.orthographicSize = 8f;

                Debug.Log("相机控制器已添加到主相机");
            }

            if (camController != null && player != null)
            {
                camController.SetTarget(player.transform);
            }

            Debug.Log("✓ 相机设置完成");
        }

        /// <summary>
        /// 设置光照
        /// </summary>
        private void SetupLighting()
        {
            // 设置环境光
            RenderSettings.ambientLight = new Color(0.7f, 0.7f, 0.7f);

            // 查找或创建方向光
            Light dirLight = FindFirstObjectByType<Light>();
            if (dirLight == null)
            {
                GameObject lightObj = new GameObject("Directional Light");
                dirLight = lightObj.AddComponent<Light>();
                dirLight.type = LightType.Directional;
                dirLight.transform.rotation = Quaternion.Euler(50, -30, 0);

                Debug.Log("方向光已创建");
            }

            Debug.Log("✓ 光照设置完成");
        }

        private void Update()
        {
            if (!enableDebugKeys) return;

            HandleDebugInput();
        }

        /// <summary>
        /// 处理调试输入
        /// </summary>
        private void HandleDebugInput()
        {
            // F1: 重新生成地图
            if (Input.GetKeyDown(KeyCode.F1))
            {
                if (mapManager != null)
                {
                    mapManager.GenerateMap();
                    Debug.Log("地图已重新生成");
                }
            }

            // F2: 切换测试地图
            if (Input.GetKeyDown(KeyCode.F2))
            {
                useTestMap = !useTestMap;
                if (mapManager != null)
                {
                    if (useTestMap)
                    {
                        mapManager.GenerateTestMap();
                        Debug.Log("切换到测试地图");
                    }
                    else
                    {
                        mapManager.GenerateMap();
                        Debug.Log("切换到随机地图");
                    }
                }
            }

            // F3: 显示/隐藏网格
            if (Input.GetKeyDown(KeyCode.F3))
            {
                MapGrid grid = FindFirstObjectByType<MapGrid>();
                if (grid != null)
                {
                    // 切换显示状态
                    Debug.Log("网格显示切换（请在Scene视图中查看）");
                }
            }

            // 鼠标右键: 寻路演示
            if (Input.GetMouseButtonDown(1) && showPathfinding)
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    pathfindingTarget = hit.point;

                    if (mapManager != null && player != null)
                    {
                        currentPath = mapManager.FindPath(player.transform.position, pathfindingTarget);
                        Debug.Log($"寻路完成: {currentPath.Count} 个路点");
                    }
                }
            }
        }

        private void OnDrawGizmos()
        {
            if (!showPathfinding || currentPath == null || currentPath.Count == 0)
                return;

            // 绘制路径
            Gizmos.color = Color.yellow;
            for (int i = 0; i < currentPath.Count - 1; i++)
            {
                Vector3 start = currentPath[i] + Vector3.up * 0.5f;
                Vector3 end = currentPath[i + 1] + Vector3.up * 0.5f;
                Gizmos.DrawLine(start, end);
                Gizmos.DrawSphere(start, 0.2f);
            }

            // 绘制目标点
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(pathfindingTarget + Vector3.up * 0.5f, 0.3f);
        }
    }
}

