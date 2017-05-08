using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CnC
{
    public enum MessageType : byte
    {
        Activate = (byte)'A',
        //Deactivate = (byte)'B',

        Ping = (byte)'?',
        Reboot = (byte)'R',

        ReadFlashPage = (byte)'X',
        WriteFlashPage = (byte)'W',
        ReadEepromPage = (byte)'E',
        WriteEepromPage = (byte)'F',

        ReadSignature = (byte)'S',

        ReadBootloaderVersion = (byte)'V',
    }
}
