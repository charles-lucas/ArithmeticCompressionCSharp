using System.Text.Json;
using System.Text.Json.Serialization;

namespace ArithmeticCoder
{
    public class ModelOrderN
    {
        //Ctor for JSON 
        public ModelOrderN()
        {
            _maxOrder = 0;
            _contexts = new Dictionary<ContextKey, Context>();
            _escapedContexts = new List<ContextKey>();
            _allSymbolContext = new Context(Order.AllSymbols);
            _controlContext = new Context(Order.Control);
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

            _compatabilityMode = false;
        }

        public ModelOrderN(UInt32 maxOrder, bool compatability = false)
        {
            Context context0 = new Context();
            _maxOrder = maxOrder;
            _contexts = new Dictionary<ContextKey, Context>();
            _escapedContexts = new List<ContextKey>();
            _allSymbolContext = new Context(Order.AllSymbols);
            _controlContext = new Context(Order.Control);
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

            _compatabilityMode = compatability;

            if(_compatabilityMode)
            {
                _contexts[_contextKey].Update(new Stat(0x00, 0), false);
                while(_contextKey.Key.Count < _contextKey.MaxLength)
                {
                    _contextKey = new ContextKey(_contextKey, 0x00);
                    if(_contextKey.Key.Count != _contextKey.MaxLength)
                    {
                        _contexts.Add(_contextKey, new Context(new Stat(0x00, 0)));
                    }
                    else
                    {
                        _contexts.Add(_contextKey, new Context());
                    }
                }
            }
        }

        public bool ConvertIntToSymbol(Int32 character, Symbol symbol)
        {
            int index;
            Context table = GetCurrentContext();
            UInt16[] totals;

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
            JsonSerializerOptions options = new JsonSerializerOptions { WriteIndented = false};

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

        private void DecrementOrder()
        {
            if (_order == Order.Model)
            {
                _escapedContexts.Add(_contextKey);
                if (!_contextKey.Empty())
                {
                    _contextKey = _contextKey.GetLesser();
                    if (!_contexts.ContainsKey(_contextKey))
                    {
                        _contexts.Add(_contextKey, new Context());
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

        public void Flush()
        {
            Flush(_contextKey);
        }

        public void Flush(ContextKey contextKey)
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

        public void Update(Int32 character)
        {
            if (character >= 0)
            {
                foreach (ContextKey key in _escapedContexts)
                {
                    _contexts[key].Update((byte)character);
                }
                if (_order == Order.Model)
                {
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

        public void AddSymbol(Int32 character)
        {
            if (character >= 0 && _order == Order.Model)
            {
                _contextKey = AllocateNextContext(_contextKey, (byte)character);
            }
        }

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
                _contexts.Add(key, new Context());
            }

            if (_compatabilityMode)
            {   
                // ensure lessers exist 
                lesser = key.GetLesser();
                while (lesser != null && !lesser.Empty())
                {
                    if (!_contexts.ContainsKey(lesser))
                    {
                        _contexts.Add(lesser, new Context());
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

        public bool CompatabilityMode
        { 
            get;
            set;
        }

        private Dictionary<ContextKey, Context> _contexts;
        private List<ContextKey> _escapedContexts;
        private Context _allSymbolContext;
        private Context _controlContext;
        private byte[] _scoreboard;
        private UInt16[] _totals;
        private Order _order;
        private ContextKey _contextKey;
        private UInt32 _maxOrder;
        private ContextKey _lastContext;
        private bool _compatabilityMode;
    }
}
