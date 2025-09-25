using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArithmeticCoder
{
    internal static class Constants
    {
        public const UInt32 MAXIMUM_SCALE = 16383;
        public const Int16 ESCAPE = 256;
        public const Int16 DONE = -1;
        public const Int16 FLUSH = -2;
        public const Int16 EOF = -1;
        public const Int32 CompressionLimit = 90;
        public const Int32 EndOfPacketSpace = 8; // bytes
        public const Int16 EndOfPacket = -3;
    }
}
