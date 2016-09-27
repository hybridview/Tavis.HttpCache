namespace Tavis.HttpCache.Cache
{
    public enum CacheStatus
    {
        CannotUseCache,
        Revalidate,
        ReturnStored
    }
}