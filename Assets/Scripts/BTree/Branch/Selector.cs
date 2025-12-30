using System.Collections.Generic;

namespace BTree.Branch
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [TaskInlinable]
    public class Selector<T> : SingleRunningChildBranch<T> where T : class
    {
        public Selector()
        {
        }

        public Selector(List<Task<T>>? children) : base(children)
        {
        }

        public Selector(Task<T> first, Task<T>? second) : base(first, second)
        {
        }

        protected override int Enter()
        {
            if (children.Count == 0)
            {
                return TaskStatus.CHILDLESS;
            }
            else if (IsCheckingGuard())
            {
                // 条件检测性能优化
                for (int i = 0; i < children.Count; i++)
                {
                    Task<T> child = children[i];
                    if (Template_CheckGuard(child))
                    {
                        return TaskStatus.SUCCESS;
                    }
                }
                return TaskStatus.ERROR;
            }
            return TaskStatus.RUNNING;
        }

        protected override int OnChildCompleted(Task<T> child)
        {
            if (child.IsCancelled)
            {
                return TaskStatus.CANCELLED;
            }
            if (child.IsSucceeded)
            {
                return TaskStatus.SUCCESS;
            }
            else if (IsAllChildCompleted)
            {
                return TaskStatus.ERROR;
            }
            else
            {
                return TaskStatus.RUNNING;
            }
        }
    }
}