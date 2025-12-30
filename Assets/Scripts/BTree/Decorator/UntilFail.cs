namespace BTree.Decorator
{
    /// <summary>
    /// 重复运行子节点，直到该任务失败
    /// </summary>
    [TaskInlinable]
    public class UntilFail<T> : LoopDecorator<T> where T : class
    {
        public UntilFail()
        {
        }

        public UntilFail(Task<T> child) : base(child)
        {
        }

        protected override int OnChildCompleted(Task<T> child)
        {
            if (child.IsCancelled)
            {
                return TaskStatus.CANCELLED;
            }
            if (child.IsFailed)
            {
                return TaskStatus.SUCCESS;
            }
            else if (!HasNextLoop())
            {
                return TaskStatus.MAX_LOOP_LIMIT;
            }
            else
            {
                return TaskStatus.RUNNING;
            }
        }
    }
}