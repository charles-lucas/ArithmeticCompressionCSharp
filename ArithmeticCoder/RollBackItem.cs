namespace ArithmeticCoder
{
    /// <summary>
    /// Base class for roll back items
    /// </summary>
    internal class RollBackItem
    {
        public RollBackItem() { }
    }

    /// <summary>
    /// Class to store roll back information for updates
    /// </summary>
    internal class RollBackUpdate : RollBackItem
    {
        /// <summary>
        /// Constructor for <c>RollBackUpdate</c>
        /// </summary>
        /// <param name="increment">Whether to update incremented count.</param>
        /// <param name="newPosition">Value of new position after update.</param>
        /// <param name="oldPosition">Value of position before update.</param>
        /// <param name="created">Whether the stat was created by the update.</param>
        /// <param name="stats">Stats list to roll back to at start of rollback.</param>
        public RollBackUpdate(bool increment, Int32 newPosition, Int32 oldPosition, bool created, List<Stat>? stats)
        {
            _increment = increment;
            _newPosition = newPosition;
            _oldPosition = oldPosition;
            _created = created;
            _stats = stats;
        }

        /// <summary>
        /// Property to get increment value.
        /// </summary>
        public bool Increment => _increment;

        /// <summary>
        /// Property to get new position value.
        /// </summary>
        public Int32 NewPosition => _newPosition;

        /// <summary>
        /// Property to get old position value.
        /// </summary>
        public Int32 OldPosition => _oldPosition;

        /// <summary>
        /// Property to get created value.
        /// </summary>
        public bool Created => _created;

        /// <summary>
        /// Property to get stats list.
        /// </summary>
        public List<Stat>? Stats => _stats;

        private bool _increment;
        private Int32 _newPosition;
        private Int32 _oldPosition;
        private bool _created;
        private List<Stat> _stats;
    }

    /// <summary>
    /// Class to store roll back information for add symbol
    /// </summary>
    internal class RollBackAddSymbol : RollBackItem
    {
        /// <summary>
        /// Constructor for <c>RollBackAddSymbol</c>
        /// </summary>
        /// <param name="key">context key to roll back.</param>
        public RollBackAddSymbol(ContextKey key)
        {
            _contextKey = key;
        }

        /// <summary>
        /// Property to the the context key
        /// </summary>
        public ContextKey ContextKey => _contextKey;

        private ContextKey _contextKey;
    }

    /// <summary>
    /// Class to store roll back information for context key.
    /// </summary>
    internal class RollBackContextKey : RollBackItem
    {
        /// <summary>
        /// Constructor for <c>RollBackContextKey</c>.
        /// </summary>
        /// <param name="key">Context key for roll back.</param>
        /// <param name="scoreboard">scoreboard for roll back.</param>
        public RollBackContextKey(ContextKey key, byte[] scoreboard)
        {
            _contextKey = key;
            _scoreboard = scoreboard;
        }

        /// <summary>
        /// Property to get context key
        /// </summary>
        public ContextKey ContextKey => _contextKey;

        /// <summary>
        /// Property to get the scorboard.
        public byte[] ScoreBoard => _scoreboard;

        private ContextKey _contextKey;
        private byte[] _scoreboard;
    }

    /// <summary>
    /// Class to store roll back information for coder
    /// </summary>
    internal class RollBackCoder : RollBackItem
    {
        /// <summary>
        /// Constructor for <c>RollBackCoder</c>
        /// </summary>
        /// <param name="low">Value of low for roll back.</param>
        /// <param name="high">Value of high for roll back.</param>
        /// <param name="code">Value of code for roll back.</param>
        /// <param name="underflowBits">Value of underflowBits for roll back.</param>
        public RollBackCoder(UInt16 low, UInt16 high, UInt16 code, UInt64 underdflowBits)
        {
            _low = low;
            _high = high;
            _code = code;
            _underflowBits = underdflowBits;
        }

        /// <summary>
        /// Property to get the low value.
        /// </summary>
        public UInt16 Low => _low;

        /// <summary>
        /// Property to get the high value.
        /// </summary>
        public UInt16 High => _high;

        /// <summary>
        /// Property to get the code value.
        /// </summary>
        public UInt16 Code => _code;

        /// <summary>
        /// Property to get the underflowBits value.
        /// </summary>
        public UInt64 UnderflowBits => _underflowBits;

        private UInt16 _low;
        private UInt16 _high;
        private UInt16 _code;
        private UInt64 _underflowBits;
    }

    /// <summary>
    /// Class to store roll back information for bit writer
    /// </summary>
    internal class RollBackBitWriter : RollBackItem
    {
        /// <summary>
        /// </summary>
        /// <param name="rack">Value for rack for roll back.</param>
        /// <param name="mask">Value for mask for roll back.</param>
        public RollBackBitWriter(byte rack, byte mask)
        {
            _rack = rack;
            _mask = mask;
        }

        /// <summary>
        /// Property to get rack value.
        /// </summary>
        public byte Rack => _rack;

        /// <summary>
        /// Property to get mask value.
        /// </summary>
        public byte Mask => _mask;

        private byte _rack;
        private byte _mask;
    }

    /// <summary>
    /// Class to store roll back information for compression tracker
    /// </summary>
    internal class RollBackCompressionTracker : RollBackItem
    {
        /// <summary>
        /// Constructor for <c>RollBackCompressionTracker</c>
        /// </summary>
        /// <param name="inputBytes">Value of input bytes for roll back.</param>
        /// <param name="outputBytes">Value of output bytes for roll back.</param>
        /// <param name="keepRollBack">Value of keepRollBack for roll back.</param>
        public RollBackCompressionTracker(UInt64 inputBytes, UInt64 outputBytes, bool keepRollBack)
        {
            _inputBytes = inputBytes;
            _outputBytes = outputBytes;
            _keepRollBack = keepRollBack;
        }

        /// <summary>
        /// Property to get input bytes value
        /// </summary>
        public UInt64 InputBytes => _inputBytes;

        /// <summary>
        /// Property to get output bytes value
        /// </summary>
        public UInt64 OutputBytes => _outputBytes;

        /// <summary>
        /// Property to get keepRollBack value
        /// </summary>
        public bool KeepRollBack => _keepRollBack;

        private UInt64 _inputBytes;
        private UInt64 _outputBytes;
        private bool _keepRollBack;
    }
}
