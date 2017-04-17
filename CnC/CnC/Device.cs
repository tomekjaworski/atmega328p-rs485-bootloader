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

    }
}
