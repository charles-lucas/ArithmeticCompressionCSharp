using System.IO;

namespace ArithmeticCoder
{
    internal class ReaderObject
    {
        public ReaderObject(BinaryReader reader )
        {
            _breader = reader;
        }

        public ReaderObject(Queue<byte> input)
        {
            _inputQue = input;
        }

        public byte ReadByte()
        {
            byte result = 0x00;

            if (_inputQue != null)
            {
                try
                {
                    result = _inputQue.Dequeue();
                }
                catch (InvalidOperationException)
                {
                    throw new System.IO.EndOfStreamException(); 
                }
            }
            else if(_breader != null)
            {
                result = _breader.ReadByte();
            }

            return result;
        }

        private BinaryReader? _breader = null;
        private Queue<byte>? _inputQue = null;
    }
    internal class BitStreamReader
    {
        public BitStreamReader(BinaryReader stream, bool compatabilityMode = true)
        {
            _rack = 0x00;
            _mask = 0x80;
            _input = new ReaderObject(stream);
            _addedZeros = 0;
            _compatabilityMode = compatabilityMode;
        }

        public BitStreamReader(Queue<byte> input, bool compatabilityMode = true)
        {
            _rack = 0x00;
            _mask = 0x80;
            _input = new ReaderObject(input);
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
                    value = _input.ReadByte();
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
        private ReaderObject _input;
        private bool _compatabilityMode;
        private Int32 _addedZeros;
    }
}
