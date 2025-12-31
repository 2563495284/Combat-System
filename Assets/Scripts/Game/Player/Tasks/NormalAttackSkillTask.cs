using UnityEngine;
using BTree;
/// <summary>
/// 普通攻击技能任务
/// 实现基础的近战物理攻击，支持动画事件驱动和时序控制两种模式
/// </summary>
public class NormalAttackSkillTask : SkillTask<CharacterBlackboard>
{
    // 攻击配置参数
    private float _attackRange = 2f;        // 攻击范围
    private float _attackRadius = 1.5f;     // 攻击半径
    private float _hitTiming = 0.3f;        // 伤害判定时机(秒) - 仅在时序模式下使用
    private LayerMask _enemyLayer;          // 敌人层级

    // 运行时数据
    private float _elapsedTime;             // 已运行时间
    private bool _hitExecuted;              // 是否已执行伤害判定
    private float _skillDuration;           // 技能持续时间

    // 动画相关
    private bool _useAnimationEvents = true; // 是否使用动画事件驱动

    // 连击系统
    private const int MAX_COMBO_COUNT = 3;   // 最大连击数 (0, 1, 2)
    private const float COMBO_TIMEOUT = 1.5f; // 连击超时时间(秒)
    private const string COMBO_INDEX_KEY = "ComboIndex";      // 黑板键：连击索引
    private const string LAST_ATTACK_TIME_KEY = "LastAttackTime"; // 黑板键：上次攻击时间

    protected override void OnSkillStart()
    {
        _elapsedTime = 0f;
        _hitExecuted = false;

        // 更新连击索引
        int comboIndex = UpdateComboIndex();

        // 从配置读取技能持续时间，如果有动画则使用动画时长
        _skillDuration = State.Cfg.duration / 1000f;

        // 设置敌人层级
        _enemyLayer = LayerMask.GetMask("Enemy");

        // 禁用移动
        if (Caster != null)
        {
            Caster.MoveComp?.SetCanMove(false);
        }

        // 播放攻击动画并获取实际时长
        float animDuration = PlayAttackAnimation(comboIndex);
        if (animDuration > 0)
        {
            _skillDuration = animDuration;
        }
        else if (_skillDuration <= 0)
        {
            _skillDuration = 0.5f; // 默认时长
        }

        // 注册动画事件回调
        if (_useAnimationEvents && Caster?.AnimComp != null)
        {
            Caster.AnimComp.RegisterHitCallback(OnAnimationHitEvent);
            Caster.AnimComp.RegisterEndCallback(OnAnimationEndEvent);
        }

        Debug.Log($"[普通攻击] {Caster?.EntityName} 开始攻击 连击:{comboIndex} (时长:{_skillDuration}秒)");
    }

    protected override int OnExecute(float deltaTime)
    {
        _elapsedTime += deltaTime;

        // 如果不使用动画事件，则使用时序控制伤害判定
        if (!_useAnimationEvents || Caster?.AnimComp == null)
        {
            // 在特定时机执行伤害判定
            if (!_hitExecuted && _elapsedTime >= _hitTiming)
            {
                ExecuteAttackHit();
                _hitExecuted = true;
            }
        }

        // 检查技能是否完成
        if (_elapsedTime >= _skillDuration)
        {
            return TaskStatus.SUCCESS;
        }

        // 也可以通过动画状态判断是否完成
        if (Caster?.AnimComp != null && Caster.AnimComp.IsAnimationFinished())
        {
            return TaskStatus.SUCCESS;
        }

        return TaskStatus.RUNNING;
    }

    protected override void OnSkillEnd()
    {
        // 清除动画回调
        Caster?.AnimComp?.ClearAllCallbacks();

        // 恢复移动能力
        Caster?.MoveComp?.SetCanMove(true);

        // 更新上次攻击时间
        Blackboard?.Set(LAST_ATTACK_TIME_KEY, Time.time);

        Debug.Log($"[普通攻击] {Caster?.EntityName} 攻击结束");
    }

    /// <summary>
    /// 执行攻击判定
    /// </summary>
    private void ExecuteAttackHit()
    {
        if (Caster == null) return;

        // 计算攻击位置和方向
        Vector3 attackOrigin = Caster.transform.position;
        Vector3 attackDirection = Caster.transform.forward;

        // 如果有指定目标,朝向目标
        if (Target != null && Target.IsAlive())
        {
            attackDirection = (Target.transform.position - attackOrigin).normalized;
        }

        // 计算攻击判定位置
        Vector3 attackCenter = attackOrigin + attackDirection * (_attackRange * 0.5f);

        // 检测范围内的敌人
        Collider[] hits = Physics.OverlapSphere(attackCenter, _attackRadius, _enemyLayer);

        int hitCount = 0;

        foreach (var hit in hits)
        {
            var targetEntity = hit.GetComponent<PlayerCombatEntity>();
            if (targetEntity != null && targetEntity != Caster && targetEntity.IsAlive())
            {
                // 检查阵营(不同阵营才能攻击)
                if (targetEntity.Camp != Caster.Camp)
                {
                    DealDamageToTarget(targetEntity);
                    hitCount++;
                }
            }
        }

        // 播放效果
        if (hitCount > 0)
        {
            PlayHitEffect(attackCenter);
            Debug.Log($"[普通攻击] 命中 {hitCount} 个目标");
        }
        else
        {
            Debug.Log("[普通攻击] 未命中任何目标");
        }

        // 调试可视化
        DebugDrawAttackRange(attackCenter);
    }

