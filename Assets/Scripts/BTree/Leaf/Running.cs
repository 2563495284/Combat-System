namespace BTree.Leaf
{
    /// <summary>
    /// 保存运行状态的子节点
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Running<T> : LeafTask<T> where T : class
    {
        protected override int Execute()
        {
            return TaskStatus.RUNNING;
        }

        protected override void OnEventImpl(object eventObj)
        {
        }
    }
}