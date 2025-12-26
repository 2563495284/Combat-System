using UnityEngine;
using Character3C.Tasks;

namespace Character3C
{
    /// <summary>
    /// 玩家角色管理器
    /// 整合角色控制、输入和相机系统
    /// </summary>
    [RequireComponent(typeof(CharacterController2D))]
    [RequireComponent(typeof(InputController))]
    public class PlayerCharacter : MonoBehaviour
    {
        [Header("相机设置")]
        [SerializeField] private CameraController mainCamera;
        [SerializeField] private bool createCameraIfNull = true;

        [Header("任务管理")]
        [SerializeField] private bool autoStartPlayerControl = true;

        private CharacterController2D controller;
        private InputController inputController;
        private PlayerControlTask playerTask;
        private CameraFollowTask cameraTask;

        private void Awake()
        {
            // 获取组件
            controller = GetComponent<CharacterController2D>();
            inputController = GetComponent<InputController>();

            // 设置相机
            SetupCamera();
        }

        private void Start()
        {
            // 启动玩家控制任务
            if (autoStartPlayerControl)
            {
                StartPlayerControl();
            }

            // 启动相机跟随
            if (mainCamera != null)
            {
                StartCameraFollow();
            }
        }

        /// <summary>
        /// 设置相机
        /// </summary>
        private void SetupCamera()
        {
            // 如果没有指定相机，尝试查找
            if (mainCamera == null)
            {
                mainCamera = FindFirstObjectByType<CameraController>();

                // 如果还是没有，创建一个
                if (mainCamera == null && createCameraIfNull)
                {
                    var camObj = Camera.main != null ? Camera.main.gameObject : new GameObject("Main Camera");
                    camObj.tag = "MainCamera";

                    if (!camObj.TryGetComponent<Camera>(out _))
                    {
                        camObj.AddComponent<Camera>();
                    }

                    mainCamera = camObj.AddComponent<CameraController>();
                    Debug.Log("自动创建了相机控制器");
                }
            }
        }

        /// <summary>
        /// 启动玩家控制
        /// </summary>
        public void StartPlayerControl()
        {
            if (playerTask == null)
            {
                playerTask = new PlayerControlTask(controller);
            }

            controller.SetTask(playerTask);
            Debug.Log("玩家控制已启动");
        }

        /// <summary>
        /// 停止玩家控制
        /// </summary>
        public void StopPlayerControl()
        {
            playerTask?.Stop();
            Debug.Log("玩家控制已停止");
        }

        /// <summary>
        /// 启动相机跟随
        /// </summary>
        public void StartCameraFollow()
        {
            if (mainCamera == null)
            {
                Debug.LogWarning("没有相机控制器，无法启动相机跟随");
                return;
            }

            if (cameraTask == null)
            {
                cameraTask = new CameraFollowTask(mainCamera, transform);
            }

            cameraTask.Start();
            Debug.Log("相机跟随已启动");
        }

        /// <summary>
        /// 停止相机跟随
        /// </summary>
        public void StopCameraFollow()
        {
            cameraTask?.Stop();
            Debug.Log("相机跟随已停止");
        }

        /// <summary>
        /// 触发相机震动
        /// </summary>
        public void TriggerCameraShake(float intensity = 0.2f, float duration = 0.3f)
        {
            cameraTask?.TriggerShake(intensity, duration);
        }

        /// <summary>
        /// 设置相机边界
        /// </summary>
        public void SetCameraBounds(Vector2 min, Vector2 max)
        {
            cameraTask?.SetBounds(min, max);
        }

        /// <summary>
        /// 获取角色控制器
        /// </summary>
        public CharacterController2D GetController() => controller;

        /// <summary>
        /// 获取输入控制器
        /// </summary>
        public InputController GetInputController() => inputController;

        /// <summary>
        /// 获取角色黑板数据
        /// </summary>
        public CharacterBlackboard GetBlackboard() => controller?.Blackboard;
    }
}

