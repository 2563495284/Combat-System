using UnityEngine;
using Character3C.Tasks;

namespace Character3C
{
    /// <summary>
    /// 角色三C系统使用示例
    /// 演示如何使用和扩展角色系统
    /// </summary>
    public class CharacterExample : MonoBehaviour
    {
        [Header("角色引用")]
        [SerializeField] private PlayerCharacter player;

        [Header("测试功能")]
        [SerializeField] private bool enableDebugKeys = true;

        private void Start()
        {
            if (player == null)
            {
                player = FindFirstObjectByType<PlayerCharacter>();
            }

            // 设置相机边界（根据关卡大小）
            player?.SetCameraBounds(
                new Vector2(-50, -10),
                new Vector2(50, 10)
            );

            Debug.Log("=== 角色三C系统示例 ===");
            Debug.Log("基本控制：");
            Debug.Log("  移动: A/D 或 左右方向键");
            Debug.Log("  跳跃: Space");
            Debug.Log("  冲刺: Left Shift");
            Debug.Log("  攻击: 鼠标左键 或 J");
            Debug.Log("\n调试按键（如果启用）：");
            Debug.Log("  F1: 切换输入");
            Debug.Log("  F2: 禁用移动");
            Debug.Log("  F3: 触发相机震动");
            Debug.Log("  F4: 显示黑板信息");
        }

        private void Update()
        {
            if (!enableDebugKeys || player == null)
                return;

            // F1 - 切换输入启用
            if (Input.GetKeyDown(KeyCode.F1))
            {
                var input = player.GetInputController();
                bool currentState = input.GetMoveInput() != Vector2.zero || Input.GetButton("Jump");
                input.SetInputEnabled(!currentState);
                Debug.Log($"输入已{(currentState ? "禁用" : "启用")}");
            }

            // F2 - 切换移动能力
            if (Input.GetKeyDown(KeyCode.F2))
            {
                var blackboard = player.GetBlackboard();
                blackboard.CanMove = !blackboard.CanMove;
                Debug.Log($"移动能力已{(blackboard.CanMove ? "启用" : "禁用")}");
            }

            // F3 - 触发相机震动
            if (Input.GetKeyDown(KeyCode.F3))
            {
                player.TriggerCameraShake(0.3f, 0.5f);
                Debug.Log("触发相机震动");
            }

            // F4 - 显示黑板信息
            if (Input.GetKeyDown(KeyCode.F4))
            {
                PrintBlackboardInfo();
            }
        }

        /// <summary>
        /// 打印黑板信息
        /// </summary>
        private void PrintBlackboardInfo()
        {
            var blackboard = player.GetBlackboard();

            Debug.Log("=== 角色黑板数据 ===");
            Debug.Log($"速度: {blackboard.Velocity}");
            Debug.Log($"在地面: {blackboard.IsGrounded}");
            Debug.Log($"朝向右: {blackboard.IsFacingRight}");
            Debug.Log($"跳跃次数: {blackboard.JumpCount}");
            Debug.Log($"正在冲刺: {blackboard.IsDashing}");
            Debug.Log($"正在攻击: {blackboard.IsAttacking}");
            Debug.Log($"连击索引: {blackboard.ComboIndex}");
            Debug.Log($"可以移动: {blackboard.CanMove}");
            Debug.Log($"可以跳跃: {blackboard.CanJump}");
            Debug.Log($"可以冲刺: {blackboard.CanDash}");
            Debug.Log($"可以攻击: {blackboard.CanAttack}");
        }

        // === 以下是一些实用的示例方法 ===

        /// <summary>
        /// 示例：播放过场动画时禁用玩家控制
        /// </summary>
        public void PlayCutscene()
        {
            player.GetInputController().SetInputEnabled(false);

            var blackboard = player.GetBlackboard();
            blackboard.CanMove = false;
            blackboard.CanJump = false;
            blackboard.CanDash = false;
            blackboard.CanAttack = false;

            Debug.Log("过场动画开始 - 玩家控制已禁用");
        }

        /// <summary>
        /// 示例：过场动画结束后恢复玩家控制
        /// </summary>
        public void EndCutscene()
        {
            player.GetInputController().SetInputEnabled(true);

            var blackboard = player.GetBlackboard();
            blackboard.CanMove = true;
            blackboard.CanJump = true;
            blackboard.CanDash = true;
            blackboard.CanAttack = true;

            Debug.Log("过场动画结束 - 玩家控制已恢复");
        }

        /// <summary>
        /// 示例：角色受伤时的处理
        /// </summary>
        public void OnPlayerHurt(float damage, Vector2 knockbackDirection)
        {
            var blackboard = player.GetBlackboard();

            // 播放受伤动画
            var animator = player.GetComponent<CharacterAnimator>();
            animator?.TriggerHurt();

            // 应用击退
            if (blackboard.Rigidbody != null)
            {
                blackboard.Rigidbody.linearVelocity = Vector2.zero;
                blackboard.Rigidbody.AddForce(knockbackDirection * 10f, ForceMode2D.Impulse);
            }

            // 触发相机震动
            player.TriggerCameraShake(0.2f, 0.3f);

            // 短暂无敌
            StartCoroutine(InvincibleCoroutine(1f));

            Debug.Log($"玩家受到 {damage} 点伤害");
        }

        /// <summary>
        /// 示例：短暂无敌协程
        /// </summary>
        private System.Collections.IEnumerator InvincibleCoroutine(float duration)
        {
            var blackboard = player.GetBlackboard();
            blackboard.Set("IsInvincible", true);

            // 闪烁效果
            var animator = player.GetComponent<CharacterAnimator>();
            float elapsed = 0f;
            while (elapsed < duration)
            {
                animator?.SetSpriteAlpha(0.5f);
                yield return new WaitForSeconds(0.1f);
                animator?.SetSpriteAlpha(1f);
                yield return new WaitForSeconds(0.1f);
                elapsed += 0.2f;
            }

            blackboard.Set("IsInvincible", false);
            animator?.SetSpriteAlpha(1f);
        }

        /// <summary>
        /// 示例：传送角色到指定位置
        /// </summary>
        public void TeleportPlayer(Vector3 position)
        {
            player.transform.position = position;

            // 重置速度
            var blackboard = player.GetBlackboard();
            if (blackboard.Rigidbody != null)
            {
                blackboard.Rigidbody.linearVelocity = Vector2.zero;
            }

            // 立即移动相机
            var camera = FindFirstObjectByType<CameraController>();
            camera?.SnapToTarget();

            Debug.Log($"玩家传送到: {position}");
        }

        /// <summary>
        /// 示例：设置关卡边界
        /// </summary>
        public void SetLevelBounds(Vector2 min, Vector2 max)
        {
            player.SetCameraBounds(min, max);
            Debug.Log($"设置关卡边界: Min={min}, Max={max}");
        }
    }
}

