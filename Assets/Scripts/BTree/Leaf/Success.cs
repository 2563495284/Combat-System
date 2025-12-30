namespace BTree.Leaf
{
    /// <summary>
    /// 固定返回成功的子节点
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Success<T> : LeafTask<T> where T : class
    {
        protected override int Execute()
        {
            return TaskStatus.SUCCESS;
        }

        protected override void OnEventImpl(object eventObj)
        {
        }
    }
}