using System;
using System.Runtime.Serialization;

namespace Commons
{
    #nullable enable
    /// <summary>
    /// 该异常表示对象的状态错误
    /// </summary>
    public class IllegalStateException : InvalidOperationException
    {
        public IllegalStateException()
        {
        }

        protected IllegalStateException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public IllegalStateException(string? message) : base(message)
        {
        }

        public IllegalStateException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}