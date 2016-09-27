namespace HttpCacheTests
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading;
    using System.Threading.Tasks;
    using HttpCacheTests.Fixture;
    using Tavis;
    using Tavis.HttpCache;
    using Xunit;

    public class ResponseDirectiveTests
    {
        private readonly TestServerFixture _fixture;
        private readonly HttpClient _client;

        public ResponseDirectiveTests()
        {
            _fixture = new TestServerFixture();
            _client = _fixture.Client;
        }

        [Fact]
        public async Task Private_caching_a_POST_response()
        {
            // Cache-Control: max-age=5

            var response = await _client.PostAsync("/CacheablePostResponse", new StringContent("Here is a message"));
                // Server round trip
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            HttpAssert.FromServer(response);

            var response2 = await _client.GetAsync("/CacheablePostResponse"); // No round trip
            Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
            HttpAssert.FromCache(response2);

            _fixture.TimeTravel(7); // Pause for resource to expire

            var response3 = await _client.GetAsync("/CacheablePostResponse"); // Server round trip
            Assert.Equal(HttpStatusCode.OK, response3.StatusCode);
            HttpAssert.FromServer(response3);
        }

        [Fact]
        public async Task Private_caching_a_redirect()
        {
            // Cache-Control: max-age=5
            var response = await _client.GetAsync("/PointA"); // Server round trip
            Assert.Equal(HttpStatusCode.Found, response.StatusCode);
            HttpAssert.FromServer(response);

            var response2 = await _client.GetAsync("/PointA"); // No round trip
            Assert.Equal(HttpStatusCode.Found, response2.StatusCode);
            HttpAssert.FromCache(response2);

            _fixture.TimeTravel(7); // Pause for resource to expire

            var response3 = await _client.GetAsync("/PointA"); // Server round trip
            Assert.Equal(HttpStatusCode.Found, response3.StatusCode);
            HttpAssert.FromServer(response3);
        }

        [Fact]
        public async Task Private_caching_accept_language_vary_header_and_request_with_no_accept_language()
        {
            var linkEnglish = new Link
            {
                Target = new Uri("/VaryingCacheableResource", UriKind.Relative)
            };

            var linkEnglishRequest = linkEnglish.CreateRequest();
            linkEnglishRequest.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue("en"));

            var response = await _client.SendAsync(linkEnglishRequest);
            HttpAssert.FromServer(response);

            var response2 = await _client.SendAsync(linkEnglish.CreateRequest());

            HttpAssert.FromServer(response2);
        }

        [Fact]
        public async Task Private_caching_with_accept_language_vary_header()
        {
            var linkEnglish = new Link
            {
                Target = new Uri("/VaryingCacheableResource", UriKind.Relative)
            };
            linkEnglish.AddRequestBuilder(
                request => { request.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue("en")); });

            var linkFrench = new Link
            {
                Target = new Uri("/VaryingCacheableResource", UriKind.Relative)
            };
            linkFrench.AddRequestBuilder(
                request => { request.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue("fr")); });

            var response = await _client.SendAsync(linkEnglish.CreateRequest());
            Assert.Equal("This is cached content", await response.Content.ReadAsStringAsync());
            HttpAssert.FromServer(response);

            var responseExplicitEn = await _client.SendAsync(linkEnglish.CreateRequest());
            var content = await responseExplicitEn.Content.ReadAsStringAsync();
            Assert.Equal("This is cached content", content);
            HttpAssert.FromCache(responseExplicitEn);

            var responseExplicitEn2 = await _client.SendAsync(linkEnglish.CreateRequest());
            var content2 = await responseExplicitEn2.Content.ReadAsStringAsync();
            Assert.Equal("This is cached content", content2);
            HttpAssert.FromCache(responseExplicitEn2);

            var responseExplicitFr = await _client.SendAsync(linkFrench.CreateRequest());
            Assert.Equal("Ce donnée est caché", await responseExplicitFr.Content.ReadAsStringAsync());
            HttpAssert.FromServer(responseExplicitFr);

            var responseExplicitFr2 = await _client.SendAsync(linkFrench.CreateRequest());
            Assert.Equal("Ce donnée est caché", await responseExplicitFr2.Content.ReadAsStringAsync());
            HttpAssert.FromCache(responseExplicitFr2);

            var responseExplicitEn3 = await _client.SendAsync(linkEnglish.CreateRequest());
            Assert.Equal("This is cached content", await responseExplicitEn3.Content.ReadAsStringAsync());
            HttpAssert.FromCache(responseExplicitEn3);
        }

        [Fact]
        public async Task Private_caching_with_encoding_vary_header()
        {
            var linkCompressed = new Link
            {
                Target = new Uri("/VaryingCompressedContent", UriKind.Relative)
            };
            linkCompressed.AddRequestBuilder(
                request => request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip")));

            var linkUnCompressed = new Link
            {
                Target = new Uri("/VaryingCompressedContent", UriKind.Relative)
            };
            linkUnCompressed.AddRequestBuilder(
                request => request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("identity")));

            var response = await _client.SendAsync(linkCompressed.CreateRequest());
            var content = await response.Content.ReadAsStringAsync();
            HttpAssert.FromServer(response);

            var response2 = await _client.SendAsync(linkCompressed.CreateRequest());
            var content2 = await response2.Content.ReadAsStringAsync();
            HttpAssert.FromCache(response2);

            var response3 = await _client.SendAsync(linkUnCompressed.CreateRequest());
            var content3 = await response3.Content.ReadAsStringAsync();
            HttpAssert.FromServer(response3);

            var response4 = await _client.SendAsync(linkUnCompressed.CreateRequest());
            var content4 = await response4.Content.ReadAsStringAsync();
            HttpAssert.FromCache(response4);

            var response5 = await _client.SendAsync(linkCompressed.CreateRequest());
            var content5 = await response5.Content.ReadAsStringAsync();
            HttpAssert.FromCache(response5);
        }

        [Fact]
        public async Task Private_caching_with_vary_star_header()
        {
            var response = await _client.GetAsync("/VaryStar");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            HttpAssert.FromServer(response);

            var response2 = await _client.GetAsync("/VaryStar");
            Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
            HttpAssert.FromServer(response2);
        }

        [Fact]
        public async Task Simple_private_caching()
        {
            // Cache-Control: max-age=5
            var response = await _client.GetAsync("/CacheableResource"); // Server round trip
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("This is cached content", await response.Content.ReadAsStringAsync());
            HttpAssert.FromServer(response);

            _fixture.TimeTravel(1); // Pause to see non-zero age

            var response2 = await _client.GetAsync("/CacheableResource"); // No round trip
            Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
            Assert.Equal("This is cached content", await response2.Content.ReadAsStringAsync());
            HttpAssert.FromCache(response2);

            var response2a = await _client.GetAsync("/CacheableResource"); // No round trip
            Assert.Equal(HttpStatusCode.OK, response2a.StatusCode);
            Assert.Equal("This is cached content", await response2a.Content.ReadAsStringAsync());
            HttpAssert.FromCache(response2);

            _fixture.TimeTravel(7); // Pause for resource to expire

            var response3 = await _client.GetAsync("/CacheableResource"); // Server round trip
            Assert.Equal(HttpStatusCode.OK, response3.StatusCode);
            Assert.Equal("This is cached content", await response3.Content.ReadAsStringAsync());
            HttpAssert.FromServer(response3);
        }

        [Fact]
        public async Task Simple_private_caching_by_method()
        {
            var headRequest = new HttpRequestMessage
            {
                RequestUri = new Uri("/CacheableResource", UriKind.Relative),
                Method = HttpMethod.Head
            };
            var response = await _client.SendAsync(headRequest); // Server round trip
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            HttpAssert.FromServer(response);

            var response2 = await _client.GetAsync("/CacheableResource"); // Server round trip
            Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
            HttpAssert.FromServer(response2);

            var headRequest2 = new HttpRequestMessage
            {
                RequestUri = new Uri("/CacheableResource", UriKind.Relative),
                Method = HttpMethod.Head
            };
            var response3 = await _client.SendAsync(headRequest2); // Local round trip
            Assert.Equal(HttpStatusCode.OK, response3.StatusCode);
            HttpAssert.FromCache(response3);

            var response4 = await _client.GetAsync("/CacheableResource"); // Local round trip
            Assert.Equal(HttpStatusCode.OK, response4.StatusCode);
            HttpAssert.FromCache(response4);

            // Cached HEAD response != cached GET response
            var content3 = await response3.Content.ReadAsStringAsync();
            var content4 = await response4.Content.ReadAsStringAsync();
            Assert.NotEqual(content3, content4);
        }

        [Fact]
        public async Task Simple_private_caching_using_query_parameter()
        {
            var response = await _client.GetAsync("/CacheableResource?value=10"); // Server roundtrip
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            HttpAssert.FromServer(response);

            var response2 = await _client.GetAsync("/CacheableResource?value=20"); // Server roundtrip
            Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
            HttpAssert.FromServer(response2);

            var response3 = await _client.GetAsync("/CacheableResource?value=10"); // From cache
            Assert.Equal(HttpStatusCode.OK, response3.StatusCode);
            HttpAssert.FromCache(response3);

            _fixture.TimeTravel(7); // Pause for resource to expire

            var response4 = await _client.GetAsync("/CacheableResource?value=10"); // Server roundtrip
            Assert.Equal(HttpStatusCode.OK, response4.StatusCode);
            HttpAssert.FromServer(response4);
        }

        [Fact]
        public async Task Simple_private_caching_with_etag()
        {
            var response = await _client.GetAsync("/ResourceWithEtag"); // Server roundtrip
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            HttpAssert.FromServer(response);

            var response2 = await _client.GetAsync("/ResourceWithEtag"); // From cache
            Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
            HttpAssert.FromCache(response2);

            _fixture.TimeTravel(7); // Pause for resource to expire

            var response3 = await _client.GetAsync("/ResourceWithEtag"); // Server roundtrip and 304
            Assert.Equal(HttpStatusCode.OK, response3.StatusCode);
            HttpAssert.FromCache(response3);

            var response4 = await _client.GetAsync("/ResourceWithEtag");
            // Server roundtrip and 304 / but should be cached
            Assert.Equal(HttpStatusCode.OK, response4.StatusCode);
            HttpAssert.FromCache(response4);
        }
    }
}