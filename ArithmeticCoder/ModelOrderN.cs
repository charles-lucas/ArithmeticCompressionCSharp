using System.Text.Json;
using System.Text.Json.Serialization;

namespace ArithmeticCoder
{
    public class ModelOrderN : IModel
    {
        public ModelOrderN(UInt32 maxOrder)
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

            _contexts.Add(_contextKey, context0);

            _controlContext.Update(-Constants.FLUSH);
            _controlContext.Update(-Constants.DONE);

            for (int bite = 0x0; bite <= 256; bite++)
            {
                _allSymbolContext.Update((byte)bite);
            }
        }

        public bool ConvertIntToSymbol(Int32 character, Symbol symbol)
        {
            int i;
            Context table;
            UInt16[] totals;

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

            //TotalizeTable( table );
            totals = table.Totalize(_scoreboard);

            symbol.Scale = totals[0];

            if (_order == Order.Control)
            {
                character = -character;
            }

            if ( (i = table.Stats.IndexOf(new Stat((byte)character, 0))) >= 0 )
            {
                if (table.Stats[i].Count != 0)
                {
                    symbol.LowCount = totals[i + 2];
                    symbol.HighCount = totals[i + 1];
                    return false;
                }
            }

            symbol.LowCount = totals[1];
            symbol.HighCount = totals[0];

            DecrementOrder();

            return true;
        }

        public string ToJson()
        {
            string json;
            JsonSerializerOptions options = new JsonSerializerOptions{ WriteIndented = true };
            json = JsonSerializer.Serialize(_contexts, options);

            return json;
        }

        public void FromJson(string json)
        {

        }

        public void SetMaxOrder()
        {
            foreach (ContextKey key in _contexts.Keys)
            {
                key.MaxLength = _maxOrder;
            }
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
            foreach(Context context in _contexts.Values)
            {
                context.Rescale();
            }
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
                // Set context to max context
                if (_escapedContexts.Count != 0)
                {
                    _contextKey = _escapedContexts.First();
                }
                _escapedContexts.Clear();
            }
        }

        public void AddSymbol(Int32 character)
        {
            if (character >= 0)
            {
                _contextKey = new ContextKey(_contextKey, (byte)character);
                if (!_contexts.ContainsKey(_contextKey))
                {
                    _contexts.Add(_contextKey, new Context());
                }
                _order = Order.Model;
            }
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

        private Dictionary<ContextKey, Context> _contexts;
        private List<ContextKey> _escapedContexts;
        private Context _allSymbolContext;
        private Context _controlContext;
        private byte[] _scoreboard;
        private UInt16[] _totals;
        private Order _order;
        private ContextKey _contextKey;
        private UInt32 _maxOrder;
    }
}
