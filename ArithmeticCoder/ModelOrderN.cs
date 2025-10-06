using System.Text.Json;
using System.Text.Json.Serialization;

namespace ArithmeticCoder
{
    internal class ModelOrderN
    {
        //Ctor for JSON 
        public ModelOrderN()
        {
            _compatabilityMode = false;
            _maxOrder = 0;
            _contexts = new Dictionary<ContextKey, Context>();
            _escapedContexts = new List<ContextKey>();
            _allSymbolContext = new Context(Order.AllSymbols, _compatabilityMode);
            _controlContext = new Context(Order.Control, _compatabilityMode);
            _scoreboard = new byte[256];
            _totals = new UInt16[16];
            _order = Order.Model;
            _contextKey = new ContextKey(_maxOrder);
            _lastContext = _contextKey;
            
            _controlContext.Update(-Constants.FLUSH);
            _controlContext.Update(-Constants.DONE);
            _controlContext.Update(-Constants.EndOfPacket);

            for (int bite = 0x0; bite < 256; bite++)
            {
                _allSymbolContext.Update((byte)bite);
            }

            
            _keepRollBack = false;
            _rollBackActions = new Stack<RollBackItem>();
            _rollBackContexts = new Stack<Context>();
        }

        public ModelOrderN(UInt32 maxOrder, bool compatability = false)
        {
            _compatabilityMode = compatability;
            Context context0 = new Context(_compatabilityMode);
            _maxOrder = maxOrder;
            _contexts = new Dictionary<ContextKey, Context>();
            _escapedContexts = new List<ContextKey>();
            _allSymbolContext = new Context(Order.AllSymbols, _compatabilityMode);
            _controlContext = new Context(Order.Control, _compatabilityMode);
            _scoreboard = new byte[256];
            _totals = new UInt16[16];
            _order = Order.Model;
            _contextKey = new ContextKey(maxOrder);
            _lastContext = _contextKey;

            _contexts.Add(_contextKey, context0);

            _controlContext.Update(-Constants.FLUSH);
            _controlContext.Update(-Constants.DONE);
            _controlContext.Update(-Constants.EndOfPacket);

            for (int bite = 0x0; bite < 256; bite++)
            {
                _allSymbolContext.Update((byte)bite);
            }

            
            _keepRollBack = false;
            _rollBackActions = new Stack<RollBackItem>();
            _rollBackContexts = new Stack<Context>();

            if (_compatabilityMode)
            {
                _contexts[_contextKey].Update(new Stat(0x00, 0), false);
                while(_contextKey.Key.Count < _contextKey.MaxLength)
                {
                    _contextKey = new ContextKey(_contextKey, 0x00);
                    if(_contextKey.Key.Count != _contextKey.MaxLength)
                    {
                        _contexts.Add(_contextKey, new Context(new Stat(0x00, 0), _compatabilityMode));
                    }
                    else
                    {
                        _contexts.Add(_contextKey, new Context(_compatabilityMode));
                    }
                }
            }
        }

        //* This routine is called when a given symbol needs to be encoded.
        //* It is the job of this routine to find the symbol in the context
        //* table associated with the current table, and return the low and
        //* high counts associated with that symbol, as well as the scale.
        //* Finding the table is simple.  Unfortunately, once I find the table,
        //* I have to build the table of cumulative counts, which is
        //* expensive, and is done elsewhere.  If the symbol is found in the
        //* table, the appropriate counts are returned.  If the symbol is
        //* not found, the ESCAPE symbol probabilities are returned, and
        //* the current order is reduced.  Note also the kludge to support
        //* the order -2 character set, which consists of negative numbers
        //* instead of unsigned chars.  This insures that no match will ever
        //* be found for the EOF or FLUSH symbols in  any "normal" table.
        public bool ConvertIntToSymbol(Int32 character, Symbol symbol)
        {
            int index;
            Context table = GetCurrentContext();
            UInt32[] totals;

            totals = table.Totalize(_scoreboard);

            symbol.Scale = totals[0];

            if (_order == Order.Control)
            {
                character = -character;
            }

            if ( character > 0 && (index = table.Stats.IndexOf(new Stat((byte)character, 0))) >= 0 )
            {
                if (table.Stats[index].Count != 0)
                {
                    symbol.LowCount = totals[index + 2];
                    symbol.HighCount = totals[index + 1];
                    return false;
                }
            }

            symbol.LowCount = totals[1];
            symbol.HighCount = totals[0];

            DecrementOrder();

            return true;
        }

