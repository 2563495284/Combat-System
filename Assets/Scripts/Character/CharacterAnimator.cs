using UnityEngine;

namespace Character3C
{
    /// <summary>
    /// 角色动画管理器
    /// 负责管理 2D 帧动画播放
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class CharacterAnimator : MonoBehaviour
    {
        [Header("动画参数名称")]
        [SerializeField] private string moveSpeedParam = "MoveSpeed";
        [SerializeField] private string verticalVelocityParam = "VerticalVelocity";
        [SerializeField] private string isGroundedParam = "IsGrounded";
        [SerializeField] private string isDashingParam = "IsDashing";
        [SerializeField] private string isAttackingParam = "IsAttacking";
        [SerializeField] private string comboIndexParam = "ComboIndex";

        [Header("动画触发器")]
        [SerializeField] private string jumpTrigger = "Jump";
        [SerializeField] private string dashTrigger = "Dash";
        [SerializeField] private string attackTrigger = "Attack";
        [SerializeField] private string hurtTrigger = "Hurt";
        [SerializeField] private string dieTrigger = "Die";

        [Header("Sprite渲染器（可选）")]
        [SerializeField] private SpriteRenderer spriteRenderer;

        private Animator animator;

        // 动画状态缓存
        private int moveSpeedHash;
        private int verticalVelocityHash;
        private int isGroundedHash;
        private int isDashingHash;
        private int isAttackingHash;
        private int comboIndexHash;
        private int jumpHash;
        private int dashHash;
        private int attackHash;
        private int hurtHash;
        private int dieHash;

        private void Awake()
        {
            animator = GetComponent<Animator>();

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            // 缓存动画参数哈希值
            moveSpeedHash = Animator.StringToHash(moveSpeedParam);
            verticalVelocityHash = Animator.StringToHash(verticalVelocityParam);
            isGroundedHash = Animator.StringToHash(isGroundedParam);
            isDashingHash = Animator.StringToHash(isDashingParam);
            isAttackingHash = Animator.StringToHash(isAttackingParam);
            comboIndexHash = Animator.StringToHash(comboIndexParam);
            jumpHash = Animator.StringToHash(jumpTrigger);
            dashHash = Animator.StringToHash(dashTrigger);
            attackHash = Animator.StringToHash(attackTrigger);
            hurtHash = Animator.StringToHash(hurtTrigger);
            dieHash = Animator.StringToHash(dieTrigger);
        }

        /// <summary>
        /// 更新动画状态
        /// </summary>
        public void UpdateAnimations(CharacterBlackboard blackboard)
        {
            if (animator == null) return;

            // 更新移动速度
            float moveSpeed = Mathf.Abs(blackboard.Velocity.x);
            animator.SetFloat(moveSpeedHash, moveSpeed);

            // 更新垂直速度
            animator.SetFloat(verticalVelocityHash, blackboard.Velocity.y);

            // 更新地面状态
            animator.SetBool(isGroundedHash, blackboard.IsGrounded);

            // 更新冲刺状态
            animator.SetBool(isDashingHash, blackboard.IsDashing);

            // 更新攻击状态
            animator.SetBool(isAttackingHash, blackboard.IsAttacking);
            animator.SetInteger(comboIndexHash, blackboard.ComboIndex);

            // 更新动画速度
            animator.speed = blackboard.AnimationSpeed;
        }

        /// <summary>
        /// 触发跳跃动画
        /// </summary>
        public void TriggerJump()
        {
            if (animator != null)
            {
                animator.SetTrigger(jumpHash);
            }
        }

        /// <summary>
        /// 触发冲刺动画
        /// </summary>
        public void TriggerDash()
        {
            if (animator != null)
            {
                animator.SetTrigger(dashHash);
            }
        }

        /// <summary>
        /// 触发攻击动画
        /// </summary>
        public void TriggerAttack(int comboIndex = 0)
        {
            if (animator != null)
            {
                animator.SetInteger(comboIndexHash, comboIndex);
                animator.SetTrigger(attackHash);
            }
        }

        /// <summary>
        /// 触发受伤动画
        /// </summary>
        public void TriggerHurt()
        {
            if (animator != null)
            {
                animator.SetTrigger(hurtHash);
            }
        }

        /// <summary>
        /// 触发死亡动画
        /// </summary>
        public void TriggerDie()
        {
            if (animator != null)
            {
                animator.SetTrigger(dieHash);
            }
        }

        /// <summary>
        /// 播放指定动画
        /// </summary>
        public void PlayAnimation(string stateName, int layer = 0, float normalizedTime = 0f)
        {
            if (animator != null)
            {
                animator.Play(stateName, layer, normalizedTime);
            }
        }

        /// <summary>
        /// 设置动画速度
        /// </summary>
        public void SetAnimationSpeed(float speed)
        {
            if (animator != null)
            {
                animator.speed = speed;
            }
        }

        /// <summary>
        /// 获取当前动画状态信息
        /// </summary>
        public AnimatorStateInfo GetCurrentStateInfo(int layer = 0)
        {
            return animator != null ? animator.GetCurrentAnimatorStateInfo(layer) : default;
        }

        /// <summary>
        /// 检查动画是否正在播放
        /// </summary>
        public bool IsAnimationPlaying(string stateName, int layer = 0)
        {
            if (animator == null) return false;
            return animator.GetCurrentAnimatorStateInfo(layer).IsName(stateName);
        }

        /// <summary>
        /// 获取当前动画的归一化时间
        /// </summary>
        public float GetAnimationNormalizedTime(int layer = 0)
        {
            if (animator == null) return 0f;
            return animator.GetCurrentAnimatorStateInfo(layer).normalizedTime;
        }

        /// <summary>
        /// 设置 Sprite 渲染颜色
        /// </summary>
        public void SetSpriteColor(Color color)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = color;
            }
        }

        /// <summary>
        /// 设置 Sprite 透明度
        /// </summary>
        public void SetSpriteAlpha(float alpha)
        {
            if (spriteRenderer != null)
            {
                Color color = spriteRenderer.color;
                color.a = alpha;
                spriteRenderer.color = color;
            }
        }
    }
}

