using UnityEngine;

namespace CombatSystem.Core
{
    /// <summary>
    /// 任务入口基类
    /// 支持行为树、状态机等脚本系统
    /// </summary>
    /// <typeparam name="TBlackboard">黑板类型</typeparam>
    public abstract class TaskEntry<TBlackboard>
        where TBlackboard : Blackboard
    {
        /// <summary>
        /// 关联的黑板数据
        /// </summary>
        public TBlackboard Blackboard { get; set; }

        /// <summary>
        /// 任务是否正在运行
        /// </summary>
        public bool IsRunning { get; protected set; }

        /// <summary>
        /// 任务是否已完成
        /// </summary>
        public bool IsCompleted { get; protected set; }

        /// <summary>
        /// 启动任务
        /// </summary>
        public virtual void Start()
        {
            IsRunning = true;
            IsCompleted = false;
            OnStart();
        }

        /// <summary>
        /// 更新任务
        /// </summary>
        /// <param name="deltaTime">帧间隔时间</param>
        public void Update(float deltaTime)
        {
            if (!IsRunning || IsCompleted)
                return;

            OnUpdate(deltaTime);
        }

        /// <summary>
        /// 处理事件
        /// </summary>
        /// <param name="evt">事件对象</param>
        public void OnEvent(object evt)
        {
            if (!IsRunning)
                return;

            HandleEvent(evt);
        }

        /// <summary>
        /// 停止任务
        /// </summary>
        public virtual void Stop()
        {
            IsRunning = false;
            OnStop();
        }

        /// <summary>
        /// 完成任务
        /// </summary>
        protected void Complete()
        {
            IsCompleted = true;
            IsRunning = false;
            OnComplete();
        }

        /// <summary>
        /// 启动时调用
        /// </summary>
        protected virtual void OnStart() { }

        /// <summary>
        /// 每帧更新
        /// </summary>
        protected virtual void OnUpdate(float deltaTime) { }

        /// <summary>
        /// 事件处理
        /// </summary>
        protected virtual void HandleEvent(object evt) { }

        /// <summary>
        /// 停止时调用
        /// </summary>
        protected virtual void OnStop() { }

        /// <summary>
        /// 完成时调用
        /// </summary>
        protected virtual void OnComplete() { }
    }

    /// <summary>
    /// 默认任务入口（使用通用黑板）
    /// </summary>
    public abstract class TaskEntry : TaskEntry<Blackboard> { }
}

