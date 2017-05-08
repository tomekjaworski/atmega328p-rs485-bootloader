using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CnC
{
    public struct Device
    {
        public int address;
        public SerialPort sp;

        public Device(SerialPort sp, int addr)
        {
            this.sp = sp;
            this.address = addr;
        }

        public override string ToString()
        {
            return string.Format("0x{0:X2} on {1}", address, sp.PortName);
        }
    }
}
