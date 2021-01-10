using AutoMapper;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moodful.Configuration;

[assembly: FunctionsStartup(typeof(Moodful.Startup))]
namespace Moodful
{
    /// <summary>
    /// https://docs.microsoft.com/en-us/aspnet/core/fundamentals/startup?view=aspnetcore-5.0
    /// https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-5.0
    /// https://github.com/Azure/azure-functions-host/issues/4464#issuecomment-521869759
    /// </summary>
    public class Startup : FunctionsStartup
    {
        public Startup()
        {
        }

        public override void Configure(IFunctionsHostBuilder builder)
        {
            var context = builder.GetContext();
            var securityOptions = context.Configuration.GetSection(nameof(SecurityOptions));

            builder.Services
                .Configure<SecurityOptions>(securityOptions)
                .AddAutoMapper(typeof(MappingProfile));
        }

        public override void ConfigureAppConfiguration(IFunctionsConfigurationBuilder builder)
        {
            var context = builder.GetContext();

            builder.ConfigurationBuilder
                .SetBasePath(context.ApplicationRootPath)
                .AddJsonFile("appsettings.json")
                .AddJsonFile($"appsettings.{context.EnvironmentName}.json", optional: true)
                .AddJsonFile("local.settings.json", optional: true)
                .Build();
        }
    }
}
