using System.Collections.Generic;

namespace BTree.Branch
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [TaskInlinable]
    public class Sequence<T> : SingleRunningChildBranch<T> where T : class
    {
        public Sequence()
        {
        }

        public Sequence(List<Task<T>>? children) : base(children)
        {
        }

        public Sequence(Task<T> first, Task<T>? second) : base(first, second)
        {
        }

        protected override int Enter()
        {
            if (children.Count == 0)
            {
                return TaskStatus.SUCCESS;
            }
            else if (IsCheckingGuard())
            {
                // 条件检测性能优化
                for (int i = 0; i < children.Count; i++)
                {
                    Task<T> child = children[i];
                    if (!Template_CheckGuard(child))
                    {
                        return child.Status;
                    }
                }
                return TaskStatus.SUCCESS;
            }
            return TaskStatus.RUNNING;
        }

        protected override int OnChildCompleted(Task<T> child)
        {
            if (child.IsCancelled)
            {
                return TaskStatus.CANCELLED;
            }
            if (child.IsFailed)
            { // 失败码有传递的价值
                return child.Status;
            }
            else if (IsAllChildCompleted)
            {
                return TaskStatus.SUCCESS;
            }
            else
            {
                return TaskStatus.RUNNING;
            }
        }
    }
}