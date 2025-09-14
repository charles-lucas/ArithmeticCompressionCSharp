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

        private byte _rack;
        private byte _mask;
        private BinaryWriter _stream;
    }
}
