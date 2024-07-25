namespace HexaEngine.Core
{
    public interface IConverter<TIn, TOut>
    {
        public TOut Convert(TIn value);
    }
}