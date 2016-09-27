namespace HttpCacheTests
{
    using System.Net.Http;
    using Xunit;

    public static class HttpAssert
    {
        public static void FromCache(HttpResponseMessage response)
        {
            Assert.NotNull(response.Headers.Age);
        }

        public static void FromServer(HttpResponseMessage response)
        {
            Assert.Null(response.Headers.Age);
        }
    }
}