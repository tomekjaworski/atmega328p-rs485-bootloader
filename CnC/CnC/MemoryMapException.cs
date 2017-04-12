using System;
using System.Runtime.Serialization;

namespace CnC
{
    [Serializable]
    internal class MemoryMapException : Exception
    {
        public MemoryMapException()
        {
        }

        public MemoryMapException(string message) : base(message)
        {
        }

        public MemoryMapException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected MemoryMapException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}