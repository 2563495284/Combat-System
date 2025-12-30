using System.Diagnostics;

namespace BTree.Branch.Join
{
    /// <summary>
    /// Main策略，当第一个任务完成时就完成。
    /// 类似<see cref="SimpleParallel{T}"/>，但Join在得出结果前不重复运行已完成的子节点
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class JoinMain<T> : JoinPolicy<T> where T : class
    {
        /** 单例 */
        private static readonly JoinMain<T> INST = new JoinMain<T>();

        public static JoinMain<T> GetInstance() => INST;

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
                return TaskStatus.CHILDLESS;
            }
            return TaskStatus.RUNNING;
        }

        public int OnChildCompleted(Join<T> join, Task<T> child)
        {
            Task<T> mainTask = join.GetFirstChild();
            if (child == mainTask)
            {
                return child.Status;
            }
            return TaskStatus.RUNNING;
        }

        public void OnEvent(Join<T> join, object eventObj)
        {
            Task<T> mainTask = join.GetFirstChild();
            ParallelChildHelper<T> childHelper = ParallelBranch<T>.GetChildHelper(mainTask);

            Task<T> inlinedChild = childHelper.GetInlinedChild();
            if (inlinedChild != null)
            {
                inlinedChild.OnEvent(eventObj);
            }
            else
            {
                join.GetFirstChild().OnEvent(eventObj);
            }
        }
    }
}