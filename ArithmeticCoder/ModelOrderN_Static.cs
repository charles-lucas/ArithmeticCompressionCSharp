namespace ArithmeticCoder
{
    internal class ModelOrderN_Static : ModelOrderN
    {
        /// <summary>
        /// Constructor for JSON deserialization
        /// </summary>
        public ModelOrderN_Static() : base()
        {
        }

        /// <summary>
        /// Constructor for <c>ModelOrderN_Static</c>, not inted for use. Static models should be loaded from JSON.
        /// </summary>
        /// <param name="maxOrder">The maxium order for the model being created.</param>
        /// <param name="compatability">True, for refernece implemetation compatability(The Compression Book).<param>
        public ModelOrderN_Static(UInt32 maxOrder, bool compatability = false)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Method to Decrement order. Override of base behavior to be static.
        /// </summary>
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

        /// <summary>
        /// Method to Flush model. Override of base behavior to be static.
        /// </summary>
        public override void Flush()
        {
        }

        /// <summary>
        /// Method to Flush context. Override of base behavior to be static.
        /// </summary>
        public override void Flush(ContextKey contextKey)
        {
        }

        /// <summary>
        /// Method to Update model. Override of base behavior to be static.
        /// </summary>
        public override void Update(Int32 character)
        {
            _order = Order.Model;

            for (int i = 0; i < _scoreboard.Length; i++)
            {
                _scoreboard[i] = 0;
            }
        }

        /// <summary>
        /// Method to add symbol model. Override of base behavior to be static.
        /// </summary>
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
