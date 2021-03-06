using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Mime;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Hosting;
using PasswordSharing.Web;
using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using PasswordSharing.Constants;
using PasswordSharing.Web.Models;
using Shouldly;

namespace PasswordSharing.ApiTests
{
    public class PasswordApiTests : IClassFixture<WebApplicationFactory<Startup>>
    {
        private readonly WebApplicationFactory<Startup> _factory;

        public PasswordApiTests(WebApplicationFactory<Startup> factory)
        {
            var directory = Directory.GetCurrentDirectory();
            _factory = factory.WithWebHostBuilder(builder => builder
				.UseKestrel()
				.UseContentRoot(Directory.GetCurrentDirectory())
                .ConfigureAppConfiguration((builderContext, config) =>
                {
					var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

                    config.AddJsonFile(Path.Combine(directory, "appsettings.json"), optional: false, reloadOnChange: true)
						.AddJsonFile($"appsettings.{env}.json", optional: true, reloadOnChange: true);
                }));
        }

        [Fact]
        public async Task GenerateShouldGenerateLinkToPassword()
        {
            using (var httpClient = _factory.CreateClient())
            using (var request = new HttpRequestMessage(HttpMethod.Post, "api/password"))
            {
                var passwordInModel = new PasswordInModel { Passwords = new[] { "123" }, ExpiresIn = 100 };
                request.Content = new ObjectContent<PasswordInModel>(passwordInModel,
                    new JsonMediaTypeFormatter(),
                    MediaTypeNames.Application.Json);

                using (var response = await httpClient.SendAsync(request))
                {
                    response.StatusCode.ShouldBe(HttpStatusCode.OK);
                    var content = await response.Content.ReadAsAsync<UrlModel>();
                    content.Url.ShouldStartWith("http://localhost/api/password", Case.Insensitive);
                }
            }
        }

        [Fact]
        public async Task GenerateShouldGenerateLinkLessThan2048Symbols()
        {
            using (var httpClient = _factory.CreateClient())
            using (var request = new HttpRequestMessage(HttpMethod.Post, "api/password"))
            {
                var passwordInModel = new PasswordInModel { Passwords = new[] { "123" }, ExpiresIn = 100 };
                request.Content = new ObjectContent<PasswordInModel>(passwordInModel,
                    new JsonMediaTypeFormatter(),
                    MediaTypeNames.Application.Json);

                using (var response = await httpClient.SendAsync(request))
                {
                    response.StatusCode.ShouldBe(HttpStatusCode.OK);
                    var content = await response.Content.ReadAsAsync<UrlModel>();
                    content.Url.Length.ShouldBeLessThan(2048);
                }
            }
        }

        [Fact]
        public async Task PasswordShouldBeRestoredByApiProvidedLink()
        {
            using (var httpClient = _factory.CreateClient())
            using (var request = new HttpRequestMessage(HttpMethod.Post, "api/password"))
            {
                var passwordInModel = new PasswordInModel { Passwords = new[] { "123" }, ExpiresIn = 100 };
                request.Content = new ObjectContent<PasswordInModel>(passwordInModel,
                    new JsonMediaTypeFormatter(),
                    MediaTypeNames.Application.Json);

                using (var response = await httpClient.SendAsync(request))
                {
                    var content = await response.Content.ReadAsAsync<UrlModel>();

                    using (var retrieveRequest = new HttpRequestMessage(HttpMethod.Get, content.Url))
                    using (var retrieveResponse = await httpClient.SendAsync(retrieveRequest))
                    {
                        retrieveResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
                        var passwordOutModel = await retrieveResponse.Content.ReadAsAsync<PasswordOutModel>();
                        passwordOutModel.Passwords.ShouldBe(passwordInModel.Passwords);
                    }
                }
            }
        }

