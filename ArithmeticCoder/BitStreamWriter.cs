namespace ArithmeticCoder
{
    internal class BitStreamWriter
    {
        public BitStreamWriter(BinaryWriter stream)
        {
            _rack = 0x00;
            _mask = 0x80;
            _stream = stream;
        }

        public Int32 WriteBit(bool bit)
        {
            Int32 bitCount = 0;

            if (bit)
            {
                _rack |= _mask;
            }
            _mask >>= 1;

            if (_mask == 0x00)
            {
                _stream.Write(_rack);
                _mask = 0x80;
                _rack = 0x00;
                bitCount += 8;
            }

            if (_mask == 0x00)
            {
                _mask = 0x80;
            }

            return bitCount;
        }

        public void WriteBits(UInt64 code, Int32 count)
        {
            UInt64 mask = (UInt64)(1 << (count - 1));

            while(mask != 0)
            {
                if((mask & code) != 0)
                {
                    _rack |= _mask;
                }
                _mask >>= 1;
                if(_mask == 0x00)
                {
                    _stream.Write(_rack);
                    _rack = 0x00;
                    _mask = 0x80;
                }
                mask >>= 1;
            }
        }

        public void Flush()
        {
            if(_mask != 0x80)
            {
                _stream.Write(_rack);
            }
            _stream.Flush();
        }

        public void Flush(byte bite)
        {
            _stream.Write(bite);
            _stream.Flush();
        }

        public Int64 Length
        {
            get
            {
                _stream.Flush();
                return _stream.BaseStream.Length;
            }
        }

        public void WriteByte(byte bite)
        {
            _stream.Write(bite);
        }

        public byte Mask => _mask;
        public byte Rack => _rack;

        private byte _rack;
        private byte _mask;
        private BinaryWriter _stream;
    }
}
