using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 动画组件 - 统一的动画控制器
/// 整合了 CharacterAnimator 和原 AnimationComponent 的功能
/// 支持黑板驱动的动画更新和事件回调系统
/// </summary>
public class AnimationComponent : MonoBehaviour
{
    [Header("动画器引用")]
    [SerializeField] private Animator animator;

    [Header("Sprite 渲染器（用于翻转）")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("动画配置")]
    [SerializeField] private AnimationConfig animationConfig = new AnimationConfig();
    [SerializeField] private bool autoGetAnimator = true;
    [SerializeField] private bool debugMode = false;

    // 动画事件回调
    private System.Action _onAnimationHit;      // 动画触发伤害判定
    private System.Action _onAnimationEnd;      // 动画播放结束
    private System.Action<string> _onAnimationEvent; // 通用动画事件

    // 运行时动画状态（B方案：动画组件内部持有，不再由黑板承载）
    private bool _inSkillState;
    private int _comboIndex;

    // 动画状态缓存
    private Dictionary<string, AnimationClip> _animationClips = new Dictionary<string, AnimationClip>();

    #region 初始化

    private void Awake()
    {
        if (autoGetAnimator && animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (animator != null)
        {
            CacheAnimationClips();
        }

        // 初始化动画配置的哈希值
        animationConfig.InitializeHashes();
    }

    /// <summary>
    /// 缓存所有动画片段
    /// </summary>
    private void CacheAnimationClips()
    {
        if (animator == null || animator.runtimeAnimatorController == null)
            return;

        var clips = animator.runtimeAnimatorController.animationClips;
        foreach (var clip in clips)
        {
            if (!_animationClips.ContainsKey(clip.name))
            {
                _animationClips[clip.name] = clip;

                if (debugMode)
                    Debug.Log($"[AnimationComponent] 缓存动画: {clip.name}, 时长: {clip.length}秒");
            }
        }
    }

    #endregion

    #region 黑板驱动的动画更新

    /// <summary>
    /// 根据黑板数据更新动画状态（每帧调用）
    /// </summary>
    public void UpdateAnimations(CharacterBlackboard blackboard)
    {
        if (animator == null || blackboard == null) return;

        // 更新移动相关动画参数
        float moveSpeed = Mathf.Abs(blackboard.Velocity.magnitude);
        animator.SetFloat(animationConfig.speedHash, moveSpeed);
        // 更新地面状态
        animator.SetBool(animationConfig.isGroundedHash, blackboard.IsGrounded);

        // 更新技能/连击状态（由 AnimationComponent 内部状态驱动）
        animator.SetBool(animationConfig.skillStateHash, _inSkillState);
        animator.SetInteger(animationConfig.comboIndexHash, _comboIndex);

        // 更新 Sprite 翻转
        UpdateSpriteFlip(blackboard);
    }

    /// <summary>
    /// 更新 Sprite 翻转（根据朝向）
    /// </summary>
    private void UpdateSpriteFlip(CharacterBlackboard blackboard)
    {
        if (spriteRenderer == null) return;

        // 根据朝向方向翻转 Sprite
        if (blackboard.FacingDirection.x != 0)
        {
            spriteRenderer.flipX = blackboard.FacingDirection.x < 0f;
        }
    }

    #endregion

    #region 动画播放控制

    /// <summary>
    /// 进入技能状态（切换到 Skill 子状态机）
    /// </summary>
    public void EnterSkillState()
    {
        _inSkillState = true;
        animator.SetBool(animationConfig.skillStateHash, true);

        if (debugMode)
            Debug.Log($"[AnimationComponent] 进入技能状态");
    }

    /// <summary>
    /// 退出技能状态（切换回 Movement 子状态机）
    /// </summary>
    public void ExitSkillState()
    {
        _inSkillState = false;
        animator.SetBool(animationConfig.skillStateHash, false);

        if (debugMode)
            Debug.Log($"[AnimationComponent] 退出技能状态");
    }

    /// <summary>
    /// 检查是否在技能状态
    /// </summary>
    public bool IsInSkillState()
    {
        if (animator == null) return false;

        return animator.GetBool(animationConfig.skillStateHash);
    }

    /// <summary>
    /// 设置连击段数（用于 Skill 子状态机选择 hero_jian_attack0/1/2）
    /// </summary>
    public void SetComboIndex(int comboIndex)
    {
        _comboIndex = comboIndex;
        if (animator == null) return;
        animator.SetInteger(animationConfig.comboIndexHash, comboIndex);
    }

    /// <summary>
    /// 播放普通攻击（B方案：外部不再通过黑板控制 Skill/Combo 参数）
    /// - 设置 ComboIndex
    /// - 进入 Skill 子状态机
    /// - 尝试直接播放 hero_jian_attack 0/1/2（若找不到则依赖状态机过渡）
    /// </summary>
    public float PlayNormalAttack(int comboIndex, int layer = 0)
    {
        SetComboIndex(comboIndex);
        EnterSkillState();

        if (animator != null)
        {
            // 兼容两种命名：有空格/无空格
            string stateNameWithSpace = $"hero_jian_attack {comboIndex}";
            string stateNameNoSpace = $"hero_jian_attack{comboIndex}";

            int hashWithSpace = Animator.StringToHash(stateNameWithSpace);
            int hashNoSpace = Animator.StringToHash(stateNameNoSpace);

            if (animator.HasState(layer, hashWithSpace))
            {
                animator.Play(stateNameWithSpace, layer, 0f);
            }
            else if (animator.HasState(layer, hashNoSpace))
            {
                animator.Play(stateNameNoSpace, layer, 0f);
            }
        }

        // 时长仅做兜底：优先使用动画事件结束
        float len = GetAnimationLength($"hero_jian_attack{comboIndex}");
        if (len <= 0f) len = GetAnimationLength($"hero_jian_attack {comboIndex}");
        return len;
    }

    /// <summary>
    /// 播放指定动画
    /// </summary>
    public void PlayAnimation(string stateName, int layer = 0, float normalizedTime = 0f)
    {
        if (animator != null)
        {
            animator.Play(stateName, layer, normalizedTime);

            if (debugMode)
                Debug.Log($"[AnimationComponent] 播放动画: {stateName}");
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
    /// 设置移动速度参数
    /// </summary>
    public void SetMovementSpeed(float speed)
    {
        if (animator == null) return;

        animator.SetFloat(animationConfig.speedHash, speed);
    }

    /// <summary>
    /// 设置布尔参数
    /// </summary>
    public void SetBool(string paramName, bool value)
    {
        if (animator == null) return;

        animator.SetBool(paramName, value);
    }

    /// <summary>
    /// 设置浮点参数
    /// </summary>
    public void SetFloat(string paramName, float value)
    {
        if (animator == null) return;

        animator.SetFloat(paramName, value);
    }

    /// <summary>
    /// 设置整型参数
    /// </summary>
    public void SetInt(string paramName, int value)
    {
        if (animator == null) return;

        animator.SetInteger(paramName, value);
    }

    /// <summary>
    /// 触发动画参数
    /// </summary>
    public void SetTrigger(string triggerName)
    {
        if (animator == null) return;

        animator.SetTrigger(triggerName);
    }

    #endregion

    #region 动画信息查询

    /// <summary>
    /// 获取动画长度
    /// </summary>
    public float GetAnimationLength(string animationName)
    {
        if (_animationClips.TryGetValue(animationName, out var clip))
        {
            return clip.length;
        }

        // 尝试查找包含该名称的动画
        foreach (var kvp in _animationClips)
        {
            if (kvp.Key.Contains(animationName))
            {
                return kvp.Value.length;
            }
        }

        if (debugMode)
            Debug.LogWarning($"[AnimationComponent] 未找到动画: {animationName}");

        return 0f;
    }

    /// <summary>
    /// 获取当前动画状态信息
    /// </summary>
    public AnimatorStateInfo GetCurrentStateInfo(int layerIndex = 0)
    {
        if (animator == null)
            return default;

        return animator.GetCurrentAnimatorStateInfo(layerIndex);
    }

    /// <summary>
    /// 获取当前动画归一化时间(0-1)
    /// </summary>
    public float GetCurrentAnimationTime(int layerIndex = 0)
    {
        if (animator == null)
            return 0f;

        return animator.GetCurrentAnimatorStateInfo(layerIndex).normalizedTime;
    }

    /// <summary>
    /// 检查动画是否播放完成
    /// </summary>
    public bool IsAnimationFinished(int layerIndex = 0)
    {
        if (animator == null)
            return true;

        var stateInfo = animator.GetCurrentAnimatorStateInfo(layerIndex);
        return stateInfo.normalizedTime >= 1.0f && !animator.IsInTransition(layerIndex);
    }

    /// <summary>
    /// 检查是否在播放特定动画状态
    /// </summary>
    public bool IsInState(string stateName, int layerIndex = 0)
    {
        if (animator == null)
            return false;

        return animator.GetCurrentAnimatorStateInfo(layerIndex).IsName(stateName);
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

    #endregion

    #region Sprite 控制

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

    /// <summary>
    /// 设置 Sprite 翻转
    /// </summary>
    public void SetSpriteFlipX(bool flip)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = flip;
        }
    }

    #endregion

    #region 动画事件回调(由Animation Event调用)

    /// <summary>
    /// 动画事件 - 触发伤害判定
    /// 在动画编辑器中添加此事件到攻击动画的适当帧
    /// </summary>
    public void OnAnimationHit()
    {
        if (debugMode)
            Debug.Log($"[AnimationComponent] 动画事件 - 伤害判定");

        _onAnimationHit?.Invoke();
    }

    /// <summary>
    /// 动画事件 - 动画结束
    /// </summary>
    public void OnAnimationEnd()
    {
        if (debugMode)
            Debug.Log($"[AnimationComponent] 动画事件 - 动画结束");

        _onAnimationEnd?.Invoke();
    }

    /// <summary>
    /// 动画事件 - 通用事件
    /// </summary>
    public void OnAnimationEvent(string eventName)
    {
        if (debugMode)
            Debug.Log($"[AnimationComponent] 动画事件 - {eventName}");

        _onAnimationEvent?.Invoke(eventName);
    }

    /// <summary>
    /// 动画事件 - 播放音效
    /// </summary>
    public void OnPlaySound(string soundName)
    {
        if (debugMode)
            Debug.Log($"[AnimationComponent] 播放音效 - {soundName}");

        // TODO: 集成音效系统
        // AudioManager.Instance?.PlaySound(soundName);
    }

    /// <summary>
    /// 动画事件 - 播放特效
    /// </summary>
    public void OnPlayEffect(string effectName)
    {
        if (debugMode)
            Debug.Log($"[AnimationComponent] 播放特效 - {effectName}");

        // TODO: 集成特效系统
        // EffectManager.Instance?.PlayEffect(effectName, transform.position);
    }

    #endregion

    #region 事件注册

    /// <summary>
    /// 注册伤害判定回调
    /// </summary>
    public void RegisterHitCallback(System.Action callback)
    {
        _onAnimationHit = callback;
    }

    /// <summary>
    /// 注册动画结束回调
    /// </summary>
    public void RegisterEndCallback(System.Action callback)
    {
        _onAnimationEnd = callback;
    }

    /// <summary>
    /// 注册通用事件回调
    /// </summary>
    public void RegisterEventCallback(System.Action<string> callback)
    {
        _onAnimationEvent = callback;
    }

    /// <summary>
    /// 清除所有回调
    /// </summary>
    public void ClearAllCallbacks()
    {
        _onAnimationHit = null;
        _onAnimationEnd = null;
        _onAnimationEvent = null;
    }

    #endregion

    #region 属性访问

    public Animator Animator => animator;
    public SpriteRenderer SpriteRenderer => spriteRenderer;
    public AnimationConfig Config => animationConfig;
    public bool HasAnimator => animator != null;
    public bool DebugMode { get => debugMode; set => debugMode = value; }

    #endregion
}
