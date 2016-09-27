namespace HttpCacheTests
{
    using System;
    using System.Net.Http;
    using Tavis;

    public static class LinkExtensions
    {
        public static void AddRequestBuilder(this Link link, Action<HttpRequestMessage> requestBuilderAction)
        {
            link.AddRequestBuilder(request =>
            {
                requestBuilderAction(request);
                return request;
            });
        }
    }
}