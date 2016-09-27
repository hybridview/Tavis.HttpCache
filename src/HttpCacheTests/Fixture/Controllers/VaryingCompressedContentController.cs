namespace HttpCacheTests.Fixture.Controllers
{
    using System;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Web.Http;

    public class VaryingCompressedContentController : ApiController
    {
        public HttpResponseMessage Get()
        {
            HttpContent content = new StringContent("Hello world");

            var response = new HttpResponseMessage();

            if (Request.Headers.AcceptEncoding.Contains(new StringWithQualityHeaderValue("gzip")))
            {
                response.Headers.Vary.Add("Accept-Encoding");
                content = new CompressedContent(content, "gzip");
            }
            response.Headers.CacheControl = new CacheControlHeaderValue { MaxAge = new TimeSpan(0, 0, 0, 60) };

            response.Content = content;
            return response;
        }
    }
}