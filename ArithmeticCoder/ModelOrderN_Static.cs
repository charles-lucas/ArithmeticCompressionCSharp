namespace ArithmeticCoder
{
    internal class ModelOrderN_Static : ModelOrderN
    {
        //Ctor for JSON 
        public ModelOrderN_Static() : base()
        {
        }

        public ModelOrderN_Static(UInt32 maxOrder, bool compatability = false)
        {
            throw new NotImplementedException();
        }

        protected override void DecrementOrder()
        {
            if (_order == Order.Model)
            {
                _contextKey = _contextKey.GetLesser();
                  
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
            else
            {
                _order = Order.Control;
            }
        }

        public override void Flush()
        {
        }

        public override void Flush(ContextKey contextKey)
        {
        }

        public override void Update(Int32 character)
        {
            _order = Order.Model;

            for (int i = 0; i < _scoreboard.Length; i++)
            {
                _scoreboard[i] = 0;
            }
        }

        public override void AddSymbol(Int32 character)
        {
            if (character >= 0 && _order == Order.Model)
            {
                _contextKey = new ContextKey(_contextKey, (byte)character);
                while (_order == Order.Model && !_contexts.ContainsKey(_contextKey))
                {
                    DecrementOrder();
                }
            }
        }        
    }
}
