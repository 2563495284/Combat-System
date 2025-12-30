using System.Collections.Generic;

namespace BTree.Branch
{
#nullable enable
    /// <summary>
    /// 迭代所有的子节点最后返回成功
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [TaskInlinable]
    public class Foreach<T> : SingleRunningChildBranch<T> where T : class
    {
        public Foreach()
        {
        }

        public Foreach(List<Task<T>>? children) : base(children)
        {
        }

        protected override int Enter()
        {
            if (children.Count == 0)
            {
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
            if (IsAllChildCompleted)
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