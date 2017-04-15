using IntelHEX;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

            // send advertisement to all devices, co they can stay in bootloader mode
            SendAdvertisement(ports);

            // purge buffers
            PurgeSerialPorts(ports);

            // get list of devices for each serial port
            List<Endpoint> endpoints = new List<Endpoint>();
            foreach (SerialPort sp in ports)
                AcquireDevicesOnSerialPort(endpoints, sp);


            /*



            // show geathered devices
            ShowDevices(endpoints.ToArray());
            PurgeSerialPorts(ports);


            Message msg_ping = new Message(0x51, MessageType.BL_COMMAND_PING);
            byte[] msg_ping_binary = msg_ping.ToBinary();
            ports[0].Write(msg_ping_binary, 0, msg_ping_binary.Length);


            SerialPort sp = ports[0];
            byte[] rx = new byte[1024];
            FifoBuffer rxqueue = new FifoBuffer(1024);
            while(true) {
                int read = sp.Read(rx, 0, rx.Length);
                rxqueue.Write(rx, 0, read);
            }

            /*
            for (Endpoint ep in endpoints) {
                
            }*/


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
            Console.WriteLine("Sending PING to serial port {0}... ", sp.PortName);
            Console.CursorVisible = false;

            bool intro = true;
            int cx=0, cy=0;

            int timeout = 200;

            sp.ReadTimeout = 20;
            byte[] buffer = new byte[1024];
            MessageExtractor me = new MessageExtractor();

            // scan through 0x00 - 0xEF. Range 0xF0 - 0xFF is reserved
            for (int i = 0x00; i < 0xF0; i++) { 

                if (intro) {
                    Console.Write("Looking for device ");
                    cx = Console.CursorLeft;
                    cy = Console.CursorTop;
                    intro = false;
                }

                Console.SetCursorPosition(cx, cy);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("0x{0:X2}", i);
                Console.ForegroundColor = ConsoleColor.Gray;


                // send ping do selected device
                sp.DiscardInBuffer();
                sp.DiscardOutBuffer();
                me.Discard();

                Message ping_message = new Message((byte)i, MessageType.BL_COMMAND_PING);
                sp.Write(ping_message.Binary, 0, ping_message.BinarySize);

                DateTime start = DateTime.Now;
                do {
                    int read = -1;
                    try {
                        read = sp.Read(buffer, 0, buffer.Length);
                    } catch(TimeoutException tex) {
                        continue; // ignore timeouts
                    }

                    me.AddData(buffer, read);
                    Message msg = null;
                    if (!me.TryExtract(ref msg, i, MessageType.BL_COMMAND_PING))
                        continue; // failed

                    Console.WriteLine(" Found!");
                    intro = true;

                } while ((DateTime.Now - start).TotalMilliseconds <= timeout);

            }

            Console.CursorVisible = true;
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
            char[] anim = { '/', '-', '\\', '|' };
            int anim_counter = 0;

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("\nSending CnC Advertisemen to {0} serial ports: ", ports.Count);
            Console.ForegroundColor = ConsoleColor.Gray;

            Console.CursorVisible = false;
            int cx = Console.CursorLeft;

            while (!Console.KeyAvailable) {
                req = new byte[] { (byte)'A' };
                foreach (SerialPort sp in ports)
                    sp.Write(req, 0, 1);

                Thread.Sleep(100);
                Console.CursorLeft = cx;
                Console.Write(anim[anim_counter++ % 4]);
            }

            Console.WriteLine(" Done.");
            Console.CursorVisible = true;
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
