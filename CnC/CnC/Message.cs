using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CnC
{
    public class Message
    {
        byte address;
        MessageType type;
        byte[] payload;

        public byte Address => this.address;
        public MessageType Type => this.type;
        public byte[] Payload => this.payload;

        public byte[] Binary => ToBinary();
        public int BinarySize => 1 + 1 + 1 + this.payload.Length + 2;

        public Message(byte address, MessageType type)
            : this(address, type, new byte[0])
        { }

        public Message(byte address, MessageType type, byte[] payload)
        {
            if (payload == null)
                throw new NullReferenceException("payload");
            if (payload.Length > 200)
                throw new ArgumentOutOfRangeException("payload");

            this.address = address;
            this.type = type;
            this.payload = payload.Clone() as byte[];

        }

        private byte[] ToBinary()
        {
            byte[] msg = new byte[this.BinarySize];
            msg[0] = address;
            msg[1] = (byte)type;
            msg[2] = (byte)payload.Length;

            Array.Copy(payload, 0, msg, 3, payload.Length);

            UInt16 checksum = 0;
            for (int i = 0; i < payload.Length + 3; i++)
                checksum += msg[i];

            msg[3 + payload.Length + 0] = (byte)((checksum & 0xFF00) >> 8);
            msg[3 + payload.Length + 1] = (byte)(checksum & 0x00FF);

            return msg;
        }


    }
}
