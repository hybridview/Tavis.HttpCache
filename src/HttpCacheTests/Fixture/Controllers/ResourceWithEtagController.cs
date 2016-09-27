namespace HttpCacheTests.Fixture.Controllers
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Web.Http;

    public class ResourceWithEtagController : ApiController
    {
        public HttpResponseMessage Get()
        {
            var etag = new EntityTagHeaderValue("\"XYZPQR\"");

            if (Request.Headers.IfNoneMatch.Count > 0)
                if (Request.Headers.IfNoneMatch.Contains(etag))
                {
                    var notModifiedresponse = new HttpResponseMessage(HttpStatusCode.NotModified);
                    notModifiedresponse.Headers.CacheControl = new CacheControlHeaderValue
                    {
                        MaxAge = new TimeSpan(0, 0, 0, 5)
                    };
                    notModifiedresponse.Headers.ETag = etag;
                    return notModifiedresponse;
                }
            var content = new StringContent("This is cached content with an etag");
            var response = new HttpResponseMessage
            {
                Content = content
            };
            response.Headers.CacheControl = new CacheControlHeaderValue { MaxAge = new TimeSpan(0, 0, 0, 5) };

            response.Headers.ETag = etag;
            return response;
        }
    }
}