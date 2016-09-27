namespace Tavis.HttpCache.Cache
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;

    public class HttpCache
    {
        private readonly IContentStore _contentStore;
        private readonly GetUtcNow _getUtcNow;
        public readonly Func<HttpResponseMessage, bool> StoreBasedOnHeuristics = (r) => false;
        public readonly Dictionary<HttpMethod, object> CacheableMethods = new Dictionary<HttpMethod, object>
        {
            {HttpMethod.Get, null},
            {HttpMethod.Head, null},
            {HttpMethod.Post, null}
        };

        public HttpCache(IContentStore contentStore, GetUtcNow getUtcNow = null)
        {
            _contentStore = contentStore;
            _getUtcNow = getUtcNow ?? (() => DateTime.UtcNow);
        }

        public bool SharedCache { get; set; }

        public async Task<CacheQueryResult> QueryCacheAsync(HttpRequestMessage request)
        {
            // Do we have anything stored for this method and URI?  Return entries for all variants
            var cacheEntryList = await _contentStore.GetEntriesAsync(new CacheKey(request.RequestUri, request.Method)).ConfigureAwait(false);
            if (cacheEntryList == null)  // Should I use null or Count() == 0 ?
            {
                return CacheQueryResult.CannotUseCache();
            }

            // Find the first matching variant based on the vary header fields defined in the cacheEntry 
            // and the values in the request
            var selectedEntry = MatchVariant(request, cacheEntryList);

            // Do we have a matching variant representation?
            if (selectedEntry == null)
            {
                return CacheQueryResult.CannotUseCache();
            }

            // Get the complete response, including body based on the selected variant
            var response = await _contentStore.GetResponseAsync(selectedEntry.VariantId).ConfigureAwait(false);

            // Do caching directives require that we revalidate it regardless of freshness?
            var requestCacheControl = request.Headers.CacheControl ?? new CacheControlHeaderValue();
            if ((requestCacheControl.NoCache || selectedEntry.CacheControl.NoCache))
            {
                return CacheQueryResult.Revalidate(this, selectedEntry, response);
            }

            // Is it fresh?
            if (selectedEntry.IsFresh())
            {
                
                if (requestCacheControl.MinFresh != null)
                {
                    var age = CalculateAge(response);
                    if (age <= requestCacheControl.MinFresh)
                    {
                        return CacheQueryResult.ReturnStored(this, selectedEntry,response);
                    }
                }
                else
                {
                    return CacheQueryResult.ReturnStored(this, selectedEntry,response);    
                }
                
            }

            // Did the client say we can serve it stale?
            if (requestCacheControl.MaxStale)
            {
                if (requestCacheControl.MaxStaleLimit != null)
                {
                    if (_getUtcNow() - selectedEntry.Expires <= requestCacheControl.MaxStaleLimit)
                    {
                        return CacheQueryResult.ReturnStored(this, selectedEntry,response);
                    }
                }
                else
                {
                    return CacheQueryResult.ReturnStored(this, selectedEntry,response);    
                }
            }

            // Do we have a selector to allow us to do a conditional request to revalidate it?
            return selectedEntry.HasValidator 
                ? CacheQueryResult.Revalidate(this, selectedEntry, response) 
                : CacheQueryResult.CannotUseCache(); // Can't do anything to help
        }

        public bool CanStore(HttpResponseMessage response)
        {
            // Only cache responses from methods that allow their responses to be cached
            if (!CacheableMethods.ContainsKey(response.RequestMessage.Method)) return false;
            
            
            // Ensure that storing is not explicitly prohibited
            if (response.RequestMessage.Headers.CacheControl != null && response.RequestMessage.Headers.CacheControl.NoStore) return false;

            var cacheControlHeaderValue = response.Headers.CacheControl;
            if (cacheControlHeaderValue != null && cacheControlHeaderValue.NoStore) return false;

            if (SharedCache)
            {
                if (cacheControlHeaderValue != null && cacheControlHeaderValue.Private) return false;
                if (response.RequestMessage.Headers.Authorization != null )
                {
                    if (cacheControlHeaderValue == null || !(cacheControlHeaderValue.MustRevalidate
                                                    || cacheControlHeaderValue.SharedMaxAge != null
                                                    || cacheControlHeaderValue.Public))
                    {
                        return false;
                    }
                }

            }
     
            if (response.Content != null && response.Content.Headers.Expires != null) return true;
            if (cacheControlHeaderValue != null)
            {
                if (cacheControlHeaderValue.MaxAge != null) return true;
                if (cacheControlHeaderValue.SharedMaxAge != null) return true;
            }

            var sc = (int) response.StatusCode;
            if ( sc == 200 || sc == 203 || sc == 204 || 
                 sc == 206 || sc == 300 || sc == 301 || 
                 sc == 404 || sc == 405 || sc == 410 || 
                 sc == 414 || sc == 501)
            {
                return StoreBasedOnHeuristics(response);
            }

            return false;
        }

        public async Task UpdateFreshnessAsync(CacheQueryResult result, HttpResponseMessage notModifiedResponse )
        {
            var selectedEntry = result.SelectedEntry;

            UpdateCacheEntry(notModifiedResponse, selectedEntry);

            await _contentStore.UpdateEntryAsync(selectedEntry, result.SelectedResponse).ConfigureAwait(false);  //TODO
        }

        public async Task StoreResponseAsync(HttpResponseMessage response)
        {
            var primaryCacheKey = new CacheKey(response.RequestMessage.RequestUri, response.RequestMessage.Method);

            CacheEntry selectedEntry = null;

            IEnumerable<CacheEntry> cacheEntries = await _contentStore.GetEntriesAsync(primaryCacheKey).ConfigureAwait(false);
            if (cacheEntries != null)
            {
                selectedEntry = MatchVariant(response.RequestMessage, cacheEntries);
            }

            if (selectedEntry != null)
            {
                UpdateCacheEntry(response, selectedEntry);
                await _contentStore.UpdateEntryAsync(selectedEntry, response).ConfigureAwait(false);
            }
            else
            {
                selectedEntry = new CacheEntry(primaryCacheKey, response, _getUtcNow);
                UpdateCacheEntry(response, selectedEntry);
                await _contentStore.AddEntryAsync(selectedEntry, response).ConfigureAwait(false);
            }
        }

        private static CacheEntry MatchVariant(HttpRequestMessage request, IEnumerable<CacheEntry> cacheEntryList)
        {
            var selectedEntry = cacheEntryList?
                .Where(ce => ce.Match(request))
                .OrderByDescending(ce => ce.Date)
                .FirstOrDefault();
            return selectedEntry;
        }

        private void UpdateCacheEntry(HttpResponseMessage updatedResponse, CacheEntry entry)
        {
            var newExpires = GetExpireDate(updatedResponse);

            if (newExpires > entry.Expires)
            {
                entry.Expires = newExpires;
            }
            entry.Etag = updatedResponse.Headers.ETag?.Tag;
            if (updatedResponse.Content != null)
            {
                entry.LastModified = updatedResponse.Content.Headers.LastModified;
            }
        }

        private DateTimeOffset GetExpireDate(HttpResponseMessage response)
        {
            if (response.Headers.CacheControl?.MaxAge != null)
            {
                return _getUtcNow() + response.Headers.CacheControl.MaxAge.Value;
            }
            return response.Content?.Headers.Expires ?? _getUtcNow();
        }

        public static void ApplyConditionalHeaders(CacheQueryResult result, HttpRequestMessage request)
        {
            Debug.Assert(result.SelectedEntry != null);
            if (result.SelectedEntry == null || !result.SelectedEntry.HasValidator) return;

            if (result.SelectedEntry.Etag != null)
            {
                request.Headers.IfNoneMatch.Add(new EntityTagHeaderValue(result.SelectedEntry.Etag));
            }
            else
            {
                if (result.SelectedEntry.LastModified != null)
                {
                    request.Headers.IfModifiedSince = result.SelectedEntry.LastModified;
                }
            }
        }

        public void UpdateAgeHeader(HttpResponseMessage response)
        {
            if (response.Headers.Date.HasValue)
            {
                response.Headers.Age = CalculateAge(response);
            }
        }

        public TimeSpan CalculateAge(HttpResponseMessage response)
        {
            var age = _getUtcNow() - response.Headers.Date.Value;
            if (age.TotalMilliseconds < 0) age = new TimeSpan(0);
            
            return new TimeSpan(0, 0, (int) Math.Round(age.TotalSeconds));;
        }
    }
}