using System;
using System.Runtime.Serialization;

namespace CnC
{
    [Serializable]
    internal class MemoryVerificationException : Exception
    {
        private uint difference_address;
        private byte existing_byte;
        private byte expected_byte;
        private string name;

        public MemoryVerificationException()
        {
        }

        public MemoryVerificationException(string message) : base(message)
        {
        }

        public MemoryVerificationException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public MemoryVerificationException(string name, uint difference_address, byte expected_byte, byte existing_byte)
        {
            this.name = name;
            this.difference_address = difference_address;
            this.expected_byte = expected_byte;
            this.existing_byte = existing_byte;
        }

        protected MemoryVerificationException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public override string ToString()
        {
            return string.Format("Memory ({0}) verification exception at 0x{1:X8}. Expected byte 0x{2:X2} but found 0x{3:X2}",
                name, difference_address, expected_byte, expected_byte);
        }
    }
}