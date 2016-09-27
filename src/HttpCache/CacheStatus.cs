namespace Tavis.HttpCache
{
    public enum CacheStatus
    {
        CannotUseCache,
        Revalidate,
        ReturnStored
    }
}