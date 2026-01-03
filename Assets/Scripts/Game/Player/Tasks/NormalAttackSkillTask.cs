using UnityEngine;
using BTree;

/// <summary>
/// 普通攻击技能任务
/// - 进入 Skill 子状态机
/// - 根据连击索引播放 hero_jian_attack0/1/2（由 Animator 参数 NormalAttack 驱动）
/// - 动画事件驱动命中与结束；缺失动画事件时用时序兜底
/// </summary>
public class NormalAttackSkillTask : SkillTask<Blackboard>
{
    private struct ComboHitConfig
    {
        public float damageMultiplier;
        public float forwardOffset;
        public Vector3 halfExtents; // OverlapBox 的半尺寸
        public Vector3 centerOffset;
    }

    // 一技能一脚本：三连击的段配置都放在同一个任务里（后续可移到 StateCfg/ScriptableObject）
    private static readonly ComboHitConfig[] COMBO_CONFIGS = new ComboHitConfig[]
    {
        new ComboHitConfig { damageMultiplier = 1.0f, forwardOffset = 1.0f, halfExtents = new Vector3(0.9f, 0.8f, 0.9f), centerOffset = new Vector3(0f, 0.9f, 0f) },
        new ComboHitConfig { damageMultiplier = 1.2f, forwardOffset = 1.1f, halfExtents = new Vector3(1.0f, 0.85f, 1.0f), centerOffset = new Vector3(0f, 0.9f, 0f) },
        new ComboHitConfig { damageMultiplier = 1.5f, forwardOffset = 1.25f, halfExtents = new Vector3(1.15f, 0.9f, 1.15f), centerOffset = new Vector3(0f, 0.9f, 0f) },
    };

    private LayerMask _enemyLayer; // 敌人层级

    // 连击系统
    private const int MAX_COMBO_COUNT = 3;    // 最大连击数 (0, 1, 2)
    private const float COMBO_TIMEOUT = 10f;  // 连击超时时间(秒)

    private bool _hitExecuted = false;
    private bool _animEnded = false;
    private float _elapsedTime = 0f;
    private int _comboIndex = 0;
    private float _fallbackDuration = 0f;

    protected override void OnSkillStart()
    {
        _hitExecuted = false;
        _animEnded = false;
        _elapsedTime = 0f;

        // 获取本次施放段（跨施放持久化：存 SkillComponent）
        int skillId = State?.Cfg?.cid ?? 0;
        _comboIndex = Caster?.SkillComp != null
            ? Caster.SkillComp.PeekComboIndex(skillId, COMBO_TIMEOUT, MAX_COMBO_COUNT)
            : 0;

        // 设置敌人层级
        _enemyLayer = LayerMask.GetMask("Enemy");
        Caster?.MoveComp?.SetCanMove(false);

        if (Caster?.AnimComp != null)
        {
            // 由动画驱动逻辑：Hit/End 都走 AnimationEvent 回调
            Caster.AnimComp.RegisterHitCallback(OnAnimationHitEvent);
            Caster.AnimComp.RegisterEndCallback(OnAnimationEndEvent);

            // 选择本段动画（Animator 子状态机或直接 Play）
            _fallbackDuration = Caster.AnimComp.PlayNormalAttack(_comboIndex);
        }
        else
        {
            _fallbackDuration = 0f;
        }

        // 兜底：如果拿不到 clip 长度，就用配置 duration
        if (_fallbackDuration <= 0f && State?.Cfg != null && State.Cfg.duration > 0)
        {
            _fallbackDuration = State.Cfg.duration / 1000f;
        }

        Blackboard?.Set("ComboIndex", _comboIndex);

        Debug.Log($"[普通攻击] {Caster?.EntityName} 开始攻击 连击:{_comboIndex}");
        
    }

