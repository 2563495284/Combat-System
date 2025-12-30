namespace BTree.Leaf
{
    /// <summary>
    /// 等待一定帧数
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class WaitFrame<T> : LeafTask<T> where T : class
    {
        private int required;

        public WaitFrame()
        {
        }

        public WaitFrame(int required)
        {
            this.required = required;
        }

        protected override int Execute()
        {
            if (RunFrames >= required)
            {
                return TaskStatus.SUCCESS;
            }
            return TaskStatus.RUNNING;
        }

        protected override void OnEventImpl(object eventObj)
        {
        }

        /// <summary>
        /// 需要等待的帧数
        /// </summary>
        public int Required
        {
            get => required;
            set => required = value;
        }
    }
}