namespace ArithmeticCoder
{
    /// <summary>
    /// Class <c>CompressionTracker</c> that is used to track local compression ratio.
    /// </summary>
    internal class CompressionTracker
    {
        /// <summary>
        /// Property to get the singleton instance of <c>CompressionTracker</c>.
        /// </summary>
        /// <returns>Singleton instance of <c>CompressionTracker</c></returns>
        public static CompressionTracker Instance
        {
            get
            {
                if(_instance == null)
                {
                    _instance = new CompressionTracker();
                }

                return _instance;
            }
        }

        /// <summary>
        /// Constructor for <c>CompressionTracker</c>
        /// </summary>
        private CompressionTracker()
        {

        }

        /// <summary>
        /// Method to reset tracking statististics.
        /// </summary>
        public void Reset()
        {
            if (_keepRollBack)
            {
                RollBackCompressionTracker tracker = new RollBackCompressionTracker(_inputBytes, _outputBytes, _keepRollBack);
                _rollBackStack.Push(tracker);
            }
            _inputBytes = 0;
            _outputBytes = 0;
        }

        /// <summary>
        /// Method to set roll back check point.
        /// </summary>
        public void SetRollBackCheckPoint()
        {
            _keepRollBack = true;
        }

        /// <summary>
        /// Method to increment input bytes read.
        /// </summary>
        public void IncrementInput()
        {
            if (_keepRollBack)
            {
                RollBackCompressionTracker tracker = new RollBackCompressionTracker(_inputBytes, _outputBytes, _keepRollBack);
                _rollBackStack.Push(tracker);
            }
            _inputBytes++;
        }

        /// <summary>
        /// Method to increment bytes writen.
        /// </summary>
        public void IncrementOutput()
        {
            if (_keepRollBack)
            {
                RollBackCompressionTracker tracker = new RollBackCompressionTracker(_inputBytes, _outputBytes, _keepRollBack);
                _rollBackStack.Push(tracker);
            }
            _outputBytes++;
        }

        /// <summary>
        /// Method to roll back state to check point.
        /// </summary>
        public void RollBack()
        {
            RollBackItem? item = null;
            _keepRollBack = false;

            while(_rollBackStack.Count > 0)
            {
                item = _rollBackStack.Pop();
                if (item != null)
                {
                    if (item.GetType() == typeof(RollBackCompressionTracker))
                    {
                        RollBackCompressionTracker tracker = (RollBackCompressionTracker)item;
                        _inputBytes = tracker.InputBytes;
                        _outputBytes = tracker.OutputBytes;
                    }
                }
            }
        }

        /// <summary>
        /// Property to get the number of bytes read from the input.
        /// </summary>
        /// <returns><c>UInt64</c> number of bytes read from input.</returns>
        public UInt64 InputBytes => _inputBytes;

        /// <summary>
        /// Property to get the number of bytes writen to the output.
        /// </summary>
        /// <returns><c>UInt64</c> number of bytes writen to the output.</returns>
        public UInt64 OutputBytes => _outputBytes;

        private static CompressionTracker? _instance = null;
        private UInt64 _inputBytes = 0;
        private UInt64 _outputBytes = 0;
        private bool _keepRollBack = false;
        private Stack<RollBackItem> _rollBackStack = new Stack<RollBackItem>();
    }
}
