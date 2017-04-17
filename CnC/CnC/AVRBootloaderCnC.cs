using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CnC
{

    public class AVRBootloaderCnC
    {
        SerialPort[] available_ports;
        SerialPort[] opened_ports;

        public SerialPort[] OpenedSerialPorts => this.opened_ports.Clone() as SerialPort[];

        public AVRBootloaderCnC(SerialPort[] ports = null)
        {
            if (ports == null)
                this.available_ports = this.GetAllSerialPorts();
            else
                this.available_ports = ports.Clone() as SerialPort[];
        }

        private SerialPort[] GetAllSerialPorts()
        {
            SerialPort[] ports = SerialPort.GetPortNames().Select(x => new SerialPort(x, 19200, Parity.Even, 8, StopBits.One)).ToArray();
            return ports;
        }

        public void ShowAvailableSerialPorts()
        {
            Console.WriteLine("Available serial ports: {0}",
                String.Join(", ", this.available_ports.Select(x => x.PortName).ToArray()));
        }


        public void OpenAllSerialPorts()
        {
            // open all ports
            List<SerialPort> list = new List<SerialPort>();
            foreach (SerialPort sp in this.available_ports) {
                try {
                    Console.Write("Opening {0}... ", sp.PortName);
                    sp.ReadTimeout = 200;
                    sp.Open();
                    Console.WriteLine("Ok");

                    list.Add(sp); // use only opened ports

                }
                catch (Exception ex) {
                    Console.WriteLine("Error: {0}", ex.Message);
                }
            }

            this.opened_ports = list.ToArray();
        }


        public void SendAdvertisement()
        {
            byte[] req;
            Console.WriteLine("*** TURN ON all devices and press any key to processed...");
            char[] anim = { '/', '-', '\\', '|' };
            int anim_counter = 0;

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("\nSending C&C Advertisement to {0} serial ports: ", this.opened_ports.Length);
            Console.ForegroundColor = ConsoleColor.Gray;

            Console.CursorVisible = false;
            int cx = Console.CursorLeft;

            while (!Console.KeyAvailable) {
                req = new byte[] { (byte)'A' };
                foreach (SerialPort sp in this.opened_ports)
                    sp.Write(req, 0, 1);

                Thread.Sleep(100);
                Console.CursorLeft = cx;
                Console.Write(anim[anim_counter++ % 4]);
            }

            Console.WriteLine(" Done.");
            Console.CursorVisible = true;

            this.PurgeSerialPorts();
        }


        public Device[] AcquireDevicesOnSerialPort(SerialPort sp)
        {
            Console.WriteLine("Sending PING to serial port {0}... ", sp.PortName);
            Console.CursorVisible = false;

            bool intro = true;
            int cx = 0, cy = 0;

            int timeout = 200;

            sp.ReadTimeout = 20;
            byte[] buffer = new byte[1024];
            MessageExtractor me = new MessageExtractor();
            List<Device> endpoints = new List<Device>();

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
                Message msg = SendAndWaitForResponse(new Device(sp, i), ping_message, 50, false, 0);

                if (msg != null) {
                    Console.WriteLine(" Found!");
                    intro = true;
                    endpoints.Add(new Device(sp, i));
                }
            }

            Console.CursorVisible = true;
            return endpoints.ToArray();
        }


        public void ShowDevices(Device[] endpoints)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\nListing {0} discovered device(s): ", endpoints.Length);
            Console.ForegroundColor = ConsoleColor.Gray;

            foreach (Device ep in endpoints) {
                Console.WriteLine("   Device 0x{0:X2} on {1}", ep.address, ep.sp.PortName);
            }
        }



        public void ReadSignature(Device endpoint, out byte[] signature)
        {
            Console.CursorVisible = false;
            signature = null;

            Console.Write("Reading AVR CPU signature (32b): ");

            Message msg_readsig = new Message((byte)endpoint.address, MessageType.ReadSignature);
            Message response = SendAndWaitForResponse(endpoint, msg_readsig, 2000);

            signature = response.Payload;

            Console.CursorVisible = true;
            Console.WriteLine("Done.");
        }

        public void ReadFLASH(Device endpoint, MemoryMap dest)
        {
            Console.CursorVisible = false;

            Console.Write("Reading FLASH memory ({0}kB):    ", dest.Size / 1024);
            ConsoleProgressBar cpb = new ConsoleProgressBar(0, dest.Size);

            for (uint addr = 0; addr < dest.Size; addr += 128, cpb.Progress = addr) {
                Message msg_readpage = new Message((byte)endpoint.address, MessageType.ReadFlashPage, new byte[] { (byte)(addr & 0xFF), (byte)(addr >> 8), });

                Message response = SendAndWaitForResponse(endpoint, msg_readpage, 2000);
                dest.Write(addr, response.Payload, 0, 128);
            }

            Console.CursorVisible = true;
            Console.WriteLine("Done.");
        }

        public void WriteFLASH(Device endpoint, MemoryMap source)
        {
            Console.CursorVisible = false;

            Console.Write("Writing FLASH memory ({0}kB):    ", source.Size / 1024);
            ConsoleProgressBar cpb = new ConsoleProgressBar(0, source.Size);

            for (uint addr = 0; addr < source.Size; addr += 128, cpb.Progress = addr) {

                byte[] payload = new byte[2 + 128];
                payload[0] = (byte)(addr & 0xFF);
                payload[1] = (byte)((addr >> 8) & 0xFF);
                source.Read(addr, payload, 2, 128);

                Message msg_write = new Message((byte)endpoint.address, MessageType.WriteFlashPage, payload);

                Message response = SendAndWaitForResponse(endpoint, msg_write, 2000);
            }

            Console.CursorVisible = true;
            Console.WriteLine("Done.");
        }

        public bool VerifyFLASH(Device endpoint, MemoryMap expected)
        {
            Console.CursorVisible = false;

            Console.Write("Verifying FLASH memory ({0}kB):  ", expected.Size / 1024);
            ConsoleProgressBar cpb = new ConsoleProgressBar(0, expected.Size);
            MemoryMap mmread = new MemoryMap(expected.Size);

            for (uint addr = 0; addr < expected.Size; addr += 128, cpb.Progress = addr) {
                Message msg_readpage = new Message((byte)endpoint.address, MessageType.ReadFlashPage, new byte[] { (byte)(addr & 0xFF), (byte)(addr >> 8), });

                Message response = SendAndWaitForResponse(endpoint, msg_readpage, 2000);
                mmread.Write(addr, response.Payload, 0, 128);
            }

            UInt32 difference_address = 0;
            bool result = expected.BinaryCompare(mmread, ref difference_address);

            Console.CursorVisible = true;
            if (result)
                Console.WriteLine("Correct.");
            else {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Failed.");
                Console.ForegroundColor = ConsoleColor.Gray;

                byte expected_byte = expected.ReadByte(difference_address);
                byte existing_byte = mmread.ReadByte(difference_address);

                throw new MemoryVerificationException("FLASH", difference_address, expected_byte, existing_byte);
            }
            return result;
        }

        public void ReadEEPROM(Device endpoint, MemoryMap dest)
        {
            Console.CursorVisible = false;

            Console.Write("Reading EEPROM memory ({0}kB):   ", dest.Size / 1024);
            ConsoleProgressBar cpb = new ConsoleProgressBar(0, dest.Size);

            for (uint addr = 0; addr < dest.Size; addr += 128, cpb.Progress = addr) {
                Message msg_readpage = new Message((byte)endpoint.address, MessageType.ReadEepromPage, new byte[] { (byte)(addr & 0xFF), (byte)(addr >> 8), });

                Message response = SendAndWaitForResponse(endpoint, msg_readpage, 2000);
                dest.Write(addr, response.Payload, 0, 128);
            }

            Console.CursorVisible = true;
            Console.WriteLine("Done.");
        }

        public bool VerifyEEPROM(Device endpoint, MemoryMap expected)
        {
            Console.CursorVisible = false;

            Console.Write("Verifying EEPROM memory ({0}kB): ", expected.Size / 1024);
            ConsoleProgressBar cpb = new ConsoleProgressBar(0, expected.Size);
            MemoryMap mmread = new MemoryMap(expected.Size);

            for (uint addr = 0; addr < expected.Size; addr += 128, cpb.Progress = addr) {
                Message msg_readpage = new Message((byte)endpoint.address, MessageType.ReadEepromPage, new byte[] { (byte)(addr & 0xFF), (byte)(addr >> 8), });

                Message response = SendAndWaitForResponse(endpoint, msg_readpage, 2000);
                mmread.Write(addr, response.Payload, 0, 128);
            }

            UInt32 difference_address = 0;
            bool result = expected.BinaryCompare(mmread, ref difference_address);

            Console.CursorVisible = true;
            if (result)
                Console.WriteLine("Correct.");
            else {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Failed.");
                Console.ForegroundColor = ConsoleColor.Gray;

                byte expected_byte = expected.ReadByte(difference_address);
                byte existing_byte = mmread.ReadByte(difference_address);

                throw new MemoryVerificationException("FLASH", difference_address, expected_byte, existing_byte);
            }
            return result;
        }

        public void WriteEEPROM(Device endpoint, MemoryMap source)
        {
            Console.CursorVisible = false;

            Console.Write("Writing EEPROM memory ({0}kB):   ", source.Size / 1024);
            ConsoleProgressBar cpb = new ConsoleProgressBar(0, source.Size);

            for (uint addr = 0; addr < source.Size; addr += 128, cpb.Progress = addr) {

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


        private void PurgeSerialPorts()
        {
            Thread.Sleep(100);
            foreach (SerialPort sp in this.opened_ports) {
                sp.DiscardInBuffer();
                sp.DiscardOutBuffer();
            }
        }

        private Message SendAndWaitForResponse(Device ep, Message request, int timeout, bool throw_timeout_exception = true, int retries = 3)
        {
            Debug.Assert(ep.address == request.Address);

            MessageExtractor me = new MessageExtractor();
            byte[] buffer = new byte[1024];

            Message msg = null;

            while (retries-- >= 0) {

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

    }
}
