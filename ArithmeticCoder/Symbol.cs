namespace ArithmeticCoder
{
    public class Symbol
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
