namespace Hexa.NET.Utilities
{
    public interface IConverter<TIn, TOut>
    {
        public TOut Convert(TIn value);
    }
}