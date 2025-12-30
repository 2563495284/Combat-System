using System.Runtime.CompilerServices;

namespace BTree.Branch
{
    /// <summary>
    /// Q：为什么不直接叫{@code ChildHelper}？
    /// A: 通常只应该在有多个运行中的子节点(含hook)的情况下才需要使用该工具类。
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ParallelChildHelper<T> where T : class
    {
        private TaskInlineHelper<T> _inlineHelper = new TaskInlineHelper<T>();
#nullable disable
        /** 子节点的重入id */
        public int reentryId;
        /** 子节点的取消令牌 -- 应当在运行前赋值 */
        public CancelToken cancelToken;
        /** 用户自定义数据 */
        public object userData;

        public virtual void Reset()
        {
            _inlineHelper.StopInline();
            reentryId = 0;
            userData = null;
            if (cancelToken != null)
            {
                cancelToken.Reset();
            }
        }

        public ref TaskInlineHelper<T> Unwrap() => ref _inlineHelper;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task<T> GetInlinedChild()
        {
            return _inlineHelper.GetInlinedChild();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void StopInline()
        {
            _inlineHelper.StopInline();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void InlineChild(Task<T> runningChild)
        {
            _inlineHelper.InlineChild(runningChild);
        }
    }
}