        [Fact]
        public async Task PasswordsShouldBeRestoredByApiProvidedLink()
        {
            using (var httpClient = _factory.CreateClient())
            using (var request = new HttpRequestMessage(HttpMethod.Post, "api/password"))
            {
                var passwordInModel = new PasswordInModel { Passwords = new[] { "1", "2", "3" }, ExpiresIn = 100 };
                request.Content = new ObjectContent<PasswordInModel>(passwordInModel,
                    new JsonMediaTypeFormatter(),
                    MediaTypeNames.Application.Json);

                using (var response = await httpClient.SendAsync(request))
                {
                    var content = await response.Content.ReadAsAsync<UrlModel>();

                    using (var retrieveRequest = new HttpRequestMessage(HttpMethod.Get, content.Url))
                    using (var retrieveResponse = await httpClient.SendAsync(retrieveRequest))
                    {
                        retrieveResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
                        var passwordOutModel = await retrieveResponse.Content.ReadAsAsync<PasswordOutModel>();
                        passwordOutModel.Passwords.ShouldBe(passwordInModel.Passwords);
                    }
                }
            }
        }

        [Fact]
        public async Task ApiShouldReturnBadRequestIfPasswordStatusChanged()
        {
            using (var httpClient = _factory.CreateClient())
            using (var request = new HttpRequestMessage(HttpMethod.Post, "api/password"))
            {
                var passwordInModel = new PasswordInModel { Passwords = new[] { "123" }, ExpiresIn = 2 };
                request.Content = new ObjectContent<PasswordInModel>(passwordInModel,
                    new JsonMediaTypeFormatter(),
                    MediaTypeNames.Application.Json);

                using (var response = await httpClient.SendAsync(request))
                {
                    var content = await response.Content.ReadAsAsync<UrlModel>();

                    await Task.Delay(TimeSpan.FromSeconds(3));

                    using (var retrieveRequest = new HttpRequestMessage(HttpMethod.Get, content.Url))
                    using (var retrieveResponse = await httpClient.SendAsync(retrieveRequest))
                    {
                        retrieveResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
                    }
                }
            }
        }

        [Fact]
        public async Task ApiShouldReturnBadRequestIfPasswordNotExists()
        {
            using (var httpClient = _factory.CreateClient())
            using (var request = new HttpRequestMessage(HttpMethod.Post, "api/password"))
            {
                var passwordInModel = new PasswordInModel { Passwords = new[] { "123" }, ExpiresIn = 200 };
                request.Content = new ObjectContent<PasswordInModel>(passwordInModel,
                    new JsonMediaTypeFormatter(),
                    MediaTypeNames.Application.Json);

                using (var response = await httpClient.SendAsync(request))
                {
                    var content = await response.Content.ReadAsAsync<UrlModel>();

                    var uri = new Uri(content.Url);
                    var key = HttpUtility.ParseQueryString(uri.Query).Get("key");

                    using (var retrieveRequest = new HttpRequestMessage(HttpMethod.Get, $"api/password/0?key={key}"))
                    using (var retrieveResponse = await httpClient.SendAsync(retrieveRequest))
                    {
                        retrieveResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
                    }
                }
            }
        }

        [Fact]
        public async Task ApiShouldReturnBadRequestIfWrongKeyProvided()
        {
            using (var httpClient = _factory.CreateClient())
            using (var request1 = new HttpRequestMessage(HttpMethod.Post, "api/password"))
            using (var request2 = new HttpRequestMessage(HttpMethod.Post, "api/password"))
            {
                var passwordInModel = new PasswordInModel { Passwords = new[] { "123" }, ExpiresIn = 200 };

                request1.Content = new ObjectContent<PasswordInModel>(passwordInModel,
                    new JsonMediaTypeFormatter(),
                    MediaTypeNames.Application.Json);

                request2.Content = new ObjectContent<PasswordInModel>(passwordInModel,
                    new JsonMediaTypeFormatter(),
                    MediaTypeNames.Application.Json);

                using (var response1 = await httpClient.SendAsync(request1))
                using (var response2 = await httpClient.SendAsync(request2))
                {
                    var content1 = await response1.Content.ReadAsAsync<UrlModel>();

                    var uri1 = new Uri(content1.Url);
                    var key1 = HttpUtility.ParseQueryString(uri1.Query).Get("key");

                    var content2 = await response2.Content.ReadAsAsync<UrlModel>();

                    var uri2 = new Uri(content2.Url);
                    var query2 = HttpUtility.ParseQueryString(uri1.Query);
                    query2.Set("key", key1);

                    using (var retrieveRequest = new HttpRequestMessage(HttpMethod.Get, $"{uri2.AbsolutePath}?{query2}"))
                    using (var retrieveResponse = await httpClient.SendAsync(retrieveRequest))
                    {
                        retrieveResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
                    }
                }
            }
        }

