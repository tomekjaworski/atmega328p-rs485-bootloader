using System;
using System.Collections.Generic;
using System.IO;
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

        public int FindSequence(byte[] pattern)
        {
            for (int i = 0; i < mem.Length; i++) {
                {
                    bool match = true;
                    for (int j = 0; j < pattern.Length; j++)
                        if (mem[i + j] != pattern[j]) {
                            match = false;
                            break;
                        }
                    if (match)
                        return i;
                }
            }

            return -1;
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

        public void Write(uint address, byte[] data, int offset, int count)
        {
            for (int i = 0; i < count; i++)
                this.Write(address++, data[offset + i]);
        }

        #endregion

        #region Read routines

        public byte ReadByte(UInt32 address)
        {
            if (address >= this.mem.Length)
                throw new MemoryMapException("Address out of bounds");

            return this.mem[address];
        }

        public Int16 ReadInt16(UInt32 address)
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

        public UInt16 ReadUInt16(UInt32 address)
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

        public Int32 ReadInt32(UInt32 address)
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

        internal void Zero()
        {
            for (int i = 0; i < this.mem.Length; i++) {
                mem[i] = 0;
                dirty[i] = false;
            }
        }

        public UInt16 ReadUInt32(UInt32 address)
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

        public void Read(uint addr, byte[] data, int offset, int count)
        {
            for (int i = 0; i < count; i++)
                data[offset + i] = this.ReadByte(addr++);
        }

        #endregion

        public void Dump(string filename, DumpMode mode = DumpMode.Text)
        {
            if (mode == DumpMode.Text) InternalTextDump(filename);
            if (mode == DumpMode.Binary) InternalBinaryDump(filename);
        }

        private void InternalBinaryDump(string filename)
        {
            using (FileStream fs = File.Create(filename))
                fs.Write(this.mem, 0, this.mem.Length);
        }

        private void InternalTextDump(string filename)
        {
            int bytes_per_row = 16;

            int address = 0;
            int bytes_left = this.mem.Length;
            using (FileStream fs = File.Create(filename))
            using (StreamWriter sw = new StreamWriter(fs, Encoding.ASCII)) {

                while (bytes_left > 0) {
                    sw.Write("{0:X8} ", address);

                    int to_read = Math.Min(bytes_per_row, bytes_left);

                    // dump data in hex
                    for (int i = 0; i < to_read; i++)
                        sw.Write("{0:X2} ", this.mem[address + i]);

                    // dump data in ascii
                    for (int i = 0; i < to_read; i++)
                        if (this.mem[address + i] >= 32 && this.mem[address + i] <= 126)
                            sw.Write((char)this.mem[address + i]);
                        else
                            sw.Write(".");
                    sw.Write(" ");

                    // dump dirty flag
                    for (int i = 0; i < to_read; i++)
                        if (this.dirty[address + i])
                            sw.Write('D');
                        else
                            sw.Write('.');

                    address += to_read;
                    bytes_left -= to_read;
                    sw.WriteLine("");
                }

            }
            //
        }

        public bool BinaryCompare(MemoryMap mmread, ref uint difference_address)
        {
            if (mmread.Size != this.Size) {
                difference_address = uint.MaxValue; //todo other method for extracting this information
                return false; // size don't match
            }

            for (uint i = 0; i < this.Size; i++)
                if (mmread.mem[i] != this.mem[i]) {
                    difference_address = i;
                    return false;
                }

            return true;
        }
    }
}
