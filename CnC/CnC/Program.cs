using IntelHEX;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CnC
{
    struct Endpoint
    {
        public MemoryStream ms;
        public SerialPort sp;

        public Endpoint(SerialPort sp)
        {
            this.sp = sp;
            this.ms = new MemoryStream();
        }
    }

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
            List<Endpoint> endpoints = new List<Endpoint>();
            foreach (String port_name in SerialPort.GetPortNames()) {
                try {
                    SerialPort sp = new SerialPort(port_name, 19200, Parity.Even, 8, StopBits.One);
                    Console.Write("Openning {0}... ", sp.PortName);
                    sp.ReadTimeout = 200;
                    sp.Open();
                    Console.WriteLine("Ok");

                    endpoints.Add(new Endpoint(sp)); // use only opened ports
                
                } catch(Exception ex) {
                    Console.WriteLine("Error: {0}", ex.Message);
                }
            }
                


            // send advertisement to all bootloader clients
            byte[] req;
            Console.WriteLine("*** TURN ON all bootloader clients and press any key to processed...");
            Console.Write("Sending CnC Advertisemen to {0} serial ports: ", endpoints.Count);
            while (!Console.KeyAvailable) {
                req = new byte[] { (byte)'A' };
                foreach (Endpoint ep in endpoints)
                    ep.sp.Write(req, 0, 1);
                Thread.Sleep(100);
                Console.Write('.');

            }
            Console.WriteLine();

            // purge buffers
            Thread.Sleep(100);
            foreach (Endpoint ep in endpoints) {
                ep.sp.DiscardInBuffer();
                ep.sp.DiscardOutBuffer();
            }

            // send challenge 
            Console.WriteLine("Sending Challenge... ");
            req = new byte[] { (byte)'C' };
            foreach (Endpoint ep in endpoints)
                ep.sp.Write(req, 0, 1);
            Console.WriteLine("Ok. ");

            // wait for them
            Console.WriteLine("Waiting... ");
            Thread.Sleep(200);

            int dt = 50;
            byte[] buffer = new byte[32];

            DateTime start = DateTime.Now;
            while((DateTime.Now - start).TotalMilliseconds <= 3000) {
                foreach (Endpoint ep in endpoints) {
                    try {
                        int read = ep.sp.Read(buffer, 0, buffer.Length);
                        ep.ms.Write(buffer, 0, read);
                        Console.WriteLine("[{0} on {1}]", string.Join(",", buffer.Take(read).Select(x => "0x" + x.ToString("X2")).ToArray()), ep.sp.PortName);
                    } catch(TimeoutException tex) {
                        //
                    }
                }

                Thread.Sleep(dt);
            }

            // concatenate streams
            MemoryStream ms = new MemoryStream();
            foreach (Endpoint ep in endpoints) {
                ep.ms.Seek(0, SeekOrigin.Begin);
                ms.Write(ep.ms.GetBuffer(), 0, (int)ep.ms.Length);
            }

            // parse concatenated streams



            }
        }
}
