namespace BTree
{
    /// <summary>
    /// 取消令牌监听器。
    ///
    /// ps：该接口用于特殊需求时减少闭包。
    /// </summary>
    public interface ICancelTokenListener
    {
        /// <summary>
        /// 该方法在取消令牌收到取消信号时执行
        /// 注意：由于取消令牌支持复用，如果监听器不能立即响应取消请求，则应当将取消码保存为局部变量。
        /// </summary>
        /// <param name="cancelToken">收到取消信号的令牌</param>
        void OnCancelRequested(CancelToken cancelToken);
    }
}