        public void Export(Stream output)
        {
            string json;
            StreamWriter outStream = new StreamWriter(output);
            JsonSerializerOptions options = new JsonSerializerOptions { WriteIndented = false };

            json = JsonSerializer.Serialize(this, options);
            outStream.WriteLine(json);

            foreach(var context in _contexts)
            {
                json = JsonSerializer.Serialize(context.Key, options);
                outStream.WriteLine(json);
                json = JsonSerializer.Serialize(context.Value, options);
                outStream.WriteLine(json);
            }
            outStream.Flush();
        }

        public void SetMaxOrder()
        {
            foreach (ContextKey key in _contexts.Keys)
            {
                key.MaxLength = _maxOrder;
            }
        }

        public void SetLastContext()
        {
            _lastContext = _contextKey;
        }

        //* This routine is called when decoding an arithmetic number.  In
        //* order to decode the present symbol, the current scale in the
        //* model must be determined.  This requires looking up the current
        //* table, then building the totals table.  Once that is done, the
        //* cumulative total table has the symbol scale at element 0.
        public void GetSymbolScale(Symbol symbol)
        {
            Context table = GetCurrentContext();

            _totals = table.Totalize(_scoreboard);
            symbol.Scale = _totals[0];
        }

        public Int32 ConvertSymbolToInt(Int32 count, Symbol symbol)
        {
            int character;
            Int32 result;
            Context table = GetCurrentContext();

            for(character = 0; count < _totals[character]; character++)
            {
                ;
            }

            symbol.HighCount = _totals[character - 1];
            symbol.LowCount = _totals[character];

            if(character == 1)
            {
                DecrementOrder();
                result = Constants.ESCAPE;
            }
            else if(_order == Order.Control)
            {
                result = -table.Stats[character - 2].Symbol;
            }
            else
            {
                result = table.Stats[character - 2].Symbol;
            }

            return result;
        }

        protected virtual void DecrementOrder()
        {
            if (_order == Order.Model)
            {
                _escapedContexts.Add(_contextKey);
                if (!_contextKey.Empty())
                {
                    _contextKey = _contextKey.GetLesser();
                    if (!_contexts.ContainsKey(_contextKey))
                    {
                        _contexts.Add(_contextKey, new Context(_compatabilityMode));
                        if (_keepRollBack)
                        {
                            _rollBackActions.Push(new RollBackAddSymbol(_contextKey));
                        }
                    }
                }
                else
                {
                    switch (_order)
                    {
                        case Order.Model:
                            _order = Order.AllSymbols;
                            break;
                        case Order.AllSymbols:
                            _order = Order.Control;
                            break;
                        default:
                            _order = Order.Control;
                            break;
                    }
                }
            }
            else
            {
                _order = Order.Control;
            }
        }

        //* This routine is called to flush the whole model, which it does
        //* by calling the recursive flush routine starting at the order 0
        //* table.
        public virtual void Flush()
        {
            //XXX FIXME should flush entire model
            Flush(_contextKey);
        }

        //* This routine is called when the entire model is to be flushed.
        //* This is done in an attempt to improve the compression ratio by
        //* giving greater weight to upcoming statistics.  This routine
        //* starts at the given table, and recursively calls itself to
        //* rescale every table in its list of links.  The table itself
        //* is then rescaled.
        public virtual void Flush(ContextKey contextKey)
        {
            ContextKey key;
            foreach(var stat in _contexts[contextKey].Stats)
            {
                key = new ContextKey(contextKey, stat.Symbol);
                if(_contexts.ContainsKey(key))
                {
                    Flush(key);
                }
            }
            _contexts[contextKey].Rescale();
        }

        //* This routine is called to increment the counts for the current
        //* contexts.  It is called after a character has been encoded or
        //* decoded.  All it does is call update_table for each of the
        //* current contexts, which does the work of incrementing the count.
        //* This particular version of update_model() practices update exclusion,
        //* which means that if lower order models weren't used to encode
        //* or decode the character, they don't get their counts updated.
        //* This seems to improve compression performance quite a bit.
        //* To disable update exclusion, the loop would be changed to run
        //* from 0 to max_order, instead of current_order to max_order.
        public virtual void Update(Int32 character)
        {
            if (character >= 0)
            {
                foreach (ContextKey key in _escapedContexts)
                {
                    if (_keepRollBack)
                    {
                        _contexts[key].SetRollBackCheckPoint(key);
                        _rollBackContexts.Push(_contexts[key]);
                    }
                    _contexts[key].Update((byte)character);

                }
                if (_order == Order.Model)
                {
                    if (_keepRollBack)
                    {
                        _contexts[_contextKey].SetRollBackCheckPoint(_contextKey);
                        _rollBackContexts.Push(_contexts[_contextKey]);
                    }
                    _contexts[_contextKey].Update((byte)character);
                    
                }
            }
            // Set context to max context
            if (_escapedContexts.Count != 0)
            {
                _contextKey = _escapedContexts.First();
            }
            _escapedContexts.Clear();
            _order = Order.Model;

            for (int i = 0; i < _scoreboard.Length; i++)
            {
                _scoreboard[i] = 0;
            }
        }

