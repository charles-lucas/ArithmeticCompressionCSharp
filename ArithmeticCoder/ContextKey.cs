using System.Text.Json;
using System.Text.Json.Serialization;

namespace ArithmeticCoder
{
    public class ContextKey : IEquatable<ContextKey>
    {
        public ContextKey()
        {
            _maxLength = 0;
            _key = new List<byte>();
        }

        public ContextKey(UInt32 maxLength)
        {
            _maxLength = maxLength;
            _key = new List<byte>();
        }

        public ContextKey(UInt32 maxLength, byte key)
        {
            _maxLength = maxLength;
            _key = new List<byte>();
            _key.Add(key);
        }

        public ContextKey(UInt32 maxLength, string[] keys)
        {
            _maxLength = maxLength;
            _key = new List<byte>();
            foreach(string keyPart in keys)
            {
                _key.Add(byte.Parse(keyPart));
            }
           
        }

        public ContextKey(ContextKey conKey)
        {
            _maxLength = conKey.MaxLength;
            _key = new List<byte>();
            foreach (byte bite in conKey.Key)
            {
                _key.Add(bite);
            }
        }

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

        public ContextKey(string[] keys)
        {
            //int part;
            _key = new List<byte>();
            _maxLength = (UInt32)keys.Length;
            foreach (string keypart in keys)
            {
                if(int.TryParse(keypart, out int part))
                {
                    _key.Add((byte)part);
                }
            }
        }

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

        public bool Empty()
        {
            return _key.Count == 0;
        }

        [JsonInclude]
        public UInt32 MaxLength
        {
            get => _maxLength;
            set
            {
                _maxLength = value;
            }
        }

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

        public override bool Equals(object? obj) => Equals(obj as ContextKey);

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

        public override int GetHashCode()
        {
            //return PolynomialHash(2147483647, 5);
            return Djb2Hash();
        }

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

        private int PolynomialHash(int p, int m)
        {
            int hash = 5381;
            int pPow = 1;
            foreach (byte bite in _key)
            {
                hash = (hash + (bite - 'a' + 1) * pPow) % m;
                pPow  = (pPow * p) % m;
            }

            return hash;
        }

        private int Djb2Hash()
        {
            int hash = 5381;

            foreach(byte bite in _key)
            {
                hash = ((hash << 5) + hash) + bite;
            }

            return hash;
        }

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

        public static ContextKey Parse(string? value)
        {
            ContextKey result;

            if (value != null)
            {
                string[] keys = value.Split(" ");
                
                result = new ContextKey(keys);
            }
            else
            {
                result = new ContextKey(0);
            }
                return result;
        }

        private List<byte> _key;
        private UInt32 _maxLength;

}
}
