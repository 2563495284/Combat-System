using CombatSystem.Buffs;
using CombatSystem.Configuration;
using CombatSystem.Skills;
using UnityEngine;

namespace CombatSystem.Core
{
    /// <summary>
    /// 状态类
    /// 代表一个技能、Buff或被动效果
    /// </summary>
    public class State
    {
        /// <summary>
        /// 状态配置
        /// </summary>
        public StateCfg Cfg { get; private set; }

        /// <summary>
        /// 状态等级
        /// </summary>
        public int Level { get; set; }

        /// <summary>
        /// 叠层数
        /// </summary>
        public int Stack { get; set; }

        /// <summary>
        /// 剩余时间（毫秒）
        /// -1表示永久
        /// </summary>
        public int TimeLeft { get; set; }

        /// <summary>
        /// 黑板数据
        /// </summary>
        public Blackboard Blackboard { get; private set; }

        /// <summary>
        /// 关联的任务
        /// </summary>
        public TaskEntry Task { get; private set; }

        /// <summary>
        /// 绑定的状态槽
        /// </summary>
        public StateSlot Slot { get; set; }

        /// <summary>
        /// 是否处于活动状态
        /// </summary>
        public bool Active { get; set; }

        /// <summary>
        /// 状态的拥有者
        /// </summary>
        public CombatEntity Owner { get; set; }

        /// <summary>
        /// 状态的施加者
        /// </summary>
        public CombatEntity Caster { get; set; }

        /// <summary>
        /// 状态创建时间戳
        /// </summary>
        public long CreateTime { get; private set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        public State(StateCfg cfg)
        {
            Cfg = cfg;
            Level = 1;
            Stack = 1;
            TimeLeft = cfg.duration;
            Blackboard = new Blackboard();
            Active = false;
            CreateTime = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        /// <summary>
        /// 初始化任务
        /// </summary>
        public void InitializeTask()
        {
            if (Task != null)
                return;

            Task = Cfg.CreateTask();
            if (Task != null)
            {
                Task.Blackboard = Blackboard;

                // 设置 State 引用到任务
                if (Task is Skills.SkillTask skillTask)
                {
                    skillTask.SetState(this);
                }
                else if (Task is Buffs.BuffTask buffTask)
                {
                    buffTask.SetState(this);
                }
            }
        }

        /// <summary>
        /// 启动状态
        /// </summary>
        public void Start()
        {
            Active = true;
            InitializeTask();
            Task?.Start();
        }

        /// <summary>
        /// 更新状态
        /// </summary>
        public void Update(float deltaTime)
        {
            if (!Active)
                return;

            // 更新时间
            if (TimeLeft > 0)
            {
                TimeLeft -= (int)(deltaTime * 1000);
                if (TimeLeft <= 0)
                {
                    TimeLeft = 0;
                    // 状态时间到期，应该被移除
                }
            }

            // 更新任务
            Task?.Update(deltaTime);
        }

        /// <summary>
        /// 处理事件
        /// </summary>
        public void OnEvent(object evt)
        {
            if (!Active)
                return;

            Task?.OnEvent(evt);
        }

        /// <summary>
        /// 停止状态
        /// </summary>
        public void Stop()
        {
            Active = false;
            Task?.Stop();
        }

        /// <summary>
        /// 增加叠层
        /// </summary>
        /// <returns>是否成功增加</returns>
        public bool AddStack(int count = 1)
        {
            if (Stack >= Cfg.maxStack)
                return false;

            Stack = Mathf.Min(Stack + count, Cfg.maxStack);
            return true;
        }

        /// <summary>
        /// 减少叠层
        /// </summary>
        /// <returns>当前叠层数</returns>
        public int RemoveStack(int count = 1)
        {
            Stack = Mathf.Max(0, Stack - count);
            return Stack;
        }

        /// <summary>
        /// 刷新持续时间
        /// </summary>
        public void RefreshDuration()
        {
            TimeLeft = Cfg.duration;
        }

        /// <summary>
        /// 是否已过期
        /// </summary>
        public bool IsExpired()
        {
            return TimeLeft == 0 && Cfg.duration > 0;
        }

        public override string ToString()
        {
            return $"State[{Cfg.cid}:{Cfg.name}] Level:{Level} Stack:{Stack} TimeLeft:{TimeLeft}ms Active:{Active}";
        }
    }
}

