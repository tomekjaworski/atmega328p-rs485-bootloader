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
        public int address;
        public SerialPort sp;

        public Endpoint(SerialPort sp, int addr)
        {
            this.sp = sp;
            this.address = addr;
        }

    }
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("SmartTable bootloader C&C software by Tomasz Jaworski");


            MemoryMap mm = new MemoryMap(0x10000);
            IntelHEX16Storage st = new IntelHEX16Storage(mm);
            st.Load(@"d:\praca\projekty\SmartTable\atmega328p-rs485-bootloader\Debug\atmega328p_bootloader.hex");
            mm.Dump("test.txt");


            if (SerialPort.GetPortNames().Length == 0) {
                Console.WriteLine("No serial ports available, quitting...");
                return;
            }

            Console.WriteLine("Available serial ports: {0}",
                String.Join(", ", SerialPort.GetPortNames()));

            List<SerialPort> ports = ListAndOpenSerialPorts();

            // send advertisement to all devices
            SendAdvertisement(ports);

            // purge buffers
            PurgeSerialPorts(ports);

            // get list of devices for each serial port
            List<Endpoint> endpoints = new List<Endpoint>();
            foreach (SerialPort sp in ports)
                AcquireDevicesOnSerialPort(endpoints, sp);

            // show geathered devices
            ShowDevices(endpoints.ToArray());



        }

        private static void ShowDevices(Endpoint[] endpoints)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\nListing {0} acquired device(s): ", endpoints.Length);
            Console.ForegroundColor = ConsoleColor.Gray;

            foreach(Endpoint ep in endpoints) {
                Console.WriteLine("   Device 0x{0:X2} on {1}", ep.address, ep.sp.PortName);
            }
        }

        private static void AcquireDevicesOnSerialPort(List<Endpoint> endpoints, SerialPort sp)
        {
            Console.WriteLine("Sending Challenge to serial port {0}... ", sp.PortName);
            byte[] req = new byte[] { (byte)'C' };
            sp.Write(req, 0, 1);
            Console.WriteLine("Ok. ");

            // wait for them
            Console.WriteLine("Waiting... ");
            Thread.Sleep(200);

            int dt = 50;
            byte[] buffer = new byte[32];

            DateTime start = DateTime.Now;
            MemoryStream ms = new MemoryStream();
            while ((DateTime.Now - start).TotalMilliseconds <= 3000) {
                try {
                    int read = sp.Read(buffer, 0, buffer.Length);
                    ms.Write(buffer, 0, read);
                    Console.WriteLine("[{0} on {1}]", string.Join(",", buffer.Take(read).Select(x => "0x" + x.ToString("X2")).ToArray()), sp.PortName);
                }
                catch (TimeoutException tex) {
                    //
                }
                Thread.Sleep(dt);
            }


            // parse the acquired stream
            ms.Seek(0, SeekOrigin.Begin);
            BinaryReader br = new BinaryReader(ms);
            while (br.PeekChar() != -1) {
                int code = br.Read();
                if (code != 'c') // challenge response
                    continue;

                int addr = br.Read(); // bootloader address
                if (addr == -1)
                    continue;

                endpoints.Add(new Endpoint(sp, addr));
            }
        }

        private static void PurgeSerialPorts(List<SerialPort> ports)
        {
            Thread.Sleep(100);
            foreach (SerialPort sp in ports) {
                sp.DiscardInBuffer();
                sp.DiscardOutBuffer();
            }
        }

        private static void SendAdvertisement(List<SerialPort> ports)
        {
            byte[] req;
            Console.WriteLine("*** TURN ON all devices and press any key to processed...");

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("\nSending CnC Advertisemen to {0} serial ports: ", ports.Count);
            Console.ForegroundColor = ConsoleColor.Gray;
            while (!Console.KeyAvailable) {
                req = new byte[] { (byte)'A' };
                foreach (SerialPort sp in ports)
                    sp.Write(req, 0, 1);
                Thread.Sleep(100);
                Console.Write('.');

            }

            Console.WriteLine(" Done.");
        }

        private static List<SerialPort> ListAndOpenSerialPorts()
        {

            // open all ports
            List<SerialPort> ports = new List<SerialPort>();
            foreach (String port_name in SerialPort.GetPortNames()) {
                try {
                    SerialPort sp = new SerialPort(port_name, 19200, Parity.Even, 8, StopBits.One);
                    Console.Write("Openning {0}... ", sp.PortName);
                    sp.ReadTimeout = 200;
                    sp.Open();
                    Console.WriteLine("Ok");

                    ports.Add(sp); // use only opened ports

                }
                catch (Exception ex) {
                    Console.WriteLine("Error: {0}", ex.Message);
                }
            }

            return ports;
        }
    }
}
