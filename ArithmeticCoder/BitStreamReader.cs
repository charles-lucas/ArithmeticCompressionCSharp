using System.IO;

namespace ArithmeticCoder
{
    /// <summary>
    /// Class <c>ReaderObject</c> wraps the actual source to read from to allow for interchangeablity.
    /// </summary>
    internal class ReaderObject
    {
        /// <summary>
        /// Constructor that creates a <c>ReaderObject</c> from a BinaryReader.
        /// </summary>
        /// <param name="reader">BinaryReader to use for read operations.</param>
        public ReaderObject(BinaryReader reader )
        {
            _breader = reader;
        }

        /// <summary>
        /// Constructor that creates a <c>ReaderObject</c> from a byte queue.
        /// </summary>
        /// <param name="input">Byte queue to use for read operations.</param>
        public ReaderObject(Queue<byte> input)
        {
            _inputQue = input;
        }

        /// <summary>
        /// Read a byte from the <c>ReaderObject</c>.
        /// </summary>
        /// <returns>Byte read from the reader.</returns>
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

    /// <summary>
    /// Class to read bits from an input source.
    /// </summary>
    internal class BitStreamReader
    {
        /// <summary>
        /// Constructor for <c>BitsStreamReder</c> which reads from the provided stream.
        /// </summary>
        /// <param name="stream"><c>BinaryReader</c> wich will be used to retrieve bits from.</param>
        /// <param name="compatabilityMode">True, to be compatible with the reference implementation.</param>
        public BitStreamReader(BinaryReader stream, bool compatabilityMode = true)
        {
            _rack = 0x00;
            _mask = 0x80;
            _input = new ReaderObject(stream);
            _addedZeros = 0;
            _compatabilityMode = compatabilityMode;
        }

        /// <summary>
        /// Constructor for <c>BitsStreamReder</c> which reads from the provided byte queue.
        /// </summary>
        /// <param name="input"><c>Queue<byte></c> wich will be used to retrieve bits from.</param>
        /// <param name="compatabilityMode">True, to be compatible with the reference implementation.</param>
        public BitStreamReader(Queue<byte> input, bool compatabilityMode = true)
        {
            _rack = 0x00;
            _mask = 0x80;
            _input = new ReaderObject(input);
            _addedZeros = 0;
            _compatabilityMode = compatabilityMode;
        }

        /// <summary>
        /// Method used to get the next bit from the source.
        /// </summary>
        /// <returns>Boolean value: True, if bit is a 1; false, if the bit is a 0.</returns>
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
