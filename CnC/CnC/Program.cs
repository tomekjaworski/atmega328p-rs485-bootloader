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

            List<Endpoint> endpoints = new List<Endpoint>();

            
            // get list of devices for each serial port
            foreach (SerialPort sp in ports)
                AcquireDevicesOnSerialPort(endpoints, sp);

            // show discovered devices
            ShowDevices(endpoints.ToArray());

            return;
            /*
            uint addr = 0;
            byte[] page = new byte[128];
            byte[] payload = new byte[2 + page.Length];

            page[0] = (byte)'T';
            page[1] = (byte)'o';
            page[2] = (byte)'m';
            page[3] = (byte)'e';
            page[4] = (byte)'k';


            payload[0] = (byte)(addr & 0xFF);
            payload[1] = (byte)((addr >> 8) & 0xFF);
            Array.Copy(page, 0, payload, 2, page.Length);

            SerialPort sp = ports[0];
            MessageExtractor me = new MessageExtractor();

            sp.DiscardInBuffer();
            sp.DiscardOutBuffer();
            me.Discard();
            Message msg_writepage = new Message(0x51, MessageType.WriteFlashPage, payload);
            //sp.Write(msg_writepage.Binary, 0, msg_writepage.BinarySize);


            Thread.Sleep(1000);
            //return;
            
            Console.CursorVisible = false;

            endpoints.Add(new Endpoint(ports[0], 0x51));

            sp = ports[0];
            me = new MessageExtractor();

            sp.DiscardInBuffer();
            sp.DiscardOutBuffer();
            me.Discard();
        
            mm = new MemoryMap(1 * 1024);

            Console.Write("Reading FLASH memory ({0}kB): ", mm.Size / 1024);
            ConsoleProgressBar cpb = new ConsoleProgressBar(0, mm.Size);

            for (addr = 0; addr < 1 * 1024; addr += 128, cpb.Progress=addr) {
                Message msg_readpage = new Message(0x51, MessageType.ReadEepromPage, new byte[] { (byte)(addr & 0xFF), (byte)(addr >> 8), });

                Message response = SendAndWaitForResponse(endpoints[0], msg_readpage, 2000);
                mm.Write(addr, response.Payload, 0, 128);
            }

            Console.CursorVisible = true;
            */

           // mm.Dump("pobrane.txt");
        }

        private static void ReadEEPROM(Endpoint endpoint, MemoryMap dest)
        {
            Console.CursorVisible = false;

            Console.Write("Reading EEPROM memory ({0}kB):   ", dest.Size / 1024);
            ConsoleProgressBar cpb = new ConsoleProgressBar(0, dest.Size);

            for (uint addr = 0; addr < 1 * 1024; addr += 128, cpb.Progress = addr) {
                Message msg_readpage = new Message((byte)endpoint.address, MessageType.ReadEepromPage, new byte[] { (byte)(addr & 0xFF), (byte)(addr >> 8), });

                Message response = SendAndWaitForResponse(endpoint, msg_readpage, 2000);
                dest.Write(addr, response.Payload, 0, 128);
            }

            Console.CursorVisible = true;
            Console.WriteLine("Done.");
        }

        private static bool VerifyEEPROM(Endpoint endpoint, MemoryMap source)
        {
            Console.CursorVisible = false;

            Console.Write("Verifying EEPROM memory ({0}kB): ", source.Size / 1024);
            ConsoleProgressBar cpb = new ConsoleProgressBar(0, source.Size);
            MemoryMap mmread = new MemoryMap(source.Size);

            for (uint addr = 0; addr < 1 * 1024; addr += 128, cpb.Progress = addr) {
                Message msg_readpage = new Message((byte)endpoint.address, MessageType.ReadEepromPage, new byte[] { (byte)(addr & 0xFF), (byte)(addr >> 8), });

                Message response = SendAndWaitForResponse(endpoint, msg_readpage, 2000);
                mmread.Write(addr, response.Payload, 0, 128);
            }

            bool result = source.BinaryCompare(mmread);

            Console.CursorVisible = true;
            if (result)
                Console.WriteLine("Correct.");
            else
                Console.WriteLine("Failed.");

            return result;
        }

        private static void WriteEEPROM(Endpoint endpoint, MemoryMap source)
        {
            Console.CursorVisible = false;

            Console.Write("Writing EEPROM memory ({0}kB):   ", source.Size / 1024);
            ConsoleProgressBar cpb = new ConsoleProgressBar(0, source.Size);

            for (uint addr = 0; addr < 1 * 1024; addr += 128, cpb.Progress = addr) {

                byte[] payload = new byte[2 + 128];
                payload[0] = (byte)(addr & 0xFF);
                payload[1] = (byte)((addr >> 8) & 0xFF);
                source.Read(addr, payload, 2, 128);

                Message msg_write = new Message((byte)endpoint.address, MessageType.WriteEepromPage, payload);

                Message response = SendAndWaitForResponse(endpoint, msg_write, 2000);
            }

            Console.CursorVisible = true;
            Console.WriteLine("Done.");
        }

        static Message SendAndWaitForResponse(Endpoint ep, Message request, int timeout, bool throw_timeout_exception = true)
        {
            Debug.Assert(ep.address == request.Address);

            MessageExtractor me = new MessageExtractor();
            byte[] buffer = new byte[1024];

            int retries = 3;
            Message msg = null;

            while (retries-- > 0) {

                // setup serial port
                ep.sp.DiscardInBuffer();
                ep.sp.DiscardOutBuffer();
                ep.sp.ReadTimeout = 20;

                // send data
                ep.sp.Write(request.Binary, 0, request.BinarySize);
                Debug.WriteLine("sent " + request.BinarySize.ToString());

                // and wait for response
                DateTime start = DateTime.Now;
                do {
                    int read = -1;
                    try {
                        read = ep.sp.Read(buffer, 0, buffer.Length);
                    }
                    catch (TimeoutException tex) {
                        Debug.WriteLine("TO");
                        continue; // ignore timeouts
                    }

                    Debug.WriteLine("R " + read.ToString());
                    me.AddData(buffer, read);
                    if (me.TryExtract(ref msg, request.Address, request.Type))
                        break; // ok, got message!

                } while ((DateTime.Now - start).TotalMilliseconds <= timeout && timeout != -1);

                // if message was correctly received then stop communication
                if (msg != null)
                    break;
                Debug.WriteLine("RETRY");
            }
            if (msg == null && throw_timeout_exception)
                throw new TimeoutException(string.Format("No response from bootloader device 0x{0:X2} on {1}", ep.address, ep.sp.PortName));

            return msg;
        }


        private static void ShowDevices(Endpoint[] endpoints)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\nListing {0} discovered device(s): ", endpoints.Length);
            Console.ForegroundColor = ConsoleColor.Gray;

            foreach (Endpoint ep in endpoints) {
                Console.WriteLine("   Device 0x{0:X2} on {1}", ep.address, ep.sp.PortName);
            }
        }

        private static void AcquireDevicesOnSerialPort(List<Endpoint> endpoints, SerialPort sp)
        {
            Console.WriteLine("Sending PING to serial port {0}... ", sp.PortName);
            Console.CursorVisible = false;

            bool intro = true;
            int cx = 0, cy = 0;

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

                Message ping_message = new Message((byte)i, MessageType.Ping);
                Message msg = SendAndWaitForResponse(new Endpoint(sp, i), ping_message, 200, false);

                if (msg != null) {
                    Console.WriteLine(" Found!");
                    intro = true;
                    endpoints.Add(new Endpoint(sp, i));
                }
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
            Console.Write("\nSending C&C Advertisement to {0} serial ports: ", ports.Count);
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
                    Console.Write("Opening {0}... ", sp.PortName);
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


    public class ConsoleProgressBar
    {
        private int cx, cy;
        private int width;
        private string content;
        private double min, max, progress;

        public double Progress
        {
            get { return this.progress; }
            set {
                this.progress = value;
                UpdateContent();
                Show();
            }
        }
          

        public ConsoleProgressBar(double min, double max,double start_value = 0, int width = 30)
        { 
            this.progress = start_value;
            this.cx = Console.CursorLeft;
            this.cy = Console.CursorTop;
            this.width = width;
            this.min = min;
            this.max = max;

            this.UpdateContent();

            Show();
        }

        private void UpdateContent()
        {
            this.content = "[";

            double p = (progress - min) / (max - min);
            int pw = (int)Math.Round((double)width * p);

            for (int i = 0; i < pw; i++) this.content += '#';
            for (int i = 0; i < width - pw; i++) this.content += '.';

            this.content += "] ";
            this.content += (p * 100.0).ToString("N2") + "%";
        }

        void Show()
        {
            Console.SetCursorPosition(cx, cy);
            Console.Write(this.content);
        }
    }
}
