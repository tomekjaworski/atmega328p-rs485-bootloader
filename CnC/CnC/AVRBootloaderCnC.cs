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
        List<SerialPort> available_ports;
        List<string> excluded_port_names;

        List<Device> discovered_devices;

        public Device[] Devices => this.discovered_devices.ToArray();

        Random random;


        public AVRBootloaderCnC()
        {
            this.available_ports = new List<SerialPort>();
            this.excluded_port_names = new List<string>();
            this.discovered_devices = new List<Device>();
            this.random =  new Random();
        }

        internal void AcquireBootloaderDevices(byte max_addr)
        {
            foreach (SerialPort sp in available_ports)
                this.discovered_devices.AddRange(AcquireDevicesOnSerialPort(sp, max_addr));

            this.discovered_devices.Sort((x, y) => x.address - y.address);
        }


        private async  Task SerialPortOpenerThread(CancellationToken ct)
        {
            List<string> port_names_to_open = new List<string>();

            while (!ct.IsCancellationRequested) {
                lock (this.available_ports)
                    foreach (string port_name in SerialPort.GetPortNames()) {
                        SerialPort sp = available_ports.Find(x => x.PortName == port_name);
                        if (sp != null)
                            continue; // ok, this port was previoulsy opened

                        port_names_to_open.Add(port_name);
                    }

                while (port_names_to_open.Count > 0) {
                    String port_name = port_names_to_open[0];
                    try {
                        SerialPort sp = new SerialPort(port_name, 19200, Parity.Even, 8, StopBits.One);

                        sp.ReadTimeout = 200;
                        sp.Open();
                        port_names_to_open.Remove(port_name);
                        lock (this.available_ports)
                            this.available_ports.Add(sp);
                    }
                    catch (Exception ex) {
                        // Console.WriteLine("Failed");
                    }

                }

                await Task.Delay(100);
            }
        }

        public bool Reset(Device dev)
        {
            Console.Write("Rebooting {0:X2}... ", dev.address);
            Message reboot_message = new Message((byte)dev.address, MessageType.Reboot);
            Message msg = SendAndWaitForResponse(dev, reboot_message, 200, false);

            if (msg != null)
                Console.WriteLine("Ok.");
            else
                Console.WriteLine("Failed.");

            return msg != null;
        }

        public bool Ping(Device dev, int timeout)
        {
            int x = this.random.Next();
            byte[] payload = BitConverter.GetBytes(x);

            Message ping_message = new Message((byte)dev.address, MessageType.Ping, payload);
            Message msg = SendAndWaitForResponse(dev, ping_message, timeout, false);

            // check if there was a response
            if (msg == null)
                return false;
            
            // check if the received payload size is the same as sent
            if (msg.Payload.Length != 4)
                return false;

            // compare the contents
            for (int i = 0; i < 4; i++)
                if (payload[i] != msg.Payload[i])
                    return false;
                    
            // well, ok then
            return true;
        }


        public void SendAdvertisementToEveryDetectedPort()
        {
            Console.WriteLine("*** TURN ON all devices and press any key to processed...\n");
            char[] anim = { '/', '-', '\\', '|' };
            int anim_counter = 0;
            int cx = 0;

            Console.CursorVisible = false;
            cx = Console.CursorLeft;

            CancellationTokenSource cts = new CancellationTokenSource();
            Task t = SerialPortOpenerThread(cts.Token);

            while (!Console.KeyAvailable) {

                Thread.Sleep(100);

                Console.CursorLeft = cx;
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write("Sending C&C Advertisement to {0} serial ports: ", this.available_ports.Count);
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.Write(anim[anim_counter++ % 4]);

                // send advertisement to ports on list
                List<SerialPort> lost_ports = new List<SerialPort>();
                byte[] req = new byte[] { (byte)'A' };

                lock (this.available_ports)
                    foreach (SerialPort out_port in available_ports)
                        try {
                            //if (out_port.IsOpen)
                                out_port.Write(req, 0, 1);
                            //else
                                //out_port.Dispose();
                        }
                        catch (Exception ex) {
                            lost_ports.Add(out_port);
                        }

                // remove lost ports
                lock (this.available_ports)
                    foreach (SerialPort lp in lost_ports)
                        this.available_ports.Remove(lp);

            }

            cts.Cancel();
            Task.WaitAll(t);
        }
        
        public Device[] AcquireDevicesOnSerialPort(SerialPort sp, byte max_addr)
        {
            Console.WriteLine("\nSending PING to serial port {0}... ", sp.PortName);
            Console.CursorVisible = false;

            bool intro = true;
            int cx = 0, cy = 0;

            int timeout = 100;
            int counter = 0;

            sp.ReadTimeout = 20;
            byte[] buffer = new byte[1024];
            MessageExtractor me = new MessageExtractor();
            List<Device> endpoints = new List<Device>();

            // scan through 0x00 - 0xEF. Range 0xF0 - 0xFF is reserved
            max_addr = Math.Min(max_addr, (byte)0xF0);

            for (int i = 0x00; i < max_addr; i++) {
              //i = 0x12;
                if (intro) {
                    Console.Write(" Looking for device ");
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
                Message msg = SendAndWaitForResponse(new Device(sp, i), ping_message, timeout, false, 0);

                if (msg != null) {
                    Console.WriteLine(" Found!");
                    counter++;
                    intro = true;
                    endpoints.Add(new Device(sp, i));
                }

                //break;
            }

            Console.SetCursorPosition(0, cy);
            Console.WriteLine(" Done. Found {0} devices on serial port {1}.", counter, sp.PortName);
            Console.CursorVisible = true;
            return endpoints.ToArray();
        }


        public void ShowDevices()
        {
            Console.WriteLine("\nListing {0} discovered device(s): ", discovered_devices.Count);

            // group them against serial port
            Dictionary<string, List<Device>> devs = new Dictionary<string, List<Device>>();
            foreach (Device dev in this.discovered_devices) {
                if (!devs.ContainsKey(dev.sp.PortName))
                    devs[dev.sp.PortName] = new List<Device>();

                devs[dev.sp.PortName].Add(dev);
            }

            // sort them
            foreach(List<Device> sp_devs in devs.Values) 
                sp_devs.Sort((x, y) => x.address - y.address);

            // and show them
            foreach (string key in devs.Keys) {
                List<Device> sp_devs = devs[key];
                Console.Write(" Port {0:010}: ", key.PadRight(5));

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(string.Join(" ", sp_devs.Select(x => x.address.ToString("X2"))));
                Console.ForegroundColor = ConsoleColor.Gray;
            }

            // and show them again, but together for better view in case of holes in addresses

            discovered_devices.Sort((x, y) => x.address - y.address);
            Console.Write(" Concatenated list: ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine( string.Join(" ", discovered_devices.Select(x => x.address.ToString("X2"))));
            Console.ForegroundColor = ConsoleColor.Gray;

        }

        public void ReadVersion(Device dev, ref string ver)
        {
            Console.CursorVisible = false;

            Console.Write("Reading bootloader fw version:   ");

            Message msg_readfwver = new Message((byte)dev.address, MessageType.ReadBootloaderVersion);
            Message response = SendAndWaitForResponse(dev, msg_readfwver, 2000);

            ver = Encoding.ASCII.GetString(response.Payload, 0, response.Payload.Length - 1);

            Console.CursorVisible = true;
            Console.WriteLine(ver);
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
            Console.WriteLine("Done ({0:X2} {1:X2} {2:X2}).", signature[0], signature[2], signature[4]);
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
                if (response.Type != MessageType.WriteEepromPage)
                    throw new CnCException("response.Type");
            }

            Console.CursorVisible = true;
            Console.WriteLine("Done.");
        }

        private void PurgeSerialPorts()
        {
            Thread.Sleep(100);
            foreach (SerialPort sp in this.available_ports) {
                sp.DiscardInBuffer();
                sp.DiscardOutBuffer();
            }
        }

        private Message SendAndWaitForResponse(Device ep, Message request, int timeout, bool throw_timeout_exception = true, int retries = 3)
        {
            Debug.Assert(ep.address == request.Address);

            MessageExtractor me = new MessageExtractor();
            byte[] buffer = new byte[1024];
            byte[] empty = new byte[128+4+5];

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
                    catch (Exception ex) {
                        Debug.WriteLine("EX");
                        break; // shit happens
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
