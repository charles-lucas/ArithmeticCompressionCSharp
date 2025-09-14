namespace ArithmeticCoder
{
    public interface IModel
    {
        public bool ConvertIntToSymbol(Int32 character, Symbol symbol);
        public void Update(Int32 bite);
        public void AddSymbol(Int32 bite);
        public void Flush();
        public string ToJson();
        public void FromJson(string json);
    }
}
