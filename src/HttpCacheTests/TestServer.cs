using System.Web.Http;

namespace HttpCacheTests
{
    public static class TestServer
    {
        public static HttpServer CreateServer()
        {
            var config = new HttpConfiguration();
            config.Routes.MapHttpRoute("default", "{controller}");
            config.MessageHandlers.Add(new AddDateHeader());
            return new HttpServer(config);
        }
    }
}
