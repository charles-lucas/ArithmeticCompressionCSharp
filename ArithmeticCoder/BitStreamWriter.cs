namespace ArithmeticCoder
{
    internal class WriterObject
    {
        public WriterObject(BinaryWriter writer)
        {
            _writer = writer;
        }

        public WriterObject(List<byte> output)
        {
            _outputList = output;
        }

        public void Write(byte bite)
        {
            if (_writer != null)
            {
                _writer.Write(bite);
            }
            else if(_outputList != null)
            {
                _outputList.Add(bite);
            }
        }

        public void Flush()
        {
            if (_writer != null)
            {
                _writer.Flush();
            }
        }

        public Int64 Length
        {
            get
            {
                Int64 result = 0;
                if (_outputList != null)
                {
                    result = _outputList.Count;
                }
                else if (_writer != null)
                {
                    result = _writer.BaseStream.Length;
                }

                return result;
            }
        }

        private BinaryWriter? _writer = null;
        private List<byte>? _outputList = null;
    }

    internal class BitStreamWriter
    {
        public BitStreamWriter(BinaryWriter output)
        {
            _rack = 0x00;
            _mask = 0x80;
            _output = new WriterObject(output);
            _rollBackActions = new Stack<RollBackItem>();
        }

        public BitStreamWriter(List<byte> output)
        {
            _rack = 0x00;
            _mask = 0x80;
            _output = new WriterObject(output);
            _rollBackActions = new Stack<RollBackItem>();
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
                _output.Write(_rack);
                CompressionTracker.Instance.IncrementOutput();
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

        public Int32 WriteBit(bool bit, List<byte> output)
        {
            Int32 bitCount = 0;

            if (bit)
            {
                _rack |= _mask;
            }
            _mask >>= 1;

            if (_mask == 0x00)
            {
                output.Add(_rack);
                CompressionTracker.Instance.IncrementOutput();
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
                    _output.Write(_rack);
                    CompressionTracker.Instance.IncrementOutput();
                    _rack = 0x00;
                    _mask = 0x80;
                }
                mask >>= 1;
            }
        }

        public void WriteBits(UInt64 code, Int32 count, List<byte> output)
        {
            UInt64 mask = (UInt64)(1 << (count - 1));

            while (mask != 0)
            {
                if ((mask & code) != 0)
                {
                    _rack |= _mask;
                }
                _mask >>= 1;
                if (_mask == 0x00)
                {
                    output.Add(_rack);
                    CompressionTracker.Instance.IncrementOutput();
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
                _output.Write(_rack);
                CompressionTracker.Instance.IncrementOutput();
            }
            _output.Flush();
        }

        public void Flush(List<byte> output)
        {
            if (_mask != 0x80)
            {
                output.Add(_rack);
                CompressionTracker.Instance.IncrementOutput();
            }
            _mask = 0x80;
        }

        public void Flush(byte bite)
        {
            _output.Write(bite);
            _output.Flush();
        }

        public void SetRollBackCheckPoint()
        {
            //_keepRollBack = true;
            _rollBackActions.Push(new RollBackBitWriter(_rack, _mask));
        }

        public void RollBack()
        {
            RollBackItem? item;

            do
            {
                item = _rollBackActions.Pop();
                if (item != null)
                {
                    if (item.GetType() == typeof(RollBackBitWriter))
                    {
                        RollBackBitWriter update = (RollBackBitWriter)item;
                        _rack = update.Rack;
                        _mask = update.Mask;
                    }
                }
            } while (_rollBackActions.Count > 0);
            //_keepRollBack = false;
        }

        public Int64 Length
        {
            get
            {
                _output.Flush();
                return _output.Length;
            }
        }

        public void WriteByte(byte bite)
        {
            _output.Write(bite);
            CompressionTracker.Instance.IncrementOutput();
        }

        public byte Mask => _mask;
        public byte Rack => _rack;

        private byte _rack;
        private byte _mask;
        private WriterObject _output;
        //private bool _keepRollBack;
        private Stack<RollBackItem> _rollBackActions;
    }
}
