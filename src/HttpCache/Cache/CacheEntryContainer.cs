namespace Tavis.HttpCache.Cache
{
    using System.Collections.Generic;

    public class CacheEntryContainer
    {
        public CacheEntryContainer(CacheKey primaryCacheKey)
        {
            PrimaryCacheKey = primaryCacheKey;
        }

        public CacheKey PrimaryCacheKey { get; }

        public List<CacheEntry> Entries { get; } = new List<CacheEntry>();
    }
}