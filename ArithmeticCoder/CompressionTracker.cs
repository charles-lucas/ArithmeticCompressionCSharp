using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArithmeticCoder
{
    internal class CompressionTracker
    {
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

        private CompressionTracker()
        {

        }

        public void Reset()
        {
            _inputBytes = 0;
            _outputBytes = 0;
            _keepRollBack = false;
            _rollBackStack.Clear();
        }

        public void SetRollBackCheckPoint()
        {
            _keepRollBack = true;
        }

        public void IncrementInput()
        {
            if (_keepRollBack)
            {
                RollBackCompressionTracker tracker = new RollBackCompressionTracker(_inputBytes, _outputBytes);
                _rollBackStack.Push(tracker);
            }
            _inputBytes++;
        }

        public void IncrementOutput()
        {
            if (_keepRollBack)
            {
                RollBackCompressionTracker tracker = new RollBackCompressionTracker(_inputBytes, _outputBytes);
                _rollBackStack.Push(tracker);
            }
            _outputBytes++;
        }

        public void RollBack()
        {
            RollBackItem? item = null;
            _keepRollBack = false;

            do
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
            } while (_rollBackStack.Count > 0);
        }

        public UInt64 InputBytes => _inputBytes;

        public UInt64 OutputBytes => _outputBytes;

        private static CompressionTracker? _instance = null;

        private UInt64 _inputBytes = 0;
        private UInt64 _outputBytes = 0;
        private bool _keepRollBack = false;
        private Stack<RollBackItem> _rollBackStack = new Stack<RollBackItem>();
    }
}
