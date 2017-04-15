using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            : this(address, type, new byte[0], 0, 0)
        { }

        public Message(byte address, MessageType type, byte[] payload)
            : this(address, type, payload, 0, payload.Length)
        { }


        public Message(byte address, MessageType type, byte[] payload, int offset, int count)
        {
            if (payload == null)
                throw new NullReferenceException("payload");
            if (count > 200)
                throw new ArgumentOutOfRangeException("payload");

            this.address = address;
            this.type = type;
            this.payload = new byte[count];
            Array.Copy(payload, offset, this.payload, 0, count);
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

    public class MessageExtractor
    {
        FifoBuffer rxqueue;

        public MessageExtractor()
        {
            this.rxqueue = new FifoBuffer(1024);
        }

        public void Discard()
        {
            rxqueue.Discard();
        }

        public void AddData(byte[] data, int count)
        {
            rxqueue.Write(data, 0, count);
            Debug.Assert(rxqueue.Count != 0);
        }

        public bool TryExtract(ref Message msg, int expected_address, MessageType expected_type)
        {
            while (true) {

                if (rxqueue.Count < 5)
                    return false;  // not enough data

                // peek header
                byte[] hdr = rxqueue.PeekBytes(0, 3);
                if (hdr[0] != expected_address) {
                    // address invalid
                    rxqueue.ReadByte();
                    continue;
                }

                if (hdr[1] != (byte)expected_type) {
                    // command does not match
                    rxqueue.ReadByte();
                    continue;
                }

                if (rxqueue.Count < 3 + hdr[2] + 2)
                    return false; // not enough data for further testing; wait for them

                // check checksum
                byte[] data = rxqueue.PeekBytes(0, 3 + hdr[2] + 2);
                UInt16 checksum = 0;
                for (int j = 0; j < data[2] + 3; j++)
                    checksum += data[j];

                if (data[data.Length - 2] != checksum >> 8 || data[data.Length - 1] != (checksum & 0x00FF)) {
                    // checksum error
                    rxqueue.ReadByte();
                    continue;
                }

                // ok, finally it seems that we have got a message! :)
                msg = new Message(data[0], (MessageType)data[1], data, 3, data[2]);
                rxqueue.DeleteFirstBytes(3 + hdr[2] + 2);
                return true;
            }
        }


    }
}
