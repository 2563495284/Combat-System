using UnityEngine;
using CombatSystem.Core;
using BTree;
/// <summary>
/// 跳跃状态任务 (2.5D)
/// 处理角色的跳跃状态逻辑
/// </summary>
public class JumpStateTask : TaskEntry<CharacterBlackboard>
{
    private bool jumpExecuted = false;

    protected override void BeforeEnter()
    {
        jumpExecuted = false;
    }

    protected override int Enter()
    {
        Debug.Log("进入跳跃状态");
        return TaskStatus.RUNNING;
    }

    protected override int Execute()
    {
        // 执行跳跃
        if (!jumpExecuted && Blackboard.InputJump)
        {
            ExecuteJump();
        }

        // 检查是否落地
        if (Blackboard.IsGrounded && Blackboard.Velocity.y <= 0)
        {
            OnLanding();
            return TaskStatus.SUCCESS;
        }

        return TaskStatus.RUNNING;
    }

    /// <summary>
    /// 执行跳跃
    /// </summary>
    private void ExecuteJump()
    {
        jumpExecuted = true;

        // 跳跃逻辑由 Character25DController 处理
        // 这里可以添加跳跃特效

        // 播放跳跃音效
        // AudioManager.Instance?.PlaySound("Jump");

        // 播放跳跃粒子特效
        PlayJumpEffect();

        Debug.Log($"执行跳跃 - 跳跃次数: {Blackboard.JumpCount}");
    }

    /// <summary>
    /// 播放跳跃特效
    /// </summary>
    private void PlayJumpEffect()
    {
        // 从黑板获取位置信息
        if (Blackboard.Transform != null)
        {
            Vector3 position = Blackboard.Transform.position;
            // 这里可以实例化跳跃粒子特效
            // ParticleManager.Instance?.PlayEffect("JumpEffect", position);
        }
    }

    /// <summary>
    /// 着陆处理
    /// </summary>
    private void OnLanding()
    {
        // 播放着陆音效
        // AudioManager.Instance?.PlaySound("Land");

        // 播放着陆粒子特效
        PlayLandEffect();

        // 根据下落高度触发相机震动
        float fallSpeed = Mathf.Abs(Blackboard.Velocity.y);
        if (fallSpeed > 15f)
        {
            // 触发重着陆震动
            Blackboard.Set("TriggerCameraShake", true);
            Blackboard.Set("ShakeIntensity", 0.3f);
            Blackboard.Set("ShakeDuration", 0.2f);
        }

        Debug.Log("角色着陆");
    }

    /// <summary>
    /// 播放着陆特效
    /// </summary>
    private void PlayLandEffect()
    {
        if (Blackboard.Transform != null)
        {
            Vector3 position = Blackboard.Transform.position;
            // ParticleManager.Instance?.PlayEffect("LandEffect", position);
        }
    }

    protected override void OnEventImpl(object evt)
    {
        // 处理跳跃相关事件
        if (evt is string eventName)
        {
            switch (eventName)
            {
                case "DoubleJump":
                    // 处理二段跳
                    Debug.Log("触发二段跳");
                    break;
            }
        }
    }

    protected override void Exit()
    {
        Debug.Log("跳跃状态结束");
    }
}


