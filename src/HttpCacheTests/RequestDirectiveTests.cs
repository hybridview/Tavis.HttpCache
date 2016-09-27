using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Xunit;

namespace HttpCacheTests
{
    using HttpCacheTests.Fixture;

    public class RequestDirectiveTests
    {
        private readonly TestServerFixture _fixture;
        private readonly HttpClient _client;

        public RequestDirectiveTests()
        {
            _fixture = new TestServerFixture();
            _client = _fixture.Client;
        }

        [Fact]
        public async Task Refuse_cached()
        {
            var response = await _client.GetAsync("/CacheableResource"); // Round trip to server
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            HttpAssert.FromServer(response);

            var request = new HttpRequestMessage()
            {
                RequestUri = new Uri("/CacheableResource", UriKind.Relative)
            };
            request.Headers.CacheControl = new CacheControlHeaderValue() {NoCache = true};

            var response2 = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
            HttpAssert.FromServer(response2);
        }

        [Fact]
        public async Task Indicate_that_stale_requests_are_ok_with_messagehandler()
        {
            var response = await _client.GetAsync("/CacheableResource");  // Round trip to server
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            HttpAssert.FromServer(response);

            _fixture.TimeTravel(6); // Stored representation is now stale by 1 second

            var request = new HttpRequestMessage()
            {
                RequestUri = new Uri("/CacheableResource", UriKind.Relative)
            };
            request.Headers.CacheControl = new CacheControlHeaderValue()
            {
                MaxStale = true,
                MaxStaleLimit = new TimeSpan(0, 0, 3)
            };
            var response2 = await _client.SendAsync(request);  // From Cache
            Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
            HttpAssert.FromCache(response2);

            _fixture.TimeTravel(3); // Stored representation is now stale by 4 second

            var request2 = new HttpRequestMessage()
            {
                RequestUri = new Uri("/CacheableResource", UriKind.Relative)
            };
            request2.Headers.CacheControl = new CacheControlHeaderValue()
            {
                MaxStale = true,
                MaxStaleLimit = new TimeSpan(0, 0, 3)
            };

            var response3 = await _client.SendAsync(request2);
            Assert.Equal(HttpStatusCode.OK, response3.StatusCode);
            HttpAssert.FromServer(response3);
        }

        [Fact]
        public async Task Require_minimum_freshness_with_messagehandler()
        {
            var response = await _client.GetAsync("/CacheableResource");  // Round trip to server

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            HttpAssert.FromServer(response);

            _fixture.TimeTravel(3);
            var request2 = new HttpRequestMessage()
            {
                RequestUri = new Uri("/CacheableResource", UriKind.Relative)
            };
            request2.Headers.CacheControl = new CacheControlHeaderValue()
            {
                MinFresh = new TimeSpan(0, 0, 2)
            };

            var response2 = await _client.SendAsync(request2);
            Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
            HttpAssert.FromServer(response2);

            _fixture.TimeTravel(3);
            var request3 = new HttpRequestMessage()
            {
                RequestUri = new Uri("/CacheableResource", UriKind.Relative)
            };
            request3.Headers.CacheControl = new CacheControlHeaderValue()
            {
                MinFresh = new TimeSpan(0, 0, 4)
            };

            var response3 = await _client.SendAsync(request3);
            Assert.Equal(HttpStatusCode.OK, response3.StatusCode);
            HttpAssert.FromCache(response3);
        }

        [Fact]
        public async Task Request_only_cached_content_with_messagehandler()
        {
            var response = await _client.GetAsync("/CacheableResource");  // Round trip to server

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            HttpAssert.FromServer(response);

            _fixture.TimeTravel(1);
            var request2 = new HttpRequestMessage()
            {
                RequestUri = new Uri("/CacheableResource", UriKind.Relative)
            };
            request2.Headers.CacheControl = new CacheControlHeaderValue()
            {
                OnlyIfCached = true
            };

            var response2 = await _client.SendAsync(request2);
            Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
            HttpAssert.FromCache(response2);

            _fixture.TimeTravel(5);
            var request3 = new HttpRequestMessage
            {
                RequestUri = new Uri("/CacheableResource", UriKind.Relative)
            };
            request3.Headers.CacheControl = new CacheControlHeaderValue()
            {
                OnlyIfCached = true
            };

            var response3 = await _client.SendAsync(request3);
            Assert.Equal(HttpStatusCode.GatewayTimeout, response3.StatusCode);
        }

        [Fact]
        public async Task Request_that_response_not_be_stored_with_messagehandler()
        {
            var request = new HttpRequestMessage()
            {
                RequestUri = new Uri("/CacheableResource", UriKind.Relative)
            };
            request.Headers.CacheControl = new CacheControlHeaderValue()
            {
                NoStore = true
            };

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            HttpAssert.FromServer(response);

            var request2 = new HttpRequestMessage()
            {
                RequestUri = new Uri("/CacheableResource", UriKind.Relative)
            };

            var response2 = await _client.SendAsync(request2);
            Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
            HttpAssert.FromServer(response2);
        }
    }
}
