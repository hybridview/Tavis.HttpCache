namespace HttpCacheTests.Fixture
{
    using System;
    using System.Net.Http;
    using System.Web.Http;
    using Tavis.HttpCache;
    using Tavis.HttpCache.Cache;

    public class TestServerFixture
    {
        private TimeSpan _offSet = TimeSpan.FromSeconds(0);

        public TestServerFixture()
        {
            GetUtcNow getUtcNow = () => DateTime.UtcNow.Add(_offSet);

            var config = new HttpConfiguration();
            config.Routes.MapHttpRoute("default", "{controller}");
            config.MessageHandlers.Add(new AddDateHeader(getUtcNow));
            var httpServer = new HttpServer(config);

            var baseAddress = new Uri($"http://{Environment.MachineName}:1001");
            var httpCache = new HttpCache(new InMemoryContentStore(), getUtcNow);
            var clientHandler = new HttpCacheHandler(httpServer, httpCache);

            Client = new HttpClient(clientHandler) { BaseAddress = baseAddress };
        }

        public HttpClient Client { get; }

        public void TimeTravel(int seconds)
        {
            _offSet = _offSet.Add(TimeSpan.FromSeconds(seconds));
        }
    }
}