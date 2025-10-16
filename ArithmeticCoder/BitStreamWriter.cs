namespace ArithmeticCoder
{
    /// <summary>
    /// Class <c>WriteObject</c> wraps the actual source to write from to allow for interchangeablity.
    /// </summary>
    internal class WriterObject
    {
        /// <summary>
        /// Constructor for <c>WriterObject</c> that uses the provided <c>BinaryWrite</c> to write data.
        /// </summary>
        /// <param name="writer"><c>BinaryWriter</c> used by the object to write data.</param>
        public WriterObject(BinaryWriter writer)
        {
            _writer = writer;
        }

        /// <summary>
        /// Constructor for <c>WriterObject</c> that uses the provided <c>List<byte></c> to write data.
        /// </summary>
        /// <param name="output"><c>List<byte></c> used by the object to write data.</param>
        public WriterObject(List<byte> output)
        {
            _outputList = output;
        }

        /// <summary>
        /// Method used to write a byte.
        /// </summary>
        /// <param name="bite">Byte to be writen.</param>
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

        /// <summary>
        /// Method to flush the <c>WriterObject</c>.
        /// </summary>
        public void Flush()
        {
            if (_writer != null)
            {
                _writer.Flush();
            }
        }

        /// <summary>
        /// Property to get the length of the output in bytes.
        /// </summary>
        /// <returns><c>Int64</c> value that represents the output size in bytes.</returns>
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

    /// <summary>
    /// Class to write bits to an output source.
    /// </summary>
    internal class BitStreamWriter
    {
        /// <summary>
        /// Constructor for <c>BitStreamWriter</c> which writes data to provided <c>BinaryWriter</c>.
        /// </summary>
        /// <param name="output"><c>BinaryWriter</c> which bits will be writen to.</param>
        public BitStreamWriter(BinaryWriter output)
        {
            _rack = 0x00;
            _mask = 0x80;
            _output = new WriterObject(output);
            _rollBackActions = new Stack<RollBackItem>();
        }

        /// <summary>
        /// Constructor for <c>BitStreamWriter</c> which writes data to provided <c>List<byte></c>.
        /// </summary>
        /// <param name="output"><c>List<byte></c> which bits will be writen to.</param>>
        public BitStreamWriter(List<byte> output)
        {
            _rack = 0x00;
            _mask = 0x80;
            _output = new WriterObject(output);
            _rollBackActions = new Stack<RollBackItem>();
        }

        /// <summary>
        /// Method to write a bit.
        /// </summary>
        /// <param name="bit">Bit to write to the output.</param>
        /// <returns><c>Int32</c> number of bits to the output source.</returns>
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

        /// <summary>
        /// Method to write a bit.
        /// </summary>
        /// <param name="bit">Bit to write to the output.</param>
        /// <param name="output">Override output source, to use instead of objects output source.</param>
        /// <returns><c>Int32</c> number of bits to the output source.</returns>
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

        /// <summary>
        /// Method to write out multiple bits.
        /// </summary>
        /// <param name="code"><c>UInt64</c> value that contains the bits to write out.</param>
        /// <param name="count">Number of bits to write out.</param>
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

        /// <summary>
        /// Method to write out multiple bits.
        /// </summary>
        /// <param name="code"><c>UInt64</c> value that contains the bits to write out.</param>
        /// <param name="count">Number of bits to write out.</param>
        /// <param name="output">Override output source, to use instead of objects output source.</param>
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

        /// <summary>
        /// Method to flush the <c>BitStreamWriter</c> output stream.
        /// </summary>
        public void Flush()
        {
            if(_mask != 0x80)
            {
                _output.Write(_rack);
                CompressionTracker.Instance.IncrementOutput();
            }
            _output.Flush();
        }

        /// <summary>
        /// Method to flush the <c>BitStreamWriter</c> output stream.
        /// </summary>
        /// <param name="output">Override output source, to use instead of objects output source.</param>
        public void Flush(List<byte> output)
        {
            if (_mask != 0x80)
            {
                output.Add(_rack);
                CompressionTracker.Instance.IncrementOutput();
            }
            _mask = 0x80;
        }

        /// <summary>
        /// Method to set a roll back check point.
        /// </summary>
        public void SetRollBackCheckPoint()
        {
            _rollBackActions.Push(new RollBackBitWriter(_rack, _mask));
        }

        /// <summary>
        /// Method to roll back changes to roll back checkpoint.
        /// </summary>
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
        }

        /// <summary>
        /// Property to get the length of the output stream.
        /// </summary>
        /// <returns><c>Int64</c> value that represents the length of the output in bytes.</returns>
        public Int64 Length
        {
            get
            {
                _output.Flush();
                return _output.Length;
            }
        }

        /// <summary>
        /// Method used to write byte to output stream.
        /// </summary>
        /// <param name="bite">Byte to be writn to ouput stream.</param>
        public void WriteByte(byte bite)
        {
            _output.Write(bite);
            CompressionTracker.Instance.IncrementOutput();
        }

        /// <summary>
        /// Property to get the Mask.
        /// </summary>
        /// <returns><c>byte</c> that is the current mask.</returns>
        public byte Mask => _mask;

        private byte _rack;
        private byte _mask;
        private WriterObject _output;
        private Stack<RollBackItem> _rollBackActions;
    }
}
