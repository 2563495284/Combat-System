namespace BTree.Branch
{
    /// <summary>
    /// <see cref="Join{T}"/>的完成策略
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface JoinPolicy<T> where T : class
    {
        /** 重置自身数据 */
        void ResetForRestart();

        /** 启动前初始化 */
        void BeforeEnter(Join<T> join);

        /// <summary>
        /// 启动
        /// </summary>
        /// <param name="join"></param>
        /// <returns>最新状态</returns>
        int Enter(Join<T> join);

        /// <summary>
        /// Join在调用该方法前更新了完成计数和成功计数
        /// </summary>
        /// <param name="join"></param>
        /// <param name="child"></param>
        /// <returns>最新状态</returns>
        int OnChildCompleted(Join<T> join, Task<T> child);

        /** join节点收到外部事件 */
        void OnEvent(Join<T> join, object eventObj);
    }
}