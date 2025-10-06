namespace ArithmeticCoder
{
    /*
    * The SYMBOL structure is what is used to defined a symbol in
    * arithmetic coding terms.  A symbol is defined as a range between
    * 0 and 1.  Since we are using integer math, instead of using 0 and 1
    * as our end points, we have an integer scale.  The low_count and
    * high_count define where the symbol falls in the range.
    */
    internal class Symbol
    {
        public Symbol()
        {
            LowCount = 0;
            HighCount = 0;
            Scale = 0;
        }

        public Symbol(UInt16 low, UInt16 high, UInt16 scale)
        {
            LowCount = low;
            HighCount = high;
            Scale = scale;
        }

        public UInt16 LowCount;
        public UInt16 HighCount;
        public UInt16 Scale;
    }
}
