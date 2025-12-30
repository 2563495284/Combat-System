using System;

namespace Commons
{
    /// <summary>
    /// 该异常表示断言错误，不应该发送的错误发生了。
    /// </summary>
    public class AssertionError : Exception
    {
        public AssertionError()
        {
        }

        public AssertionError(string? message) : base(message)
        {
        }

        public AssertionError(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}