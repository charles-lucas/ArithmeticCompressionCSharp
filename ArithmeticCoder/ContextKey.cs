using System.Text.Json.Serialization;

namespace ArithmeticCoder
{
    /// <summary>
    /// Class to used as a key for the context dictionary in <c>ModelOrderN</c>.
    /// </summary>
    internal class ContextKey : IEquatable<ContextKey>
    {
        /// <summary>
        /// Constructor, only intended for use by JSON serialization and deserialization process.
        /// </summary>
        public ContextKey()
        {
            _maxLength = 0;
            _key = new List<byte>();
        }

        /// <summary>
        /// Constructor to make a <c>ContextKey</c> with a specified max length.
        /// </summary>
        /// <param name="maxLength">The maximum length of the key.</param>
        public ContextKey(UInt32 maxLength)
        {
            _maxLength = maxLength;
            _key = new List<byte>();
        }

        /// <summary>
        /// Copy constructor to make a copy of the provided <c>ContextKey</c>.
        /// </summary>
        /// <param name="conKey">The <c>ContextKey</c> to copy.</param>
        public ContextKey(ContextKey conKey)
        {
            _maxLength = conKey.MaxLength;
            _key = new List<byte>();
            foreach (byte bite in conKey.Key)
            {
                _key.Add(bite);
            }
        }

        /// <summary>
        /// Constructor to copy and potentialy raise order of context key.
        /// </summary>
        /// <param name="conKey">The <c>ContextKey</c> to copy.</param>
        /// <param name="symbol">Symbol to add to the <c>ContextKey</c>, will trim to max length is the addition exceeds maximum value.</param>
        public ContextKey(ContextKey conKey, byte symbol)
        {
            _maxLength = conKey.MaxLength;
            _key = new List<byte>();
            foreach (byte bite in conKey.Key)
            {
                _key.Add(bite);
            }
            _key.Add(symbol);
            while (_key.Count > _maxLength)
            {
                _key.RemoveAt(0);
            }
        }

        /// <summary>
        /// Method to get the lesser <c>ContextKey</c> from the current <c>ContextKey</c>
        /// <example>ABC -> BC</example>
        /// </summary>
        /// <returns><c>ContextKey</c> that is the lesser context key of the current context key. Can return null if the current context key is empty.</returns>
        public ContextKey? GetLesser()
        {
            ContextKey? result = new ContextKey(this);
            if(result.Key.Count > 0)
            {
                result.Key.RemoveAt(0);
            }
            else
            {
                result = null;
            }

            return result;
        }

        /// <summary>
        /// Method to inquire if the <c>ContextKey</C> is empty.
        /// </summary>
        /// <returns>True, if the <c>ContextKey is empty.</returns>
        public bool Empty()
        {
            return _key.Count == 0;
        }

        /// <summary>
        /// Property used for JSON serialization and deserialization to set and get the maximum length.
        /// </summary>
        /// <param name="value">Value to set maximum length.</param>
        /// <returns><c>UInt32</c> value that is the maximum length of the <c>ContextKey</c>.</returns>
        [JsonInclude]
        public UInt32 MaxLength
        {
            get => _maxLength;
            set
            {
                _maxLength = value;
            }
        }

        /// <summary>
        /// Property used for JSON serialization and deserialization to set and get the key list.
        /// </summary>
        /// <param name="value"><c>List<byte></c> list to set key list.</param>
        /// <returns><c>List<byte></c> the key list.</returns>
        [JsonInclude]
        public List<byte> Key
        {
            get
            {
                return _key;
            }
            set
            {
                _key = value;
            }
        }

        /// <summary>
        /// Method to evaluate the equality of two <c>ContextKey</c> objects.
        /// </summary>
        /// <param name="obj">Object to compare this object with.</param>
        /// <returns>True, if the two objects are equilvent.</returns>
        public override bool Equals(object? obj) => Equals(obj as ContextKey);

        /// <summary>
        /// Method to evaluate the equality of two <c>ContextKey</c> objects.
        /// </summary>
        /// <param name="other">Object to compare this object with.</param>
        /// <returns>True, if the two objects are equilvent.</returns>
        public bool Equals(ContextKey? other)
        {
            bool result = false;
            bool allElementsCompareFail = false;

            if (other != null && _key.Count == other.Key.Count)
            {
                for (int i = 0; i < _key.Count; i++)
                {
                    if (_key[i] != other.Key[i])
                    {
                        allElementsCompareFail = true;
                    }
                }
                result = !allElementsCompareFail;
            }

            return result;
        }

        /// <summary>
        /// Property to determine if the <c>ContextKey</c> is at the maximum order.
        /// </summary>
        /// <returns>True, if the <c>ContextKey</c> is at the maximum order.</returns>
        public bool IsMaxOrder() => (_maxLength == _key.Count);

        /// <summary>
        /// Method to generate a hash code for the <c>ContextKey</c>.
        /// </summary>
        /// <returns>A 32 bit value hash code for the <c>ContextKey</c></returns>
        public override int GetHashCode()
        {
            return Djb2Hash();
        }

        /// <summary>
        /// Helper method to generate a hash code.
        /// </summary>
        /// <returns>A 32 bit value hash code for the <c>ContextKey</c></returns>
        private int Hash1()
        {
            int result = 0;
            int shift = 0;
            int mask = 0x0FFFFFFF;
            if (_key.Count <= 4)
            {
                foreach (byte bite in _key)
                {
                    result = (result << shift) | bite;
                    shift += 8;
                }
            }
            else
            {
                for (int i = 0; i < 4; i++)
                {
                    result = (result << shift) | _key[i];
                    shift += 8;
                }
                for (int i = 4; i < _key.Count; i++)
                {
                    result += _key[i];
                }
                result = (result & mask) | (_key.Count << 28);
            }

            return result;
        }

        /// <summary>
        /// Helper method to generate a hash code.
        /// </summary>
        /// <param name="p">P is a prime number used as the base for the polynomial. A common choice is 31 or 53.</param>
        /// <param name="m">M is a large prime number used as the modulus to keep the hash values within a manageable range and reduce collisions.<example>1_000_000_007</example></param>
        /// <returns>A 32 bit value hash code for the <c>ContextKey</c></returns>
        private int PolynomialHash(int p, int m)
        {
            int hash = 0;
            int pPow = 1;
            foreach (byte bite in _key)
            {
                hash = (hash + (bite - 'a' + 1) * pPow) % m;
                pPow  = (pPow * p) % m;
            }

            return hash;
        }

        /// <summary>
        /// Helper method to generate a hash code.
        /// </summary>
        /// <returns>A 32 bit value hash code for the <c>ContextKey</c></returns>
        private int Djb2Hash()
        {
            int hash = 5381;

            foreach(byte bite in _key)
            {
                hash = ((hash << 5) + hash) + bite;
            }

            return hash;
        }

        /// <summary>
        /// Method to generate a string to represent the <c>ContextKey</c>
        /// </summary>
        /// <returns>String representation of the <c>ContextKey</c></returns>
        public override string ToString()
        {
            string result = string.Empty;
            bool first = true;

            foreach(byte bite in _key)
            {
                if(!first && _maxLength > 1)
                {
                    result += " ";
                }
                else
                {
                    first = false;
                }
                result += String.Format("{0:x2}", bite);
            }

            return result;
        }

        private List<byte> _key;
        private UInt32 _maxLength;
    }
}
