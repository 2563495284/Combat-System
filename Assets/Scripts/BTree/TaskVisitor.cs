namespace BTree
{
#nullable enable
    /// <summary>
    /// Task访问器，用于访问Task的内部结构。
    /// 注意：访问器在访问过程中不能导致Task产生状态迁移，即不能使Task进入完成状态。
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface TaskVisitor<T> where T : class
    {
        /// <summary>
        /// 访问普通子节点
        /// </summary>
        /// <param name="child">child 子节点</param>
        /// <param name="index">index 子节点下标</param>
        /// <param name="param">用户参数</param>
        void VisitChild(Task<T> child, int index, object? param);

        /// <summary>
        /// 访问钩子节点(无法通过GetChild拿到的子节点，也不在ChildCount计数中)
        /// 理论上钩子还可能是List或Map，但我们这个访问者只是为了做一些简单的遍历工作，并不需要如此精细的信息，
        /// 因此方法参数可以未声明index/key等信息，以避免额外的开销和复杂度。
        /// </summary>
        /// <param name="child">钩子子节点</param>
        /// <param name="param">用户参数</param>
        void VisitHook(Task<T> child, object? param);
    }
}