namespace ArithmeticCoder
{
    /// <summary>
    /// The SYMBOL structure is what is used to defined a symbol in
    /// arithmetic coding terms.  A symbol is defined as a range between
    /// 0 and 1.  Since we are using integer math, instead of using 0 and 1
    /// as our end points, we have an integer scale.  The low_count and
    /// high_count define where the symbol falls in the range.
    /// </summary>
    internal class Symbol
    {
        /// <summary>
        /// Constructor for <c>Symbol</c>
        /// </summary>
        public Symbol()
        {
            LowCount = 0;
            HighCount = 0;
            Scale = 0;
        }

        /// <summary>
        /// Constructor for <c>Symbol</c>
        /// </summary>
        /// <param name="low">Value used to set the low count.</param>
        /// <param name="high">Value used to set the high count.</param>
        /// <param name="scale">Value used to set the scale.</param>
        public Symbol(UInt32 low, UInt32 high, UInt32 scale)
        {
            LowCount = low;
            HighCount = high;
            Scale = scale;
        }

        /// <summary>
        /// Property for low count.
        /// </summary>
        public UInt32 LowCount;

        /// <summary>
        /// Property for high count.
        /// </summary>
        public UInt32 HighCount;

        /// <summary>
        /// Property for scale.
        /// </summary>
        public UInt32 Scale;
    }
}