        //* After the model has been updated for a new character, this routine
        //* is called to "shift" into the new context.  For example, if the
        //* last context was "ABC", and the symbol 'D' had just been processed,
        //* this routine would want to update the context pointers to that
        //* context[1]=="D", contexts[2]=="CD" and contexts[3]=="BCD".  The
        //* potential problem is that some of these tables may not exist.
        //* The way this is handled is by the shift_to_next_context routine.
        //* It is passed a pointer to the "ABC" context, along with the symbol
        //* 'D', and its job is to return a pointer to "BCD".  Once we have
        //* "BCD", we can follow the lesser context pointers in order to get
        //* the pointers to "CD" and "C".  The hard work was done in
        //* shift_to_context().
        public virtual void AddSymbol(Int32 character)
        {
            if (character >= 0 && _order == Order.Model)
            {
                _contextKey = AllocateNextContext(_contextKey, (byte)character);
            }
        }

        //* This routine is called during decoding.  It is given a count that
        //* came out of the arithmetic decoder, and has to find the symbol that
        //* matches the count.  The cumulative totals are already stored in the
        //* totals[] table, from the call to get_symbol-scale, so this routine
        //* just has to look through that table.  Once the match is found,
        //* the appropriate character is returned to the caller.  Two possible
        //* complications.  First, the character might be the ESCAPE character,
        //* in which case the current_order has to be decremented.  The other
        //* complication.  First, the character might be the ESCAPE character,
        //* in which case the current_order has to be decremented.  The other
        //* complication is that the order might be -2, in which case we return
        //* the negative of the symbol so it isn't confused with a normal
        //* symbol.
        public Int32 ConvertSymbolToInt(Symbol symbol)
        {
            Int32 character;
            Context table = _contexts[_contextKey];
            Int32 result;

            for(character = 0; character < _totals[character]; character++)
            {
            }

            symbol.HighCount = _totals[character - 1];
            symbol.LowCount = _totals[character];

            if(character == 1)
            {
                DecrementOrder();
                result = Constants.ESCAPE;
            }
            else if(table.Order == Order.Control)
            {
                result = -(table.Stats[(byte)(character - 2)].Symbol);
            }
            else
            {
                result = table.Stats[(byte)(character - 2)].Symbol;
            }

            return result;
        }

        public void SetRollBackCheckPoint()
        {
            byte[] scoreboard = new byte[_scoreboard.Length];
            Array.Copy(_scoreboard, scoreboard, _scoreboard.Length);
            RollBackContextKey rollBackItem = new RollBackContextKey(_contextKey, scoreboard);
            _rollBackActions.Push(rollBackItem);
            _keepRollBack = true;
        }

        public void RollBack()
        {
            RollBackItem? item = null;
            Context? rollBackContext = null;

            do
            {
                rollBackContext = _rollBackContexts.Pop();
                if (rollBackContext != null)
                {
                    rollBackContext.RollBack();
                }
            } while (_rollBackContexts.Count > 0);

            do
            {
                item = _rollBackActions.Pop();
                if (item != null)
                {
                    if (item.GetType() == typeof(RollBackAddSymbol))
                    {
                        RollBackAddSymbol symbol = (RollBackAddSymbol)item;
                        if (_contexts.ContainsKey(symbol.ContextKey))
                        {
                            _contexts.Remove(symbol.ContextKey);
                        }
                    }
                    else if (item.GetType() == typeof(RollBackContextKey))
                    {
                        RollBackContextKey contextKey = (RollBackContextKey)item;
                        _contextKey = contextKey.ContextKey;
                        _scoreboard = contextKey.ScoreBoard;
                    }
                }
            }while(_rollBackActions.Count > 0);
            _keepRollBack = false;
        }

        public UInt128 DictionaryStats(StreamWriter stream)
        {
            Dictionary<ContextKey, UInt64> collisions = new Dictionary<ContextKey, UInt64>();
            UInt128 totalCollisions = 0;

            foreach (ContextKey key in _contexts.Keys)
            {
                if (collisions.ContainsKey(key))
                {
                    collisions[key]++;
                    totalCollisions++;
                }
                else
                {
                    collisions.Add(key, 0);
                }
            }

            stream.WriteLine(String.Format("Total entries: {0:N0}", _contexts.Count));
            stream.WriteLine(String.Format("Total collisions: {0:N0}", totalCollisions));

            foreach (var collision in collisions)
            {
                if(collision.Value != 0)
                {
                    stream.WriteLine(String.Format("Key:\t{0}\tcollisions: {1:N0}\n", collision.Key.GetHashCode(), collision.Value));
                }
            }

            return totalCollisions;
        }

