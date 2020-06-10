namespace Uptick.Utils.MemoryCleaner
{
    public interface IMemoryCleanerStrategy
    {
        long Max { get; }
        long Avg { get; }
        long CurrentSize { get; set; }
        bool IsReadyToCleanup { get; }
    }
}