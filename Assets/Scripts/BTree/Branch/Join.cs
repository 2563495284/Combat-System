using System;
using System.Collections.Generic;
using BTree.Branch.Join;
using Commons;

namespace BTree.Branch
{
    public class Join<T> : ParallelBranch<T> where T : class
    {
#nullable disable
        protected JoinPolicy<T> policy;
        /** 已进入完成状态的子节点 */
        [NonSerialized] protected int completedCount;
        /** 成功完成的子节点 */
        [NonSerialized] protected int succeededCount;
#nullable enable

        public Join()
        {
        }

        public Join(List<Task<T>>? children) : base(children)
        {
        }

        public override void ResetForRestart()
        {
            base.ResetForRestart();
            completedCount = 0;
            succeededCount = 0;
            policy.ResetForRestart();
        }

        protected override void BeforeEnter()
        {
            if (policy == null)
            {
                policy = JoinSequence<T>.GetInstance();
            }
            completedCount = 0;
            succeededCount = 0;
            // policy的数据重置
            policy.BeforeEnter(this);
        }

        protected override int Enter()
        {
            // 记录子类上下文 -- 由于beforeEnter可能改变子节点信息，因此在enter时处理
            InitChildHelpers(IsCancelTokenPerChild);
            return policy.Enter(this);
        }

        protected override int Execute()
        {
            List<Task<T>> children = this.children;
            if (children.Count == 0)
            {
                return TaskStatus.RUNNING;
            }
            for (int i = 0; i < children.Count; i++)
            {
                Task<T> child = children[i];
                ParallelChildHelper<T> childHelper = GetChildHelper(child);
                bool started = child.IsExited(childHelper.reentryId);
                if (started && child.IsCompleted)
                {
                    continue; // 未重置的情况下可能是上一次的完成状态
                }
                Task<T>? inlinedChild = childHelper.GetInlinedChild();
                if (inlinedChild != null)
                {
                    inlinedChild.Template_ExecuteInlined(ref childHelper.Unwrap(), child);
                }
                else if (child.IsRunning)
                {
                    child.Template_Execute(true);
                }
                else
                {
                    SetChildCancelToken(child, childHelper.cancelToken); // 运行前赋值取消令牌
                    Template_StartChild(child, true, ref childHelper.Unwrap());
                }
                if (child.IsCompleted)
                {
                    UnsetChildCancelToken(child); // 运行结束删除令牌
                                                  // 尝试计算结果
                    completedCount++;
                    if (child.IsSucceeded)
                    {
                        succeededCount++;
                    }
                    int result = policy.OnChildCompleted(this, child);
                    if (result != TaskStatus.RUNNING)
                    {
                        return result;
                    }
                }
                if (cancelToken.IsCancelRequested)
                { // 收到取消信号
                    return TaskStatus.CANCELLED;
                }
            }
            if (completedCount >= children.Count)
            { // child全部执行，但没得出结果
                throw new IllegalStateException();
            }
            return TaskStatus.RUNNING;
        }

        protected override void OnEventImpl(object eventObj)
        {
            policy.OnEvent(this, eventObj);
        }

        // region

        public bool IsAllChildCompleted => completedCount >= children.Count;

        public bool IsAllChildSucceeded => succeededCount >= children.Count;

        public int CompletedCount => completedCount;

        public int SucceededCount => succeededCount;
        // endregion

        public JoinPolicy<T> Policy
        {
            get => policy;
            set => policy = value;
        }
    }
}