    /// <summary>
    /// 对目标造成伤害
    /// </summary>
    private void DealDamageToTarget(CombatEntity target)
    {
        if (target == null || Caster == null) return;

        // 从黑板获取伤害值,如果没有则使用施法者攻击力
        float damage = Blackboard?.Get<float>("Damage") ?? 0f;
        if (damage <= 0)
        {
            damage = Caster.AttrComp.GetAttr(AttrType.Attack);
        }

        // 应用伤害
        Caster.DealDamage(target, damage, DamageType.Physical);

        // 应用击退效果(可选)
        ApplyKnockback(target);
    }

    /// <summary>
    /// 应用击退效果
    /// </summary>
    private void ApplyKnockback(CombatEntity target)
    {
        if (target == null || Caster == null) return;

        Vector3 knockbackDir = (target.transform.position - Caster.transform.position).normalized;
        knockbackDir.y = 0; // 只在水平方向击退

        float knockbackForce = 3f;

        // 使用移动组件应用击退
        // target.MoveComp?.ApplyKnockback(knockbackDir * knockbackForce);
    }

    /// <summary>
    /// 更新连击索引
    /// </summary>
    /// <returns>当前连击索引</returns>
    private int UpdateComboIndex()
    {
        if (Blackboard == null)
            return 0;

        // 获取上次攻击时间
        float lastAttackTime = Blackboard.Get<float>(LAST_ATTACK_TIME_KEY);
        float currentTime = Time.time;

        // 检查是否超时
        bool isTimeout = (currentTime - lastAttackTime) > COMBO_TIMEOUT;

        if (isTimeout || lastAttackTime <= 0)
        {
            // 超时或首次攻击，重置为0
            ResetCombo();
            Debug.Log($"[连击系统] 连击重置");
        }

        int currentCombo = Blackboard?.Get<int>(COMBO_INDEX_KEY) ?? 0;


        return currentCombo;
    }

    /// <summary>
    /// 重置连击系统
    /// </summary>
    private void ResetCombo()
    {
        if (Blackboard == null)
            return;

        Blackboard.Set(COMBO_INDEX_KEY, 0);
        Blackboard.Set(LAST_ATTACK_TIME_KEY, 0f);

        Debug.Log($"[连击系统] 手动重置连击");
    }

    /// <summary>
    /// 播放攻击动画
    /// </summary>
    /// <param name="comboIndex">连击索引</param>
    /// <returns>动画时长(秒)</returns>
    private float PlayAttackAnimation(int comboIndex)
    {
        return Caster.AnimComp.PlayAttackAnimation(comboIndex);
    }

    /// <summary>
    /// 动画事件回调 - 伤害判定
    /// 由 AnimationComponent 在动画事件触发时调用
    /// </summary>
    private void OnAnimationHitEvent()
    {
        if (!_hitExecuted)
        {
            ExecuteAttackHit();
            _hitExecuted = true;
        }
    }

    /// <summary>
    /// 动画事件回调 - 动画结束
    /// </summary>
    private void OnAnimationEndEvent()
    {
        // 可以在这里提前结束技能
        int comboIndex = Blackboard?.Get<int>(COMBO_INDEX_KEY) ?? 0;

        Blackboard.Set(COMBO_INDEX_KEY, (comboIndex + 1) % MAX_COMBO_COUNT);

        Debug.Log("[普通攻击] 动画播放完成");
    }

    /// <summary>
    /// 播放击中特效
    /// </summary>
    private void PlayHitEffect(Vector3 position)
    {
        // TODO: 播放粒子特效和音效
        // ParticleManager.Instance?.PlayEffect("HitEffect", position);
        // AudioManager.Instance?.PlaySound("Hit");
    }

    /// <summary>
    /// 调试绘制攻击范围
    /// </summary>
    private void DebugDrawAttackRange(Vector3 center)
    {
#if UNITY_EDITOR
        // 绘制攻击范围球体
        Color color = _hitExecuted ? Color.red : Color.yellow;
        Debug.DrawLine(center, center + Vector3.up * _attackRadius, color, 0.5f);

        // 绘制圆形范围(简化版)
        int segments = 16;
        float angleStep = 360f / segments;
        for (int i = 0; i < segments; i++)
        {
            float angle1 = i * angleStep * Mathf.Deg2Rad;
            float angle2 = (i + 1) * angleStep * Mathf.Deg2Rad;

            Vector3 point1 = center + new Vector3(Mathf.Cos(angle1), 0, Mathf.Sin(angle1)) * _attackRadius;
            Vector3 point2 = center + new Vector3(Mathf.Cos(angle2), 0, Mathf.Sin(angle2)) * _attackRadius;

            Debug.DrawLine(point1, point2, color, 0.5f);
        }
#endif
    }

    /// <summary>
    /// 设置攻击参数
    /// </summary>
    public void SetAttackParams(float range, float radius, float hitTiming)
    {
        _attackRange = range;
        _attackRadius = radius;
        _hitTiming = hitTiming;
    }

    /// <summary>
    /// 设置是否使用动画事件
    /// </summary>
    public void SetUseAnimationEvents(bool use)
    {
        _useAnimationEvents = use;
    }

    /// <summary>
    /// 获取当前连击索引
    /// </summary>
    public int GetCurrentComboIndex()
    {
        return Blackboard?.Get<int>(COMBO_INDEX_KEY) ?? 0;
    }

    /// <summary>
    /// 手动重置连击
    /// </summary>
    public void ManualResetCombo()
    {
        ResetCombo();
    }
}