        [Fact]
        public async Task ApiShouldReturnBadRequestIfLinkReused()
        {
            using (var httpClient = _factory.CreateClient())
            using (var request = new HttpRequestMessage(HttpMethod.Post, "api/password"))
            {
                var passwordInModel = new PasswordInModel { Passwords = new[] { "123" }, ExpiresIn = 100 };
                request.Content = new ObjectContent<PasswordInModel>(passwordInModel,
                    new JsonMediaTypeFormatter(),
                    MediaTypeNames.Application.Json);

                using (var response = await httpClient.SendAsync(request))
                {
                    var content = await response.Content.ReadAsAsync<UrlModel>();

                    using (var retrieveRequest1 = new HttpRequestMessage(HttpMethod.Get, content.Url))
                    using (var retrieveRequest2 = new HttpRequestMessage(HttpMethod.Get, content.Url))
                    using (var retrieveResponse1 = await httpClient.SendAsync(retrieveRequest1))
                    using (var retrieveResponse2 = await httpClient.SendAsync(retrieveRequest2))
                    {
                        retrieveResponse1.StatusCode.ShouldBe(HttpStatusCode.OK);
                        retrieveResponse2.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
                    }
                }
            }
        }

        [Fact]
        public async Task GenerateApiShouldAlwaysCreateDifferentKeysEvenForOnePassword()
        {
            using (var httpClient = _factory.CreateClient())
            using (var request1 = new HttpRequestMessage(HttpMethod.Post, "api/password"))
            using (var request2 = new HttpRequestMessage(HttpMethod.Post, "api/password"))
            {
                var passwordInModel = new PasswordInModel { Passwords = new[] { "123" }, ExpiresIn = 200 };

                request1.Content = new ObjectContent<PasswordInModel>(passwordInModel,
                    new JsonMediaTypeFormatter(),
                    MediaTypeNames.Application.Json);

                request2.Content = new ObjectContent<PasswordInModel>(passwordInModel,
                    new JsonMediaTypeFormatter(),
                    MediaTypeNames.Application.Json);

                using (var response1 = await httpClient.SendAsync(request1))
                using (var response2 = await httpClient.SendAsync(request2))
                {
                    var content1 = await response1.Content.ReadAsAsync<UrlModel>();

                    var uri1 = new Uri(content1.Url);
                    var key1 = HttpUtility.ParseQueryString(uri1.Query).Get("key");

                    var content2 = await response2.Content.ReadAsAsync<UrlModel>();
                    var uri2 = new Uri(content2.Url);
                    var key2 = HttpUtility.ParseQueryString(uri2.Query).Get("key");

                    key1.ShouldNotBe(key2);
                }
            }
        }

        [Fact]
        public async Task GenerateApiShouldReturnBadRequestIfMessageWasToLong()
        {
            const int maxSize = ((AlgorithmConstants.KeySize - 384) / 8 + 37) / 2;

            using (var httpClient = _factory.CreateClient())
            using (var request1 = new HttpRequestMessage(HttpMethod.Post, "api/password"))
            using (var request2 = new HttpRequestMessage(HttpMethod.Post, "api/password"))
            {
                var passwords1 = new[] { new string(Enumerable.Repeat('s', maxSize).ToArray()) };
                var passwords2 = new[] { new string(Enumerable.Repeat('s', maxSize + 1).ToArray()) };

                var passwordInModel1 = new PasswordInModel { Passwords = passwords1, ExpiresIn = 200 };
                var passwordInModel2 = new PasswordInModel { Passwords = passwords2, ExpiresIn = 200 };

                request1.Content = new ObjectContent<PasswordInModel>(passwordInModel1,
                    new JsonMediaTypeFormatter(),
                    MediaTypeNames.Application.Json);

                request2.Content = new ObjectContent<PasswordInModel>(passwordInModel2,
                    new JsonMediaTypeFormatter(),
                    MediaTypeNames.Application.Json);

                using (var response1 = await httpClient.SendAsync(request1))
                using (var response2 = await httpClient.SendAsync(request2))
                {
                    response1.StatusCode.ShouldBe(HttpStatusCode.OK);
                    response2.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
                }
            }
        }

