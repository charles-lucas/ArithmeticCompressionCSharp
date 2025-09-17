namespace ArithmeticCoder
{
    public interface IModel
    {
        public bool ConvertIntToSymbol(Int32 character, Symbol symbol);
        public void Update(Int32 bite);
        public void AddSymbol(Int32 bite);
        public void Flush();
        public void Export(Stream outputStream);
        public void SetMaxOrder();
    }
}
