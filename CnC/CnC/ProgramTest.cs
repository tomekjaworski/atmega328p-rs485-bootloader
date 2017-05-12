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
        static Random random;
        static void Main(string[] args)
        {
            Console.WriteLine("SmartTable bootloader C&C software by Tomasz Jaworski");
            random = new Random();

            MemoryMap fw = new MemoryMap(32*1024-2*1024);
            IntelHEX16Storage st = new IntelHEX16Storage(fw);
            st.Load(@"d:\SystemDocuments\SmartTableDriver\SmartTableFirmware\Debug\SmartTableFirmware.hex");

            int pos1 = fw.FindSequence(new byte[] { 0xaa, 0x11, 0x0d, 0x4d });
            int pos2 = fw.FindSequence(new byte[] { 0x75, 0x87, 0x60, 0x64 });
            Debug.Assert(pos2 == pos1 + 5);

            fw.Dump("test.txt");



            AVRBootloaderCnC cnc = new AVRBootloaderCnC();
            cnc.SendAdvertisementToEveryDetectedPort();
            cnc.AcquireBootloaderDevices(0x20);

            // show found devices
            cnc.ShowDevices();


            Console.WriteLine("Reading bootloader version and signature");
            foreach (Device dev in cnc.Devices)
            {
                // read bootloader version and timestamp
                string ver = "";
                cnc.ReadVersion(dev, ref ver);

                // read CPU signature
                byte[] bsig = null;
                cnc.ReadSignature(dev, out bsig);
            }

            Console.WriteLine("Writing firmare...");

            foreach (Device dev in cnc.Devices) {

                // preapre modified firmare
                fw.Write((uint)pos1 + 4, (byte)dev.address);

                cnc.WriteFLASH(dev, fw);
                cnc.VerifyFLASH(dev, fw);
            }


            foreach (Device dev in cnc.Devices)
                cnc.Reset(dev);


        }

    }

}
