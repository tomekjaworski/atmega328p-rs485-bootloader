using IntelHEX;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CnC
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("SmartTable bootloader C&C software by Tomasz Jaworski");


            MemoryMap mm = new MemoryMap(0x10000);
            IntelHEX16Storage st = new IntelHEX16Storage(mm);
            st.Load(@"d:\praca\projekty\SmartTable\SmartTableDriver\SmartTableFirmware\Debug\SmartTableFirmware.hex");
            mm.Dump("test.txt");


            if (SerialPort.GetPortNames().Length == 0)
            {
                Console.WriteLine("No serial ports available, quitting...");
                return;
            }

            Console.WriteLine("Available serial ports: {0}",
                String.Join(", ", SerialPort.GetPortNames()));


            // open all ports
            //SerialPort[] ports = SerialPort.GetPortNames().Select(x => new SerialPort(x, 19200, Parity.Even, 8, StopBits.One)).ToArray();
            List<SerialPort> ports = new List<SerialPort>();
            foreach (String port_name in SerialPort.GetPortNames()) {
                try {
                    SerialPort sp = new SerialPort(port_name, 19200, Parity.Even, 8, StopBits.One);
                    Console.Write("Openning {0}... ", sp.PortName);
                    sp.Open();
                    Console.WriteLine("Ok");

                    ports.Add(sp); // use only opened ports
                
                } catch(Exception ex) {
                    Console.WriteLine("Error: {0}", ex.Message);
                }
            }
                


            // send advertisement to all bootloader clients
            byte[] req;
            Console.Write("Sending CnC Advertisemen to {0} serial ports: ", ports.Count);
            for (int i = 0; i < 10; i++) {
                req = new byte[] { (byte)'A' };
                foreach (SerialPort sp in ports)
                    sp.Write(req, 0, 1);
                Thread.Sleep(100);
                Console.Write('.');
            }
            Console.WriteLine();

            // send challenge 
            Console.WriteLine("Sending Challenge... ");
            req = new byte[] { (byte)'C' };
            foreach (SerialPort sp in ports)
                sp.Write(req, 0, 1);
            Console.WriteLine("Ok. ");

            // wait for them
            Console.WriteLine("Waiting... ");
            int dt = 50;
            List<int> bootloaders = new List<int>();
            for (int i = 0; i < 2000/dt; i++) {
                foreach (SerialPort sp in ports) {

                    int addr = sp.ReadByte();
                    if (addr == -1)
                        continue;
                    bootloaders.Add(addr);
                    Console.Write("[0x{0:X2} on {1}] ", addr, sp.PortName);
                }

                Thread.Sleep(dt);
            }


        }
    }
}
