
using System;
using System.Runtime.Serialization;

namespace Commons.Ex
{
#nullable enable
    /// <summary>
    /// 死循环预防
    /// </summary>
    public class InfiniteLoopException : Exception
    {
        public InfiniteLoopException()
        {
        }

        public InfiniteLoopException(string? message) : base(message)
        {
        }

        public InfiniteLoopException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        protected InfiniteLoopException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}