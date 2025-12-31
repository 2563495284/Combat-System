using UnityEngine;
using CombatSystem.Core;
using System.Collections.Generic;

namespace CombatSystem.Components
{
    /// <summary>
    /// 动画组件 - 负责管理战斗系统与Unity Animator的集成
    /// </summary>
    public class AnimationComponent : MonoBehaviour
    {
        [Header("动画器引用")]
        [SerializeField] private Animator animator;

        [Header("动画配置")]
        [SerializeField] private bool autoGetAnimator = true;
        [SerializeField] private bool debugMode = false;

        [Header("状态机参数名称")]
        [SerializeField] private string skillStateParam = "IsInSkill";  // 控制 Movement/Skill 子状态机切换的参数

        // 动画事件回调
        private System.Action _onAnimationHit;      // 动画触发伤害判定
        private System.Action _onAnimationEnd;      // 动画播放结束
        private System.Action<string> _onAnimationEvent; // 通用动画事件

        // 动画状态缓存
        private Dictionary<string, AnimationClip> _animationClips = new Dictionary<string, AnimationClip>();
        private string _currentAnimationState;
        private float _currentAnimationLength;

        // 组件引用
        private CombatEntity _entity;

        #region 初始化

        private void Awake()
        {
            _entity = GetComponent<CombatEntity>();

            if (autoGetAnimator && animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            if (animator != null)
            {
                CacheAnimationClips();
            }
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

        #region 动画播放控制

        /// <summary>
        /// 进入技能状态（切换到 Skill 子状态机）
        /// </summary>
        public void EnterSkillState()
        {
            animator.SetBool(skillStateParam, true);

            if (debugMode)
                Debug.Log($"[AnimationComponent] 进入技能状态");
        }

        /// <summary>
        /// 退出技能状态（切换回 Movement 子状态机）
        /// </summary>
        public void ExitSkillState()
        {
            animator.SetBool(skillStateParam, false);

            if (debugMode)
                Debug.Log($"[AnimationComponent] 退出技能状态");
        }

        /// <summary>
        /// 检查是否在技能状态
        /// </summary>
        public bool IsInSkillState()
        {
            if (animator == null) return false;

            return animator.GetBool(skillStateParam);
        }

        /// <summary>
        /// 播放攻击动画
        /// </summary>
        /// <param name="attackIndex">攻击索引(用于连击)</param>
        /// <returns>动画时长(秒)</returns>
        public float PlayAttackAnimation(int attackIndex = 0)
        {
            EnterSkillState();
            animator.SetInteger("NormalAttack", attackIndex);
            if (debugMode)
                Debug.Log($"[AnimationComponent] 播放普通攻击动画: {attackIndex}");
            // 返回动画时长
            return GetAnimationLength("NormalAttack");
        }

        /// <summary>
        /// 播放技能动画
        /// </summary>
        /// <param name="skillName">技能名称</param>
        /// <returns>动画时长(秒)</returns>
        public float PlaySkillAnimation(string skillName)
        {
            // 自动切换到技能状态
            EnterSkillState();

            animator.SetTrigger(skillName);

            if (debugMode)
                Debug.Log($"[AnimationComponent] 播放技能动画: {skillName}");

            return GetAnimationLength(skillName);
        }

        /// <summary>
        /// 播放受击动画
        /// </summary>
        public float PlayHitAnimation()
        {
            if (animator == null) return 0f;

            animator.SetTrigger("Hit");

            if (debugMode)
                Debug.Log($"[AnimationComponent] 播放受击动画");

            return GetAnimationLength("Hit");
        }

        /// <summary>
        /// 播放死亡动画
        /// </summary>
        public float PlayDeathAnimation()
        {
            if (animator == null) return 0f;

            animator.SetTrigger("Death");
            animator.SetBool("IsDead", true);

            if (debugMode)
                Debug.Log($"[AnimationComponent] 播放死亡动画");

            return GetAnimationLength("Death");
        }

        /// <summary>
        /// 设置移动速度参数
        /// </summary>
        public void SetMovementSpeed(float speed)
        {
            if (animator == null) return;

            animator.SetFloat("Speed", speed);
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
        public bool HasAnimator => animator != null;

        #endregion
    }
}
