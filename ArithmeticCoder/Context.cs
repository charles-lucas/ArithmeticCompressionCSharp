using System.Text.Json.Serialization;

namespace ArithmeticCoder
{
    public class Context
    {
        public Context()
        {
            _stats = new List<Stat>();
            _order = Order.Model;
            _rollBackActions = new Stack<RollBackItem>();
            _contextKey = null;
        }

        public Context(Order order)
        {
            _stats = new List<Stat>();
            _order = order;
            _rollBackActions = new Stack<RollBackItem>();
            _contextKey = null;
        }

        public Context(Stat stat)
        {
            _stats = new List<Stat>();
            _order = Order.Model;
            _stats.Add(stat);
            _rollBackActions = new Stack<RollBackItem>();
            _contextKey = null;
        }

        public Context(Stat stat, Order order)
        {
            _stats = new List<Stat>();
            _stats.Add(stat);
            _order = order;
            _rollBackActions = new Stack<RollBackItem>();
            _contextKey = null;
        }

        public void Update(Stat stat, bool increment = true)
        {
            Int32 index = _stats.IndexOf(stat);
            Int32 i = index;
            Int32 newPosition = 0;
            Int32 oldPosition = 0;
            bool created = false;

            //is the stat in the stats list
            if (index >= 0)
            {
                //find index i to swap with
                i = FindSwapIndex(index);
                //sort/swap to new location
                //swap
                SwapStats(i, index);
                oldPosition = index;
                newPosition = i;
            }
            else
            {
                //add
                _stats.Add(stat);
                created = true;
                index = _stats.Count - 1;
                i = FindSwapIndex(index);
                SwapStats(i, index);
                oldPosition = index;
                newPosition= i;
            }
            if(increment)
            {
                _stats[i].Increment();
            }
            if (_stats[i].Count == 0xff)
            {
                Rescale();
            }
            if (_keepRollBack)
            {
                _rollBackActions.Push(new RollBackUpdate(increment, newPosition, oldPosition, created));
            }
        }

        public void Update(byte symbol) => Update(new Stat(symbol, 0));

        public void Decrement(Stat stat)
        {
            Int32 index;

            index = _stats.IndexOf(stat);
            if (index >= 0)
            {
                _stats[index].Count--;
            }
        }

        public UInt16[] Totalize(byte[] scoreboard)
        {
            UInt16[] result = new UInt16[258];
            UInt16 max = 0;
            int i = 0;

            while (true)
            {
                max = 0;
                i = _stats.Count + 1;
                result[i] = 0;
                for (; i > 1; i--)
                {
                    result[i - 1] = result[i];
                    if (_stats[i - 2].Count != 0)
                    {
                        if ((_order == Order.Control) || scoreboard[_stats[i - 2].Symbol] == 0)
                        {
                            result[i - 1] += _stats[i - 2].Count;
                        }
                        if (_stats[i - 2].Count > max)
                        {
                            max = _stats[i - 2].Count;
                        }
                    }
                }

                /*
                 * Here is where the escape calculation needs to take place.
                */
                if (max == 0)
                {
                    result[0] = 1;
                }
                else
                {
                    result[0] = (UInt16)(256 - (_stats.Count - 1));
                    result[0] *= (UInt16)(_stats.Count - 1);
                    result[0] /= 256;
                    result[0] /= max;
                    result[0]++;
                    result[0] += result[1];
                }
                if (result[0] < Constants.MAXIMUM_SCALE)
                {
                    break;
                }
                Rescale();
            }

            for (i = 0; i < _stats.Count; i++)
            {
                if (_stats[i].Count != 0)
                {
                    scoreboard[_stats[i].Symbol] = 1;
                }
            }

            return result;
        }

        public void SetRollBackCheckPoint(ContextKey key)
        {
            _keepRollBack = true;
            _contextKey = key;
        }

        public void RollBack()
        {
            RollBackItem? item = null;

            while(_rollBackActions.Count > 0)
            {
                item = _rollBackActions.Pop();
                if (item != null)
                {
                    if (item.GetType() == typeof(RollBackUpdate))
                    {
                        RollBackUpdate update = (RollBackUpdate)item;
                        UndoUpdate(update);
                    }
                }
            }
            _keepRollBack = false;
        }

        [JsonInclude]
        public List<Stat> Stats
        {  
            get
            {
                return _stats;
            }
            set
            {
                _stats = value;
            }
        }

        [JsonIgnore]
        public Order Order => _order;

        private void SwapStats(Int32 index1, Int32 index2)
        {
            Stat index1Stat = _stats[index1];
            Stat index2Stat = _stats[index2];
            if (index1 != index2)
            {
                _stats[index1] = index2Stat;
                _stats[index2] = index1Stat;
            }
        }

        private void UndoUpdate(RollBackUpdate? update)
        {
            if(update != null)
            {
                if(update.NewPosition != update.OldPosition)
                {
                    SwapStats(update.OldPosition, update.NewPosition);
                }
                if (update.Created)
                {
                    _stats.RemoveAt(update.OldPosition);
                }
                else if (update.Increment)
                {
                    _stats[update.OldPosition].Count--;
                }
            }
        }

        private Int32 FindSwapIndex(Int32 startIndex)
        {
            int result = startIndex;

            while (result > 0 && _stats[result - 1].Count == _stats[startIndex].Count)
            {
                result--;
            }

            return result;
        }

        public void Rescale()
        {
            foreach (Stat stat in _stats)
            {
                stat.Flush();
            }
            for (int i = _stats.Count - 1; _stats[i].Count == 0 && i >= 0; i--)
            {
                _stats.RemoveAt(i);
            }
        }

        private List<Stat> _stats;
        private Order _order;
        private bool _keepRollBack;
        private Stack<RollBackItem> _rollBackActions;
        private ContextKey? _contextKey;
    }
}
