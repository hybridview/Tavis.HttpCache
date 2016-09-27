namespace HttpCacheTests.Fixture.Controllers
{
    using System;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Web.Http;

    public class VaryingCacheableResourceController : ApiController
    {
        public HttpResponseMessage Get()
        {
            StringContent stringContent;
            if (Request.Headers.AcceptLanguage.Contains(new StringWithQualityHeaderValue("fr")))
                stringContent = new StringContent("Ce donnée est caché");
            else
                stringContent = new StringContent("This is cached content");


            var response = new HttpResponseMessage
            {
                Content = stringContent
            };
            response.Headers.CacheControl = new CacheControlHeaderValue { MaxAge = new TimeSpan(0, 0, 0, 60) };
            response.Headers.Vary.Add("accept-language");
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