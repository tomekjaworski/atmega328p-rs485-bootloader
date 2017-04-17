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

            AVRBootloaderCnC cnc = new AVRBootloaderCnC();

            // open all available serial ports
            cnc.OpenAllSerialPorts();

            // start sending advertisements and wait unit user's reaction
            cnc.SendAdvertisement();

            List<Device> devices = new List<Device>();
            foreach (SerialPort sp in cnc.OpenedSerialPorts)
                devices.AddRange(cnc.AcquireDevicesOnSerialPort(sp));

            cnc.ShowDevices(devices.ToArray());

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
            /*
            MemoryMap eeprom = new MemoryMap(1024);
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

            byte[] sig;
            //ReadSignature(endpoints[0], out sig);

        }

    }

}
