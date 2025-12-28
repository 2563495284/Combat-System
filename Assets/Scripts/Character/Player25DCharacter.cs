using UnityEngine;
using Character3C.Map;

namespace Character3C
{
    /// <summary>
    /// 2.5D 玩家角色管理器
    /// 整合所有2.5D系统组件
    /// </summary>
    [RequireComponent(typeof(Character25DController))]
    [RequireComponent(typeof(InputController))]
    public class Player25DCharacter : MonoBehaviour
    {
        [Header("Sprite渲染")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Animator animator;

        [Header("相机设置")]
        [SerializeField] private Camera25DController cameraController;
        [SerializeField] private bool autoCreateCamera = true;

        private Character25DController controller;
        private InputController inputController;

        private void Awake()
        {
            controller = GetComponent<Character25DController>();
            inputController = GetComponent<InputController>();

            // 自动获取组件
            if (spriteRenderer == null)
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (animator == null)
                animator = GetComponentInChildren<Animator>();

            SetupCamera();
            SetupVisuals();
        }

        private void Start()
        {
            // 确保在可行走的位置生成
            // EnsureValidSpawnPosition();
        }

        /// <summary>
        /// 设置相机
        /// </summary>
        private void SetupCamera()
        {
            if (cameraController == null)
            {
                cameraController = FindFirstObjectByType<Camera25DController>();

                if (cameraController == null && autoCreateCamera)
                {
                    // 创建相机
                    GameObject camObj = new GameObject("Camera 2.5D");
                    Camera cam = camObj.AddComponent<Camera>();
                    cameraController = camObj.AddComponent<Camera25DController>();

                    // 配置相机
                    cam.clearFlags = CameraClearFlags.Skybox;
                    cam.orthographic = true;
                    cam.orthographicSize = 5f;

                    Debug.Log("自动创建了2.5D相机");
                }
            }

            if (cameraController != null)
            {
                cameraController.SetTarget(transform);
            }
        }

        /// <summary>
        /// 设置视觉效果
        /// </summary>
        private void SetupVisuals()
        {
            // 如果没有Sprite Renderer，创建一个
            if (spriteRenderer == null)
            {
                GameObject spriteObj = new GameObject("Sprite");
                spriteObj.transform.SetParent(transform);
                spriteObj.transform.localPosition = Vector3.zero;
                spriteRenderer = spriteObj.AddComponent<SpriteRenderer>();
            }
        }

        /// <summary>
        /// 确保在有效位置生成
        /// </summary>
        private void EnsureValidSpawnPosition()
        {
            if (MapManager.Instance != null)
            {
                Vector3 validPos = MapManager.Instance.GetNearestWalkablePosition(transform.position);
                if (validPos != transform.position)
                {
                    transform.position = validPos;
                    Debug.Log($"角色位置已调整到可行走位置: {validPos}");
                }
            }
        }

        private void Update()
        {
            UpdateAnimation();
        }

        /// <summary>
        /// 更新动画
        /// </summary>
        private void UpdateAnimation()
        {
            if (animator == null || controller == null) return;

            var blackboard = controller.Blackboard;

            // 更新动画参数
            float speed = new Vector2(blackboard.Velocity.x, blackboard.Velocity.z).magnitude;
            animator.SetFloat("Speed", speed);
            animator.SetFloat("VerticalSpeed", blackboard.Velocity.y);
            animator.SetBool("IsGrounded", blackboard.IsGrounded);

            // 根据移动方向翻转Sprite（可选）
            if (blackboard.FacingDirection.x != 0 && spriteRenderer != null)
            {
                spriteRenderer.flipX = blackboard.FacingDirection.x > 0;
            }
        }

        /// <summary>
        /// 获取角色控制器
        /// </summary>
        public Character25DController GetController() => controller;

        /// <summary>
        /// 获取黑板
        /// </summary>
        public CharacterBlackboard25D GetBlackboard() => controller?.Blackboard;

        /// <summary>
        /// 设置Sprite
        /// </summary>
        public void SetSprite(Sprite sprite)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = sprite;
            }
        }
    }
}

