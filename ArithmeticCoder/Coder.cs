namespace ArithmeticCoder
{
    internal class Coder
    {
        public Coder(bool encode, BinaryReader? input, BinaryWriter? output, bool compatabilityMode = true)
        {
            _rollBackActions = new Stack<RollBackItem>();
            _compatabilityMode = compatabilityMode;

            if (input != null)
            {
                _input = new BitStreamReader(input, compatabilityMode);
            }
            else
            {
                _input = null;
            }

            if (output != null)
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

        public Coder(bool encode, Queue<byte>? input, List<byte>? output, bool compatabilityMode = true)
        {
            _rollBackActions = new Stack<RollBackItem>();
            _compatabilityMode = compatabilityMode;

            if (input != null)
            {
                _input = new BitStreamReader(input, compatabilityMode);
            }
            else
            {
                _input = null;
            }

            if (output != null)
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

        //* This routine is called to encode a symbol.  The symbol is passed
        //* in the SYMBOL structure as a low count, a high count, and a range,
        //* instead of the more conventional probability ranges.  The encoding
        //* process takes two steps.  First, the values of high and low are
        //* updated to take into account the range restriction created by the
        //* new symbol.  Then, as many bits as possible are shifted out to
        //* the output stream.  Finally, high and low are stable again and
        //* the routine returns.
        public void Encode(Symbol symbol, List<byte> result)
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
                    _output?.WriteBit(output, result);
                    while (_underflowBits > 0)
                    {
                        output = (~_high & 0x8000) != 0;
                        _output?.WriteBit(output, result);
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

        //* Just figuring out what the present symbol is doesn't remove
        //* it from the input bit stream.  After the character has been
        //* decoded, this routine has to be called to remove it from the
        //* input stream.
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
                if(_input != null)
                {
                    _code += (UInt16)(_input.ReadBit() ? 1 : 0);
                }
            }
        }

        //* When decoding, this routine is called to figure out which symbol
        //* is presently waiting to be decoded.  This routine expects to get
        //* the current model scale in the s->scale parameter, and it returns
        //* a count that corresponds to the present floating point code;
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

        //* At the end of the encoding process, there are still significant
        //* bits left in the high and low registers.  We output two bits,
        //* plus as many underflow bits as are necessary.
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
            _underflowBits = 0;
            if (_compatabilityMode)
            {
                _output?.WriteBits(0, 16);
            }
            _output?.Flush();
        }

        public void Flush(List<byte> result, Int32 emptyBitsInLastByte)
        {
            bool output = (_low & 0x4000) != 0;
            byte tempMask = 0x01;
            _output?.WriteBit(output, result);
            _underflowBits++;

            while (_underflowBits-- > 0)
            {
                output = (~_low & 0x4000) != 0;
                _output?.WriteBit(output, result);
            }
            _underflowBits = 0;

            if (emptyBitsInLastByte != 0 && _output != null)
            {
                tempMask <<= (7 - emptyBitsInLastByte);
                while (_output.Mask < tempMask)
                {
                    _output.WriteBit(false, result);
                }
            }

            if (_compatabilityMode)
            {
                _output?.WriteBits(0, 16, result);
            }
            _output?.Flush(result);
        }

        public void Flush(Int32 padToSize, bool pad = false)
        {
            bool output = (_low & 0x4000) != 0;
            _output?.WriteBit(output);
            _underflowBits++;

            while (_underflowBits-- > 0)
            {
                output = (~_low & 0x4000) != 0;
                _output?.WriteBit(output);
            }
            _underflowBits = 0;
            if (_compatabilityMode)
            {
                _output?.WriteBits(0, 16);
            }
            _output?.Flush();

            while(pad && _output != null && _output.Length < padToSize)
            {
                _output?.WriteByte(0x00);
            }
        }

        //* This routine must be called to initialize the encoding process.
        //* The high register is initialized to all 1s, and it is assumed that
        //* it has an infinite string of 1s to be shifted into the lower bit
        //* positions when needed.
        public void InitializeEncode()
        {
            _low = 0;
            _high = 0xffff;
            _underflowBits = 0;
        }

        //* This routine is called to initialize the state of the arithmetic
        //* decoder.  This involves initializing the high and low registers
        //* to their conventional starting values, plus reading the first
        //* 16 bits from the input stream into the code value.
        public void InitializeDecode()
        {
            _code = 0;
            for (int i = 0; i < 16; i++)
            {
                _code <<= 1;
                if (_input != null)
                {
                    _code += (UInt16)(_input.ReadBit() ? 1 : 0);
                }
            }

            _low = 0;
            _high = 0xffff;
        }

        public void SetRollBackCheckPoint()
        {
            //_keepRollBack = true;
            _rollBackActions.Push(new RollBackCoder(_low, _high, _code, _underflowBits));
            _output?.SetRollBackCheckPoint();
        }

        public void RollBack()
        {
            RollBackItem? item = null;

            do
            {
                item = _rollBackActions.Pop();
                if (item != null)
                {
                    if (item.GetType() == typeof(RollBackCoder))
                    {
                        RollBackCoder update = (RollBackCoder)item;
                        _low = update.Low;
                        _high = update.High;
                        _code = update.Code;
                        _underflowBits = update.UnderflowBits;
                    }
                }
            } while (_rollBackActions.Count > 0);
            _output?.RollBack();
            //_keepRollBack = false;
        }

        public Int64 OutputLength
        {
            get
            {
                Int64 result = 0;
                if (_output != null)
                {
                    result = _output.Length;
                }
                return result;
            }
        }

        public UInt64 UnderflowBits => _underflowBits;

        public byte Mask
        {
            get
            {
                byte result = 0;
                if (_output != null)
                {
                    result = _output.Mask;
                }
                return result;
            }
        }

        private UInt16 _low;
        private UInt16 _high;
        private UInt16 _code;
        private UInt64 _underflowBits;
        private BitStreamReader? _input;
        private BitStreamWriter? _output;
        //private bool _keepRollBack;
        private Stack<RollBackItem> _rollBackActions;
        private bool _compatabilityMode;
    }
}
