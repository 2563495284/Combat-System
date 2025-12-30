namespace BTree.Decorator
{
#nullable enable
    /// <summary>
    /// 在子节点完成之后固定返回失败
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [TaskInlinable]
    public class AlwaysFail<T> : Decorator<T> where T : class
    {
        private int failureStatus;

        public AlwaysFail()
        {
        }

        public AlwaysFail(Task<T> child) : base(child)
        {
        }

        protected override int Execute()
        {
            if (child == null)
            {
                return TaskStatus.ToFailure(failureStatus);
            }
            Task<T>? inlinedChild = inlineHelper.GetInlinedChild();
            if (inlinedChild != null)
            {
                inlinedChild.Template_ExecuteInlined(ref inlineHelper, child);
            }
            else if (child.IsRunning)
            {
                child.Template_Execute(true);
            }
            else
            {
                Template_StartChild(child, true, ref inlineHelper);
            }
            return child.IsCompleted ? TaskStatus.ToFailure(child.Status) : TaskStatus.RUNNING;
        }

        /// <summary>
        /// 失败时使用的状态码
        /// </summary>
        public int FailureStatus
        {
            get => failureStatus;
            set => failureStatus = value;
        }
    }
}