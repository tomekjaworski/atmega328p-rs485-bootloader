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

    class ____________Program
    {
        static Random random;
        static void ___________Main(string[] args)
        {
            Console.WriteLine("SmartTable bootloader C&C software by Tomasz Jaworski");
            random = new Random();

            //MemoryMap mm = new MemoryMap(0x10000);
           // IntelHEX16Storage st = new IntelHEX16Storage(mm);
          //  st.Load(@"d:\praca\projekty\SmartTable\atmega328p-rs485-bootloader\Debug\atmega328p_bootloader.hex");
           // mm.Dump("test.txt");


            //if (SerialPort.GetPortNames().Length == 0) {
            //    Console.WriteLine("No serial ports available, quitting...");
            //    return;
            //}

            AVRBootloaderCnC cnc = new AVRBootloaderCnC();

            cnc.SendAdvertisementToEveryDetectedPort();

            cnc.AcquireBootloaderDevices(0x20);

            // show found devices
            cnc.ShowDevices();

            Console.WriteLine("Reading bootloader version and signature");
            foreach (Device dev in cnc.Devices)
            {
                Console.WriteLine("Reading device {0:X2}... ", dev.address);

                // read bootloader version and timestamp
                string ver = "";
                cnc.ReadVersion(dev, ref ver);

                // read CPU signature
                byte[] bsig = null;
                cnc.ReadSignature(dev, out bsig);
            }


            #region DEMO 1 - Download EEPROM from all found devices
            
            Console.WriteLine("Reading EEPROM and Flash memories...");
            
            foreach (Device dev in cnc.Devices)
            {
                Console.WriteLine("Reading device {0:X2}... ", dev.address);

                // read eeprom
                MemoryMap mm = new MemoryMap(1024);
                cnc.ReadEEPROM(dev, mm);

                string fname = string.Format("eeprom_{0:X2}", dev.address);
                mm.Dump(fname + ".txt", DumpMode.Text);
                mm.Dump(fname + ".bin", DumpMode.Binary);

                // read flash
                mm = new MemoryMap(32 * 1024);
                cnc.ReadFLASH(dev, mm);

                fname = string.Format("flash_{0:X2}", dev.address);
                mm.Dump(fname + ".txt", DumpMode.Text);
                mm.Dump(fname + ".bin", DumpMode.Binary);
            }
            
            #endregion


            #region DEMO 2 - Increment a counter in eeprom (@ 0x0A) and in flash (@ 0x0100)
            /*
            Console.WriteLine("Reading EEPROM and Flash memories...");

            foreach (Device dev in cnc.Devices) {
                Console.WriteLine("Reading device {0:X2}... ", dev.address);

                // read eeprom
                MemoryMap mm = new MemoryMap(1024);
                cnc.ReadEEPROM(dev, mm);

                UInt16 a = mm.ReadUInt16(10);
                if (a == 0xffff)
                    a = 0;
                else
                    a++;
                mm.Write(10, a);
                cnc.WriteEEPROM(dev, mm);
                Console.WriteLine("A=" + a.ToString());
            }


            foreach (Device dev in cnc.Devices) {
                Console.WriteLine("Reading device {0:X2}... ", dev.address);

                // read eeprom
                MemoryMap mm = new MemoryMap(32*1024);
                cnc.ReadFLASH(dev, mm);

                UInt16 a = mm.ReadUInt16(0x100);
                if (a == 0xffff)
                    a = 0;
                else
                    a++;
                mm.Write(0x100, a);
                cnc.WriteFLASH(dev, mm);
                Console.WriteLine("A=" + a.ToString());
            }
            */


            #endregion

            #region  DEMO 3 - fill eeprom and flash with random data
            /*
            foreach (Device dev in cnc.Devices) {
                // read eeprom
                MemoryMap mm = new MemoryMap(1024);
                for (uint i = 0; i < 1024; i++)
                    mm.Write(i, (byte)random.Next());

                cnc.WriteEEPROM(dev, mm);
                string fname = string.Format("eeprom_random_written_{0:X2}", dev.address);
                mm.Dump(fname + ".txt", DumpMode.Text);
                mm.Dump(fname + ".bin", DumpMode.Binary);

                mm.Zero();

                cnc.ReadEEPROM(dev, mm);
                fname = string.Format("eeprom_random_read_{0:X2}", dev.address);
                mm.Dump(fname + ".txt", DumpMode.Text);
                mm.Dump(fname + ".bin", DumpMode.Binary);
            }

            foreach (Device dev in cnc.Devices) {
                // read eeprom
                MemoryMap mm = new MemoryMap(32*1024);
                for (uint i = 0; i < mm.Size; i++)
                    mm.Write(i, (byte)random.Next());

                cnc.WriteFLASH(dev, mm);
                string fname = string.Format("flash_random_written_{0:X2}", dev.address);
                mm.Dump(fname + ".txt", DumpMode.Text);
                mm.Dump(fname + ".bin", DumpMode.Binary);

                mm.Zero();

                cnc.ReadFLASH(dev, mm);
                fname = string.Format("flash_random_read_{0:X2}", dev.address);
                mm.Dump(fname + ".txt", DumpMode.Text);
                mm.Dump(fname + ".bin", DumpMode.Binary);
            }
            */
            #endregion

            /*
            List<SerialPort> ports = ListAndOpenSerialPorts();

            // send advertisement to all devices, co they can stay in bootloader mode
            SendAdvertisement(ports);

            // purge buffers
            PurgeSerialPorts(ports);

            List<Device> endpoints = new List<Device>();
            */
            /*
            // get list of devices for each serial port
            foreach (SerialPort sp in ports)
                AcquireDevicesOnSerialPort(endpoints, sp);

            // show discovered devices
            ShowDevices(endpoints.ToArray());
            */

            //endpoints.Add(new Device(ports[0], 0x51));




            /*            MemoryMap eeprom = new MemoryMap(1024);
                        ReadEEPROM(endpoints[0], eeprom);
                        eeprom.Dump("eeprom1a.txt");
                        eeprom.Write(123, eeprom.ReadInt16(123) + 123);
                        WriteEEPROM(endpoints[0], eeprom);
                        VerifyEEPROM(endpoints[0], eeprom);
                        ReadEEPROM(endpoints[0], eeprom);
                        eeprom.Dump("eeprom2a.txt");

                        ReadEEPROM(endpoints[0], eeprom);
                        eeprom.Dump("eeprom1b.txt");
                        eeprom.Write(123, eeprom.ReadInt16(123) + 123);
                        WriteEEPROM(endpoints[0], eeprom);
                        VerifyEEPROM(endpoints[0], eeprom);
                        ReadEEPROM(endpoints[0], eeprom);
                        eeprom.Dump("eeprom2b.txt");

                        ReadEEPROM(endpoints[0], eeprom);
                        eeprom.Dump("eeprom1c.txt");
                        eeprom.Write(123, eeprom.ReadInt16(123) + 123);
                        WriteEEPROM(endpoints[0], eeprom);
                        eeprom.Write(400, 99);
                        VerifyEEPROM(endpoints[0], eeprom);
                        ReadEEPROM(endpoints[0], eeprom);
                        eeprom.Dump("eeprom2c.txt");

                        ReadEEPROM(endpoints[0], eeprom);
                        eeprom.Dump("eeprom1d.txt");
                        eeprom.Write(123, eeprom.ReadInt16(123) + 123);
                        WriteEEPROM(endpoints[0], eeprom);
                        VerifyEEPROM(endpoints[0], eeprom);
                        ReadEEPROM(endpoints[0], eeprom);
                        eeprom.Dump("eeprom2d.txt");
                        */

            //byte[] sig;
            //ReadSignature(endpoints[0], out sig);

        }

    }

}