        [Fact]
        public async Task GenerateApiShouldReturnBadRequestIfOneOfMessagesWasToLong()
        {
            const int maxSize = ((AlgorithmConstants.KeySize - 384) / 8 + 37) / 2;

            using (var httpClient = _factory.CreateClient())
            using (var request = new HttpRequestMessage(HttpMethod.Post, "api/password"))
            {
                var passwords = new[]
                {
                    new string(Enumerable.Repeat('s', maxSize).ToArray()),
                    new string(Enumerable.Repeat('s', maxSize + 1).ToArray())
                };

                var passwordInModel = new PasswordInModel { Passwords = passwords, ExpiresIn = 200 };

                request.Content = new ObjectContent<PasswordInModel>(passwordInModel,
                    new JsonMediaTypeFormatter(),
                    MediaTypeNames.Application.Json);

                using (var response = await httpClient.SendAsync(request))
                {
                    response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
                }
            }
        }

        [Fact]
        public async Task LoadApiTest()
        {
            const int load = 100;

            using (var httpClient = _factory.CreateClient())
            {
                var allTasks = new List<Task>();

                for (var i = 0; i < load; i++)
                {
                    var task = Task.Run(async () =>
                    {
                        using (var request = new HttpRequestMessage(HttpMethod.Post, "api/password"))
                        {
                            var passwordInModel = new PasswordInModel { Passwords = new[] { "1", "2", "3" }, ExpiresIn = 100 };
                            request.Content = new ObjectContent<PasswordInModel>(passwordInModel,
                                new JsonMediaTypeFormatter(),
                                MediaTypeNames.Application.Json);

                            using (var response = await httpClient.SendAsync(request))
                            {
                                var content = await response.Content.ReadAsAsync<UrlModel>();

                                using (var retrieveRequest = new HttpRequestMessage(HttpMethod.Get, content.Url))
                                using (var retrieveResponse = await httpClient.SendAsync(retrieveRequest))
                                {
                                    retrieveResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
                                    var passwordOutModel = await retrieveResponse.Content.ReadAsAsync<PasswordOutModel>();
                                    passwordOutModel.Passwords.ShouldBe(passwordInModel.Passwords);
                                }
                            }
                        }
                    });

                    allTasks.Add(task);
                }

                await Task.WhenAll(allTasks);
            }
        }

        [Fact]
        public async Task ParallelRetrieveTest()
        {
            const int load = 100;

            using (var httpClient = _factory.CreateClient())
            {
                for (var i = 0; i < load; i++)
                {
                    using (var request = new HttpRequestMessage(HttpMethod.Post, "api/password"))
                    {
                        var passwordInModel = new PasswordInModel { Passwords = new[] { "123" }, ExpiresIn = 100 };
                        request.Content = new ObjectContent<PasswordInModel>(passwordInModel,
                            new JsonMediaTypeFormatter(),
                            MediaTypeNames.Application.Json);
                        using (var response = await httpClient.SendAsync(request))
                        {
                            var content = await response.Content.ReadAsAsync<UrlModel>();

                            using (var retrieveRequest1 = new HttpRequestMessage(HttpMethod.Get, content.Url))
                            using (var retrieveRequest2 = new HttpRequestMessage(HttpMethod.Get, content.Url))
                            using (var retrieveResponse1Task = httpClient.SendAsync(retrieveRequest1))
                            using (var retrieveResponse2Task = httpClient.SendAsync(retrieveRequest2))
                            {
                                await Task.WhenAll(retrieveResponse1Task, retrieveResponse2Task);

                                if (retrieveResponse1Task.Result.StatusCode == HttpStatusCode.OK)
                                {
                                    retrieveResponse1Task.Result.StatusCode.ShouldBe(HttpStatusCode.OK);
                                    retrieveResponse2Task.Result.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
                                }
                                else
                                {
                                    retrieveResponse2Task.Result.StatusCode.ShouldBe(HttpStatusCode.OK);
                                    retrieveResponse1Task.Result.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
