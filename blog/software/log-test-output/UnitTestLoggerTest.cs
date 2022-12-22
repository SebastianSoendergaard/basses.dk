using API;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Tests.Common.Logging
{
    public class CustomWebApplicationFactory<TStartup> : WebApplicationFactory<TStartup> where TStartup : class
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public CustomWebApplicationFactory(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // Register the xUnit logger
            builder.ConfigureLogging(loggingBuilder =>
            {
                loggingBuilder.Services.AddSingleton<ILoggerProvider>(serviceProvider => new XUnitLoggerProvider(_testOutputHelper));
            });
        }
    }

    public class UnitTestLoggerTest
    {
        private readonly CustomWebApplicationFactory<Startup> _factory;

        public UnitTestLoggerTest(ITestOutputHelper testOutputHelper)
        { 
            _factory = new CustomWebApplicationFactory<Startup>(testOutputHelper);
        }

        [Fact]
        public async Task ShouldLogHttpRequest()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("http://example.com");

            // Assert
            response.EnsureSuccessStatusCode();
        }
    }
}
