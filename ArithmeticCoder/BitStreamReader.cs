using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArithmeticCoder
{
    internal class BitStreamReader
    {
        public BitStreamReader(BinaryReader stream)
        {
            _rack = 0x00;
            _mask = 0x80;
            _stream = stream;
        }

        public bool ReadBit()
        {
            bool result;
            int value;

            if (_mask == 0x00)
            {
                try
                {
                    value = _stream.ReadByte();
                    _rack = (byte)value;
                }
                catch(System.IO.EndOfStreamException)
                {
                    _rack = 0x00;
                }
            }

            result = ((_rack & _mask) != 0 ? true : false);
            _mask >>= 1;

            if (_mask == 0x00)
            {
                _mask = 0x80;
            }

            return result;
        }

        private byte _rack;
        private byte _mask;
        private BinaryReader _stream;
    }
}
