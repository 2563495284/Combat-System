namespace BTree.Decorator
{
#nullable enable
    /// <summary>
    /// 循环子节点直到给定的条件达成
    /// </summary>
    [TaskInlinable]
    public class UntilCond<T> : LoopDecorator<T> where T : class
    {
        /** 循环条件 -- 不能直接使用child的guard，意义不同 */
        private Task<T>? cond;

        public override void ResetForRestart()
        {
            base.ResetForRestart();
            if (cond != null)
            {
                cond.ResetForRestart();
            }
        }

        protected override int OnChildCompleted(Task<T> child)
        {
            if (child.IsCancelled)
            {
                return TaskStatus.CANCELLED;
            }
            if (Template_CheckGuard(cond))
            {
                return TaskStatus.SUCCESS;
            }
            else if (!HasNextLoop())
            {
                return (TaskStatus.MAX_LOOP_LIMIT);
            }
            else
            {
                return TaskStatus.RUNNING;
            }
        }

        /// <summary>
        /// 子节点的循条件
        /// </summary>
        public Task<T>? Cond
        {
            get => cond;
            set => cond = value;
        }
    }
}