namespace HttpCacheTests.Fixture.Controllers
{
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Web.Http;

    public class PointBController : ApiController
    {
        public HttpResponseMessage Get()
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StringContent("Final Destination");
            response.Headers.CacheControl = new CacheControlHeaderValue { NoCache = true };
            return response;
        }
    }
}