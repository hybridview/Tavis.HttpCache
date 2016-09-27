namespace HttpCacheTests.Fixture.Controllers
{
    using System;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;
    using System.Web.Http;

    public class CacheablePostResponseController : ApiController
    {
        public HttpResponseMessage Get()
        {
            var response = new HttpResponseMessage
            {
                Content = new StringContent("From the server")
            };
            return response;
        }

        public async Task<HttpResponseMessage> Post()
        {
            var response = new HttpResponseMessage
            {
                Content = new StringContent("Post Response : " + await Request.Content.ReadAsStringAsync())
            };
            response.Headers.CacheControl = new CacheControlHeaderValue { MaxAge = new TimeSpan(0, 0, 0, 5) };

            return response;
        }
    }
}