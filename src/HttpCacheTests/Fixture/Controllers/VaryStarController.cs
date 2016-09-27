namespace HttpCacheTests.Fixture.Controllers
{
    using System;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Web.Http;

    public class VaryStarController : ApiController
    {
        public HttpResponseMessage Get()
        {
            HttpContent content = new StringContent("Hello world");

            var response = new HttpResponseMessage();
            response.Headers.Vary.Add("*");
            response.Headers.CacheControl = new CacheControlHeaderValue { MaxAge = new TimeSpan(0, 0, 0, 60) };

            response.Content = content;
            return response;
        }
    }
}