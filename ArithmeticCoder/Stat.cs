using System.Text.Json.Serialization;

namespace ArithmeticCoder
{
    /// <summary>
    /// Class used to represent the statistics for a symbol.
    /// </summary>
    internal class Stat : IEquatable<Stat>
    {
        /// <summary>
        /// <c>Stat</c> constructor.
        /// </summary>
        public Stat()
        {
            _symbol = 0x0;
            _count = 0;
        }

        /// <summary>
        /// <c>Stat</c> constructor.
        /// </summary>
        /// <param name="symbol">Value of the symbol.</param>
        /// <param name="count">Value of the count.</param>
        public Stat(byte symbol, UInt32 count)
        {
            _symbol = symbol;
            _count = count;
        }

        /// <summary>
        /// Method ued to incremnt the count.
        /// </summary>
        public void Increment()
        {
            _count++;
        }

        /// <summary>
        /// Method used to reduce the count of a <c>Stat</c> by half.
        /// </summary>
        public void Flush()
        {
            _count = (_count / 2);
        }

        /// <summary>
        /// Method to compare two <c>Stat</c> objects.
        /// </summary>
        public override bool Equals(object? obj) => Equals(obj as Stat);

        /// <summary>
        /// Method to compare two <c>Stat</c> objects.
        /// </summary>
        public bool Equals(Stat? other)
        {
            return other?.Symbol == _symbol;
        }

        /// <summary>
        /// Method used to get hash code for <c>Stat</c> object.
        /// </summary>
        public override int GetHashCode() => _symbol;

        /// <summary>
        /// Property used to get and set the symbol value
        /// </summary
        [JsonInclude]
        public byte Symbol
        {
            get
            {
                return _symbol;
            }
            set
            {
                _symbol = value;
            }
        }

        /// <summary>
        /// Property used to get and set the count value
        /// </summary>
        [JsonInclude]
        public UInt32 Count
        {
            get
            {
                return _count;
            }
            set
            {
                _count = value;
            }
        }

        private byte _symbol;
        private UInt32 _count;
    }
}
