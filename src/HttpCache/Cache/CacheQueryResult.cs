namespace Tavis.HttpCache.Cache
{
    using System.Diagnostics;
    using System.Net.Http;

    public class CacheQueryResult
    {
        public CacheEntry SelectedEntry;
        public HttpResponseMessage SelectedResponse;

        public CacheStatus Status { get; set; }

        public static CacheQueryResult CannotUseCache()
        {
            return new CacheQueryResult
            {
                Status = CacheStatus.CannotUseCache
            };
        }

        public static CacheQueryResult Revalidate(HttpCache cache, CacheEntry cacheEntry, HttpResponseMessage response)
        {
            cache.UpdateAgeHeader(response);
            return new CacheQueryResult
            {
                Status = CacheStatus.Revalidate,
                SelectedEntry = cacheEntry,
                SelectedResponse = response
            };
        }

        public static CacheQueryResult ReturnStored(HttpCache cache, CacheEntry cacheEntry, HttpResponseMessage response)
        {
            cache.UpdateAgeHeader(response);
            return new CacheQueryResult
            {
                Status = CacheStatus.ReturnStored,
                SelectedEntry = cacheEntry,
                SelectedResponse = response
            };
        }

        internal void ApplyConditionalHeaders(HttpRequestMessage request)
        {
            Debug.Assert(SelectedEntry != null);
            if ((SelectedEntry == null) || !SelectedEntry.HasValidator) return;

            var httpResponseMessage = SelectedResponse;

            if (httpResponseMessage.Headers.ETag != null)
            {
                request.Headers.IfNoneMatch.Add(httpResponseMessage.Headers.ETag);
            }
            else
            {
                if ((httpResponseMessage.Content != null) && (httpResponseMessage.Content.Headers.LastModified != null))
                    request.Headers.IfModifiedSince = httpResponseMessage.Content.Headers.LastModified;
            }
        }
    }
}