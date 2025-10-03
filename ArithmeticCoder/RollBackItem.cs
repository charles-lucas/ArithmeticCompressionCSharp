namespace ArithmeticCoder
{
    internal class RollBackItem
    {
        public RollBackItem() { }
    }

    internal class RollBackUpdate : RollBackItem
    {
        public RollBackUpdate(bool increment, Int32 newPosition, Int32 oldPosition, bool created)
        {
            _increment = increment;
            _newPosition = newPosition;
            _oldPosition = oldPosition;
            _created = created;
        }

        public bool Increment => _increment;

        public Int32 NewPosition => _newPosition;

        public Int32 OldPosition => _oldPosition;

        public bool Created => _created;

        private bool _increment;
        private Int32 _newPosition;
        private Int32 _oldPosition;
        private bool _created;
    }

    internal class RollBackAddSymbol : RollBackItem
    {
        public RollBackAddSymbol(ContextKey key)
        {
            _contextKey = key;
        }

        public ContextKey ContextKey => _contextKey;

        private ContextKey _contextKey;
    }

    internal class RollBackContextKey : RollBackItem
    {
        public RollBackContextKey(ContextKey key, byte[] scoreboard)
        {
            _contextKey = key;
            _scoreboard = scoreboard;
        }

        public ContextKey ContextKey => _contextKey;

        public byte[] ScoreBoard => _scoreboard;

        private ContextKey _contextKey;
        private byte[] _scoreboard;
    }

    internal class RollBackCoder : RollBackItem
    {
        public RollBackCoder(UInt16 low, UInt16 high, UInt16 code, UInt64 underdflowBits)
        {
            _low = low;
            _high = high;
            _code = code;
            _underflowBits = underdflowBits;
        }

        public UInt16 Low => _low;

        public UInt16 High => _high;

        public UInt16 Code => _code;

        public UInt64 UnderflowBits => _underflowBits;

        private UInt16 _low;
        private UInt16 _high;
        private UInt16 _code;
        private UInt64 _underflowBits;
    }

    internal class RollBackBitWriter : RollBackItem
    {
        public RollBackBitWriter(byte rack, byte mask)
        {
            _rack = rack;
            _mask = mask;
        }

        public byte Rack => _rack;

        public byte Mask => _mask;

        private byte _rack;
        private byte _mask;
    }

    internal class RollBackCompressionTracker : RollBackItem
    {
        public RollBackCompressionTracker(UInt64 inputBytes, UInt64 outputBytes)
        {
            _inputBytes = inputBytes;
            _outputBytes = outputBytes;
        }

        public UInt64 InputBytes => _inputBytes;

        public UInt64 OutputBytes => _outputBytes;

        private UInt64 _inputBytes;
        private UInt64 _outputBytes;
    }
}
