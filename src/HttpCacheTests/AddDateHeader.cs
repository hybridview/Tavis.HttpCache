namespace HttpCacheTests
{
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Tavis.HttpCache;

    public class AddDateHeader : DelegatingHandler
    {
        private readonly GetUtcNow _getUtcNow;

        public AddDateHeader(GetUtcNow getUtcNow)
        {
            _getUtcNow = getUtcNow;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var response = await base.SendAsync(request, cancellationToken);
            response.Headers.Date = _getUtcNow();
            return response;
        }
    }
}