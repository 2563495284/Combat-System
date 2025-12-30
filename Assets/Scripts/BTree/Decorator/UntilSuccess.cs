namespace BTree.Decorator
{
    /// <summary>
    ///  重复运行子节点，直到该任务成功
    /// </summary>
    [TaskInlinable]
    public class UntilSuccess<T> : LoopDecorator<T> where T : class
    {
        public UntilSuccess()
        {
        }

        public UntilSuccess(Task<T> child) : base(child)
        {
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