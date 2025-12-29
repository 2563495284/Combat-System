using UnityEngine;

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

            SetupVisuals();
        }

        private void Start()
        {

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

            // 更新动画参数（XY平面系统）
            float speed = Mathf.Abs(blackboard.Velocity.x); // X轴是水平移动
            animator.SetFloat("Speed", speed);
            animator.SetFloat("VerticalSpeed", blackboard.Velocity.y); // Y轴是垂直（跳跃/重力）
            animator.SetBool("IsGrounded", blackboard.IsGrounded); // 使用实际的地面检测结果

            // 根据移动方向翻转Sprite（可选）
            if (blackboard.FacingDirection.x != 0 && spriteRenderer != null)
            {
                spriteRenderer.flipX = blackboard.FacingDirection.x < 0f;
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

