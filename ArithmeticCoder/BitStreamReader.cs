namespace ArithmeticCoder
{
    internal class BitStreamReader
    {
        public BitStreamReader(BinaryReader stream, bool compatabilityMode = true)
        {
            _rack = 0x00;
            _mask = 0x80;
            _stream = stream;
            _addedZeros = 0;
            _compatabilityMode = compatabilityMode;
        }

        public bool ReadBit()
        {
            bool result;
            int value;

            if (_mask == 0x80)
            {
                try
                {
                    value = _stream.ReadByte();
                    CompressionTracker.Instance.IncrementInput();
                    _rack = (byte)value;
                }
                catch(System.IO.EndOfStreamException)
                {
                    _rack = 0x00;
                    _addedZeros++;

                    if (_compatabilityMode)
                    {
                        throw new System.IO.EndOfStreamException();
                    }
                    else if (_addedZeros > 2)
                    {
                        throw new System.IO.EndOfStreamException();
                    }
                    CompressionTracker.Instance.IncrementInput();
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
        private bool _compatabilityMode;
        private Int32 _addedZeros;
    }
}
