using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CnC
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("SmartTable bootloader C&C software by Tomasz Jaworski");

            if (SerialPort.GetPortNames().Length == 0)
            {
                Console.WriteLine("No serial ports available, quitting...");
                return;
            }

            Console.Write("Available serial ports: {0}",
                String.Join(", ", SerialPort.GetPortNames()));



        }
    }
}
