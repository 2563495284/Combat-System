namespace BTree.Branch.Join
{
    /// <summary>
    /// 默认的AnyOf，不特殊处理取消
    /// 相当于并发编程中的anyOf
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class JoinAnyOf<T> : JoinPolicy<T> where T : class
    {
        /** 单例 */
        private static readonly JoinAnyOf<T> INST = new JoinAnyOf<T>();

        public static JoinAnyOf<T> GetInstance() => INST;

        public void ResetForRestart()
        {
        }

        public void BeforeEnter(Join<T> join)
        {
        }

        public int Enter(Join<T> join)
        {
            // 不能成功，失败也不能
            if (join.ChildCount == 0)
            {
                TaskLogger.Info("JonAnyOf: children is empty");
            }
            return TaskStatus.RUNNING;
        }

        public int OnChildCompleted(Join<T> join, Task<T> child)
        {
            return child.Status;
        }

        public void OnEvent(Join<T> join, object eventObj)
        {
        }
    }
}