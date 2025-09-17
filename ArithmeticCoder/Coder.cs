namespace ArithmeticCoder
{
    internal class Coder
    {
        public Coder(bool encode, BinaryReader input, BinaryWriter? output)
        {
            _input = new BitStreamReader(input);

            if(output)
            {
                _output = new BitStreamWriter(output);
            }
            else
            {
                _output = null;
            }

            if (encode)
            {
                InitializeEncode();
            }
            else
            {
                InitializeDecode();
            }
        }

        public void Encode(Symbol symbol)
        {
            Int64 range = (_high - _low) + 1;
            _high = (UInt16)(_low + ((range * symbol.HighCount) / symbol.Scale - 1));
            _low = (UInt16)(_low + (range * symbol.LowCount) / symbol.Scale);
            bool output;

            while (true)
            {
                if ((_high & 0x8000) == (_low & 0x8000))
                {
                    output = (_high & 0x8000) != 0;
                    _output?.WriteBit(output);
                    while (_underflowBits > 0)
                    {
                        output = (~_high & 0x8000) != 0;
                        _output?.WriteBit(output);
                        _underflowBits--;
                    }
                }
                else if (((_low & 0x4000) != 0x00) && !((_high & 0x4000) != 0x00))
                {
                    _underflowBits++;
                    _low &= 0x3fff;
                    _high |= 0x4000;
                }
                else
                {
                    break;
                }

                _low <<= 1;
                _high <<= 1;
                _high |= 1;
            }
        }

        public void RemoveSymbol(Symbol symbol)
        {
            Int64 range = (_high - _low) + 1;
            _high = (UInt16)(_low + ((range * symbol.HighCount) / symbol.Scale - 1));
            _low = (UInt16)(_low + (range * symbol.LowCount) / symbol.Scale);
            bool highBitsSet;

            while (true)
            {
                highBitsSet = ((_high & 0x8000) == (_low & 0x8000));
                if (!highBitsSet && ((_low & 0x4000) == 0x4000) && ((_high & 0x4000) == 0x00))
                {
                    _code ^= 0x4000;
                    _low &= 0x3fff;
                    _high |= 0x4000;
                }
                else if (!highBitsSet)
                {
                    break;
                }

                _low <<= 1;
                _high <<= 1;
                _high |= 1;
                _code <<= 1;
                _code += (UInt16)(_input.ReadBit() ? 1 : 0);
            }
        }

        public int GetCurrentCount(Symbol symbol)
        {
            Int64 range;
            Int16 count;

            range = (_high - _low) + 1;
            count = (Int16)( ( ( ( _code - _low ) + 1 ) * symbol.Scale - 1 ) / range );

            return count;
        }

        public void AddInput(Stream stream)
        {

        }

        public void Flush()
        {
            bool output = (_low & 0x4000) != 0;
            _output?.WriteBit(output);
            _underflowBits++;

            while (_underflowBits-- > 0)
            {
                output = (~_low & 0x4000) != 0;
                _output?.WriteBit(output);
            }
        }

        public void InitializeEncode()
        {
            _low = 0;
            _high = 0xffff;
            _underflowBits = 0;
        }

        public void InitializeDecode()
        {
            _code = 0;
            for (int i = 0; i < 16; i++)
            {
                _code <<= 1;
                _code += (UInt16)(_input.ReadBit() ? 1 : 0);
            }

            _low = 0;
            _high = 0xffff;
        }

        private UInt16 _low;
        private UInt16 _high;
        private UInt16 _code;
        private UInt64 _underflowBits;
        private BitStreamReader _input;
        private BitStreamWriter _output;
    }
}
