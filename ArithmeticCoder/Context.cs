using System.Text.Json.Serialization;

namespace ArithmeticCoder
{
    public class Context
    {
        public Context(bool compatabilityMode = false)
        {
            _stats = new List<Stat>();
            _order = Order.Model;
            _rollBackActions = new Stack<RollBackItem>();
            _contextKey = null;
            _compatabilityMode = compatabilityMode;
        }

        public Context(Order order, bool compatabilityMode = false)
        {
            _stats = new List<Stat>();
            _order = order;
            _rollBackActions = new Stack<RollBackItem>();
            _contextKey = null;
            _compatabilityMode = compatabilityMode;
        }

        public Context(Stat stat, bool compatabilityMode = false)
        {
            _stats = new List<Stat>();
            _order = Order.Model;
            _stats.Add(stat);
            _rollBackActions = new Stack<RollBackItem>();
            _contextKey = null;
            _compatabilityMode = compatabilityMode;
        }

        public Context(Stat stat, Order order, bool compatabilityMode = false)
        {
            _stats = new List<Stat>();
            _stats.Add(stat);
            _order = order;
            _rollBackActions = new Stack<RollBackItem>();
            _contextKey = null;
            _compatabilityMode = compatabilityMode;
        }

        //* This routine is called to update the count for a particular symbol
        //* in a particular table.  The table is one of the current contexts,
        //* and the symbol is the last symbol encoded or decoded.  In principle
        //* this is a fairly simple routine, but a couple of complications make
        //* things a little messier.  First of all, the given table may not
        //* already have the symbol defined in its statistics table.  If it
        //* doesn't, the stats table has to grow and have the new guy added
        //* to it.  Secondly, the symbols are kept in sorted order by count
        //* in the table so that the table can be trimmed during the flush
        //* operation.  When this symbol is incremented, it might have to be moved
        //* up to reflect its new rank.  Finally, since the counters are only
        //* bytes, if the count reaches 255, the table absolutely must be rescaled
        //* to get the counts back down to a reasonable level.
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

        //* This routine is called to update the count for a particular symbol
        //* in a particular table.  The table is one of the current contexts,
        //* and the symbol is the last symbol encoded or decoded.  In principle
        //* this is a fairly simple routine, but a couple of complications make
        //* things a little messier.  First of all, the given table may not
        //* already have the symbol defined in its statistics table.  If it
        //* doesn't, the stats table has to grow and have the new guy added
        //* to it.  Secondly, the symbols are kept in sorted order by count
        //* in the table so that the table can be trimmed during the flush
        //* operation.  When this symbol is incremented, it might have to be moved
        //* up to reflect its new rank.  Finally, since the counters are only
        //* bytes, if the count reaches 255, the table absolutely must be rescaled
        //* to get the counts back down to a reasonable level.
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

        //* This routine has the job of creating a cumulative totals table for
        //* a given context.  The cumulative low and high for symbol c are going to
        //* be stored in totals[c+2] and totals[c+1].  Locations 0 and 1 are
        //* reserved for the special ESCAPE symbol.  The ESCAPE symbol
        //* count is calculated dynamically, and changes based on what the
        //* current context looks like.  Note also that this routine ignores
        //* any counts for symbols that have already shown up in the scoreboard,
        //* and it adds all new symbols found here to the scoreboard.  This
        //* allows us to exclude counts of symbols that have already appeared in
        //*  higher order contexts, improving compression quite a bit.
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

            if(_compatabilityMode)
            {
                //The book iterates from 0 to < MaxIndex, which misses the last element
                for (i = 0; i < _stats.Count - 1; i++)
                {
                    if (_stats[i].Count != 0)
                    {
                        scoreboard[_stats[i].Symbol] = 1;
                    }
                }
            }
            else
            {
                for (i = 0; i < _stats.Count; i++)
                {
                    if (_stats[i].Count != 0)
                    {
                        scoreboard[_stats[i].Symbol] = 1;
                    }
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

        //* Rescaling the table needs to be done for one of three reasons.
        //* First, if the maximum count for the table has exceeded 16383, it
        //* means that arithmetic coding using 16 and 32 bit registers might
        //* no longer work.  Secondly, if an individual symbol count has
        //* reached 255, it will no longer fit in a byte.  Third, if the
        //* current model isn't compressing well, the compressor program may
        //* want to rescale all tables in order to give more weight to newer
        //* statistics.  All this routine does is divide each count by 2.
        //* If any counts drop to 0, the counters can be removed from the
        //* stats table, but only if this is a leaf context.  Otherwise, we
        //* might cut a link to a higher order table.
        public void Rescale()
        {
            foreach (Stat stat in _stats)
            {
                stat.Flush();
            }
            if(!_compatabilityMode)
            {
                for (int i = _stats.Count - 1; i >= 0 && _stats[i].Count == 0; i--)
                {
                    _stats.RemoveAt(i);
                }
            }
        }

        private List<Stat> _stats;
        private Order _order;
        private bool _keepRollBack;
        private Stack<RollBackItem> _rollBackActions;
        private ContextKey? _contextKey;
        private bool _compatabilityMode;
    }
}
