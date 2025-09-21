using System.Text.Json.Serialization;

namespace ArithmeticCoder
{
    public class Context
    {
        public Context()
        {
            _stats = new List<Stat>();
            _order = Order.Model;
        }

        public Context(Order order)
        {
            _stats = new List<Stat>();
            _order = order;
        }

        public Context(Stat stat)
        {
            _stats = new List<Stat>();
            _order = Order.Model;
            _stats.Add(stat);
        }

        public Context(Stat stat, Order order)
        {
            _stats = new List<Stat>();
            _stats.Add(stat);
            _order = order;
        }

        public void Update(Stat stat, bool increment = true)
        {
            int index = _stats.IndexOf(stat);
            int i = index;

            //is the stat in the stats list
            if (index >= 0)
            {
                //find index i to swap with
                i = FindSwapIndex(index);
                //sort/swap to new location
                //swap
                SwapStats(i, index);
            }
            else
            {
                //add
                _stats.Add(stat);
                index = _stats.Count - 1;
                i = FindSwapIndex(index);
                SwapStats(i, index);
            }
            if(increment)
            {
                _stats[i].Increment();
            }
            if (_stats[i].Count == 0xff)
            {
                Rescale();
            }
        }

        public void Update(byte symbol) => Update(new Stat(symbol, 0));

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
    }
}
