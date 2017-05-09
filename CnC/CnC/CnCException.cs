using System;
using System.Runtime.Serialization;

namespace CnC
{
    [Serializable]
    internal class CnCException : Exception
    {
        public CnCException()
        {
        }

        public CnCException(string message) : base(message)
        {
        }

        public CnCException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected CnCException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}