    protected override int OnExecute(float deltaTime)
    {
        _elapsedTime += deltaTime;

        // 动画事件最准确
        if (_animEnded)
        {
            return TaskStatus.SUCCESS;
        }

        // 兜底：动画事件缺失时，超时结束（避免卡死）
        if (_fallbackDuration > 0f && _elapsedTime >= _fallbackDuration + 0.1f)
        {
            Debug.LogWarning($"[普通攻击] 动画结束事件缺失，使用时序兜底结束。combo={_comboIndex}");
            return TaskStatus.SUCCESS;
        }

        return TaskStatus.RUNNING;
    }

    protected override void OnSkillEnd()
    {
        // 仅在“正常完成”时推进连击；被打断/被挤出槽(CANCELLED)不推进
        if (Status == TaskStatus.SUCCESS)
        {
            int skillId = State?.Cfg?.cid ?? 0;
            Caster?.SkillComp?.CommitComboOnSuccess(skillId, MAX_COMBO_COUNT);
        }

        // 清除动画回调
        Caster?.AnimComp?.ClearAllCallbacks();
        Caster?.MoveComp?.SetCanMove(true);
        Debug.Log($"[普通攻击] {Caster?.EntityName} 攻击结束");
    }

    /// <summary>
    /// 执行攻击判定
    /// </summary>
    private void ExecuteAttackHit()
    {
        if (Caster == null) return;

        var cfg = COMBO_CONFIGS[Mathf.Clamp(_comboIndex, 0, COMBO_CONFIGS.Length - 1)];

        // 计算攻击方向（优先朝向目标）
        Vector3 attackOrigin = Caster.transform.position + cfg.centerOffset;
        Vector3 attackDirection = Caster.transform.forward;

        // 如果有指定目标,朝向目标
        if (Target != null && Target.IsAlive())
        {
            var dir = (Target.transform.position - attackOrigin);
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.0001f)
            {
                attackDirection = dir.normalized;
            }
        }
        attackDirection.y = 0f;
        if (attackDirection.sqrMagnitude < 0.0001f)
        {
            attackDirection = Vector3.forward;
        }
        attackDirection.Normalize();

        // 计算攻击判定（包围盒）
        Vector3 attackCenter = attackOrigin + attackDirection * cfg.forwardOffset;
        Quaternion rot = Quaternion.LookRotation(attackDirection, Vector3.up);

        Collider[] hits = Physics.OverlapBox(attackCenter, cfg.halfExtents, rot, _enemyLayer);

        int hitCount = 0;
        foreach (var hit in hits)
        {
            // 兼容：Collider 可能挂在子物体上
            var targetEntity = hit.GetComponentInParent<CombatEntity>();
            if (targetEntity != null && targetEntity != Caster && targetEntity.IsAlive())
            {
                if (targetEntity.Camp != Caster.Camp)
                {
                    DealDamageToTarget(targetEntity, cfg.damageMultiplier);
                    hitCount++;
                }
            }
        }

        if (hitCount > 0)
        {
            PlayHitEffect(attackCenter);
            Debug.Log($"[普通攻击] 命中 {hitCount} 个目标");
        }
        else
        {
            Debug.Log("[普通攻击] 未命中任何目标");
        }
    }

    private void DealDamageToTarget(CombatEntity target, float damageMultiplier)
    {
        if (target == null || Caster == null) return;

        // 从黑板获取伤害值,如果没有则使用施法者攻击力
        float damage = Blackboard?.Get<float>("Damage") ?? 0f;
        if (damage <= 0)
        {
            damage = Caster.AttrComp.GetAttr(AttrType.Attack);
        }

        damage = Mathf.Max(0f, damage * Mathf.Max(0f, damageMultiplier));
        Caster.DealDamage(target, damage, DamageType.Physical);
    }

    private void OnAnimationHitEvent()
    {
        if (_hitExecuted) return;
        ExecuteAttackHit();
        _hitExecuted = true;
    }

    private void OnAnimationEndEvent()
    {
        _animEnded = true;
    }

    private void PlayHitEffect(Vector3 position)
    {
        // TODO: 播放粒子特效和音效
    }
}


