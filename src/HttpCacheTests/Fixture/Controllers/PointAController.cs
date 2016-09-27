namespace HttpCacheTests.Fixture.Controllers
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Web.Http;

    public class PointAController : ApiController
    {
        public HttpResponseMessage Get()
        {
            var response = new HttpResponseMessage(HttpStatusCode.Redirect);
            response.Headers.Location = new Uri(Url.Link("Default", new { controller = "PointB" }));
            response.Headers.CacheControl = new CacheControlHeaderValue { MaxAge = new TimeSpan(0, 0, 0, 5) };
            return response;
        }
    }
}