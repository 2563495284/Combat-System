namespace BTree.Branch.Join
{
    /// <summary>
    /// 等待所有任务完成后返回成功
    /// 相当于并发编程中的WaitAll
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class JoinWaitAll<T> : JoinPolicy<T> where T : class
    {
        /** 单例 */
        private static readonly JoinWaitAll<T> INST = new JoinWaitAll<T>();

        public static JoinWaitAll<T> GetInstance() => INST;

        public void ResetForRestart()
        {
        }

        public void BeforeEnter(Join<T> join)
        {
        }

        public int Enter(Join<T> join)
        {
            if (join.ChildCount == 0)
            {
                return TaskStatus.SUCCESS;
            }
            return TaskStatus.RUNNING;
        }

        public int OnChildCompleted(Join<T> join, Task<T> child)
        {
            if (join.IsAllChildCompleted)
            {
                return TaskStatus.SUCCESS;
            }
            return TaskStatus.RUNNING;
        }

        public void OnEvent(Join<T> join, object eventObj)
        {
        }
    }
}