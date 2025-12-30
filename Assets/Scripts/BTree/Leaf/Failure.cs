namespace BTree.Leaf
{
    /// <summary>
    /// 固定返回失败的子节点
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Failure<T> : LeafTask<T> where T : class
    {
        private int failureStatus;

        protected override int Execute()
        {
            return TaskStatus.ToFailure(failureStatus);
        }

        protected override void OnEventImpl(object eventObj)
        {
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