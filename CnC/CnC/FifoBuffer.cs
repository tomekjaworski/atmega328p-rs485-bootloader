using System;
using System.Collections.Generic;
using System.Text;

namespace CnC
{

	public class FifoBuffer
	{
		private byte[] buffer;
		private int position;


		public int Count { get { return this.position; } }

		public FifoBuffer(int buffer_size)
		{
			this.buffer = new byte[buffer_size];
			this.position = 0;
		}


		public void Write(byte[] bytes, int offset, int count)
		{
            //TODO exception 
			Array.Copy(bytes, offset, this.buffer, this.position, count);
			this.position += count;
		}


		public byte[] PeekBytes(int offset, int count)
		{
			//TODO: zamiast tych korekcji lepiej rzucacaæ wyj¹tkiem
			offset = Math.Max(offset, 0);
			count = Math.Max(count, 0);

			offset = Math.Min(offset, this.position); // ograniczenie pozycji startowej
			count = Math.Min(position - offset, count); // ograniczenie ilosci danych

			byte[] b = new byte[count];
			Array.Copy(this.buffer, offset, b, 0, count);
			return b;
		}

		public void DeleteFirstBytes(int count)
		{
			int cnt = this.position - count;
			Array.Copy(this.buffer, count, this.buffer, 0, cnt);
			Array.Clear(this.buffer, cnt, count);
			position -= count;
		}

		public Int32 PeekUInt32(int offset)
		{
			return System.Net.IPAddress.NetworkToHostOrder(BitConverter.ToInt32(this.buffer, offset));
		}

        internal int ReadByte()
        {
            if (Count == 0)
                return -1;

            int b = this.buffer[0];
            DeleteFirstBytes(1);
            return b;
        }

        internal void Discard()
        {
            position = 0;
        }
    }
}