        private Context GetCurrentContext()
        {
            Context table;
            if (_order == Order.Model)
            {
                table = _contexts[_contextKey];
            }
            else if (_order == Order.AllSymbols)
            {
                table = _allSymbolContext;
            }
            else
            {
                table = _controlContext;
            }

            return table;
        }

        [JsonInclude]
        public UInt32 MaxOrder
        {
            get
            {
                return _maxOrder;
            }
            set
            {
                _maxOrder = value;
            }
        }

        [JsonInclude]
        public ContextKey LastContext
        {
            get
            {
                return _lastContext;
            }
            set
            {
                _lastContext = value;
                _contextKey = value;
            }
        }

        [JsonIgnore]
        public Dictionary<ContextKey, Context> Contexts
        {
            get
            {
                return _contexts;
            }
            set
            {
                _contexts = value;
            }
        }

        private ContextKey AllocateNextContext(ContextKey key, byte character)
        {
            ContextKey? lesser = null;
            
            key = new ContextKey(key, character);
            if (!_contexts.ContainsKey(key))
            {
                _contexts.Add(key, new Context(_compatabilityMode));
                if (_keepRollBack)
                {
                    _rollBackActions.Push(new RollBackAddSymbol(key));
                }
            }

            if (_compatabilityMode)
            {   
                // ensure lessers exist 
                lesser = key.GetLesser();
                while (lesser != null && !lesser.Empty())
                {
                    if (!_contexts.ContainsKey(lesser))
                    {
                        _contexts.Add(lesser, new Context(_compatabilityMode));
                        if (_keepRollBack)
                        {
                            _rollBackActions.Push(new RollBackAddSymbol(lesser));
                        }
                    }
                    lesser = lesser.GetLesser();
                }
            }

            return key;
        }

        public void Print()
        {
            System.Console.WriteLine("Init");
            foreach (var context in _contexts)
            {
                System.Console.WriteLine("Context: '{0}'", context.Key.ToString());
                foreach (var value in context.Value.Stats)
                {
                    System.Console.WriteLine("\tSymbol:\t{0:x2}\t{1}", value.Symbol, value.Count);
                }
                System.Console.WriteLine();
            }
            System.Console.WriteLine("Key:\t{0}\n", _contextKey.ToString());
        }

        public void Print(byte character)
        {
            System.Console.WriteLine("Added:\t{0:x2}", character);
            foreach (var context in _contexts)
            {
                System.Console.WriteLine("Context: '{0}'", context.Key.ToString());
                foreach (var value in context.Value.Stats)
                {
                    System.Console.WriteLine("\tSymbol:\t{0:x2}\t{1}", value.Symbol, value.Count);
                }
                System.Console.WriteLine();
            }
            System.Console.WriteLine("Key:\t{0}\n", _contextKey.ToString());
        }

        private UInt64 SumContext(ContextKey? key)
        {
            UInt64 result = 0;
            if (key != null && !key.Empty())
            {
                //XXX FIXME should implement as iteration instead of recurision
                result = SumContext(key.GetLesser());
                foreach (var stat in _contexts[key].Stats)
                {
                    result += stat.Count;
                }
            }

            return result;
        }

        [JsonInclude]
        public ContextKey BestKey
        {
            get{
                ContextKey result = _lastContext;
                UInt128 max = 0;
                UInt64 value = 0;

                foreach(var contextKey in _contexts.Keys)
                {
                    if(contextKey.IsMaxOrder())
                    {
                        value = SumContext(contextKey);
                        if(value > max)
                        {
                            result = contextKey;
                            max = value;
                        }
                    }
                }

                return result;
            }
            set
            {
                _bestKey = value;
            }
        }

        [JsonInclude]
        public bool CompatabilityMode
        { 
            get { return _compatabilityMode; }
            set { _compatabilityMode = value; }
        }

        protected Dictionary<ContextKey, Context> _contexts;
        private List<ContextKey> _escapedContexts;
        protected Context _allSymbolContext;
        protected Context _controlContext;
        protected byte[] _scoreboard;
        protected UInt16[] _totals;
        protected Order _order;
        protected ContextKey _contextKey;
        protected UInt32 _maxOrder;
        private ContextKey _lastContext;
        private bool _compatabilityMode;
        private bool _keepRollBack;
        private Stack<RollBackItem> _rollBackActions;
        private Stack<Context> _rollBackContexts;
        ContextKey? _bestKey;
    }
}
