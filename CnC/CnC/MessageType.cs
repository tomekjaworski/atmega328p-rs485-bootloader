using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CnC
{
    public enum MessageType : byte
    {
        BL_COMMAND_ACTIVATE = (byte)'A',
        BL_COMMAND_DEACTIVATE = (byte)'B',
        BL_COMMAND_PING = (byte)'?',
        BL_COMMAND_REBOOT = (byte)'R',

        BL_COMMAND_READ_PAGE = (byte)'X',
        BL_COMMAND_WRITE_PAGE = (byte)'W'
    }
}
