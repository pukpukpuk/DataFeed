namespace Pukpukpuk.DataFeed.Console.Windows.Console
{
    public record HighlightData
    {
        public readonly int Index;
        public readonly int Length;

        public HighlightData(int index, int length)
        {
            Index = index;
            Length = length;
        }
    }
}