using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CnC
{
    public class MemoryMap
    {
        private byte[] mem;
        private bool[] dirty;
        private ByteOrder order;

        public Int32 Size => this.mem.Length;
        public ByteOrder DefaultByteOrder => this.order;

        public MemoryMap(int size, ByteOrder order = ByteOrder.LittleEndian)
        {
            this.mem = new byte[size];
            this.dirty = new bool[size];
            this.order = order;
        }

        #region Write routines

        public void Write(UInt32 address, byte data)
        {
            if (address >= this.mem.Length)
                throw new MemoryMapException("Address out of bounds");

            this.mem[address] = data;
            this.dirty[address] = true;
        }
        public void Write(UInt32 address, UInt16 data)
        {
            byte b0 = (byte)(data & 0xFF);
            byte b1 = (byte)((data & 0xFF00) >> 8);

            if (this.order == ByteOrder.LittleEndian) {
                this.Write(address, b0);
                this.Write(address + 1, b1);
            } else {
                this.Write(address, b1);
                this.Write(address + 1, b0);
            }
        }
        public void Write(UInt32 address, Int16 data)
        {
            byte b0 = (byte)(data & 0xFF);
            byte b1 = (byte)((data & 0xFF00) >> 8);

            if (this.order == ByteOrder.LittleEndian) {
                this.Write(address, b0);
                this.Write(address + 1, b1);
            } else {
                this.Write(address, b1);
                this.Write(address + 1, b0);
            }
        }
        public void Write(UInt32 address, UInt32 data)
        {
            byte b0 = (byte)(data & 0xFF);
            byte b1 = (byte)((data & 0xFF00) >> 8);
            byte b2 = (byte)((data & 0xFF0000) >> 16);
            byte b3 = (byte)((data & 0xFF000000) >> 24);

            if (this.order == ByteOrder.LittleEndian) {
                this.Write(address, b0);
                this.Write(address + 1, b1);
                this.Write(address + 2, b2);
                this.Write(address + 3, b3);
            } else {
                this.Write(address, b3);
                this.Write(address + 1, b2);
                this.Write(address + 2, b1);
                this.Write(address + 3, b0);
            }
        }
        public void Write(UInt32 address, Int32 data)
        {
            byte b0 = (byte)(data & 0xFF);
            byte b1 = (byte)((data & 0xFF00) >> 8);
            byte b2 = (byte)((data & 0xFF0000) >> 16);
            byte b3 = (byte)((data & 0xFF000000) >> 24);

            if (this.order == ByteOrder.LittleEndian) {
                this.Write(address, b0);
                this.Write(address + 1, b1);
                this.Write(address + 2, b2);
                this.Write(address + 3, b3);
            } else {
                this.Write(address, b3);
                this.Write(address + 1, b2);
                this.Write(address + 2, b1);
                this.Write(address + 3, b0);
            }
        }

        #endregion

        #region Read routines

        byte ReadByte(UInt32 address)
        {
            if (address >= this.mem.Length)
                throw new MemoryMapException("Address out of bounds");

            return this.mem[address];
        }

        Int16 ReadInt16(UInt32 address)
        {
            uint b0 = ReadByte(address);
            uint b1 = ReadByte(address + 1);

            Int16 value;
            if (order == ByteOrder.LittleEndian)
                value = (Int16)(b0 | b1 << 8);
            else
                value = (Int16)(b1 | b0 << 8);

            return value;
        }

        UInt16 ReadUInt16(UInt32 address)
        {
            uint b0 = ReadByte(address);
            uint b1 = ReadByte(address + 1);

            UInt16 value;
            if (order == ByteOrder.LittleEndian)
                value = (UInt16)(b0 | b1 << 8);
            else
                value = (UInt16)(b1 | b0 << 8);

            return value;
        }

        Int32 ReadInt32(UInt32 address)
        {
            uint b0 = ReadByte(address);
            uint b1 = ReadByte(address + 1);
            uint b2 = ReadByte(address + 2);
            uint b3 = ReadByte(address + 3);

            Int32 value;
            if (order == ByteOrder.LittleEndian)
                value = (Int32)(b0 | b1 << 8 | b2 << 16 | b3 << 24);
            else
                value = (Int32)(b3 | b2 << 8 | b1 << 16 | b0 << 24);

            return value;
        }

        UInt16 ReadUInt32(UInt32 address)
        {
            uint b0 = ReadByte(address);
            uint b1 = ReadByte(address + 1);
            uint b2 = ReadByte(address + 2);
            uint b3 = ReadByte(address + 3);

            UInt16 value;
            if (order == ByteOrder.LittleEndian)
                value = (UInt16)(b0 | b1 << 8 | b2 << 16 | b3 << 24);
            else
                value = (UInt16)(b3 | b2 << 8 | b1 << 16 | b0 << 24);

            return value;
        }
        #endregion
    }
}
