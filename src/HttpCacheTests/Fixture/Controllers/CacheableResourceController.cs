namespace HttpCacheTests.Fixture.Controllers
{
    using System;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Web.Http;

    public class CacheableResourceController : ApiController
    {
        public HttpResponseMessage Head()
        {
            var response = new HttpResponseMessage();
            response.Headers.Add("CacheableResource", "testheader");
            response.Headers.CacheControl = new CacheControlHeaderValue
            {
                MaxAge = new TimeSpan(0, 0, 0, 5)
            };
            response.Content = new StringContent("");
            return response;
        }


        public HttpResponseMessage Get()
        {
            var response = new HttpResponseMessage
            {
                Content = new StringContent("This is cached content")
            };
            response.Headers.CacheControl = new CacheControlHeaderValue { MaxAge = new TimeSpan(0, 0, 0, 5) };

            response.Headers.CacheControl = new CacheControlHeaderValue
            {
                MaxAge = new TimeSpan(0, 0, 0, 5)
            };


            return response;
        }


        public HttpResponseMessage Get(string value)
        {
            var response = new HttpResponseMessage
            {
                Content = new StringContent("This is cached content with the value " + value)
            };
            response.Headers.CacheControl = new CacheControlHeaderValue { MaxAge = new TimeSpan(0, 0, 0, 5) };
            return response;
        }
    }
}