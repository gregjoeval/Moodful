using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

[assembly: FunctionsStartup(typeof(Moodful.Startup))]
namespace Moodful
{
  /// <summary>
  /// Reference: https://www.c-sharpcorner.com/article/how-to-ilogger-reference-in-startup-in-azure-function/
  /// </summary>
  public class Startup : FunctionsStartup
  {
    private ILoggerFactory _loggerFactory;
    public override void Configure(IFunctionsHostBuilder builder)
    {
      var config = new ConfigurationBuilder().AddJsonFile("local.settings.json", optional: true, reloadOnChange: true).AddEnvironmentVariables().Build();
      builder.Services.AddLogging();
      ConfigureServices(builder);
    }
    public void ConfigureServices(IFunctionsHostBuilder builder)
    {
      _loggerFactory = new LoggerFactory();
      var logger = _loggerFactory.CreateLogger("Startup");
      logger.LogInformation("Got Here in Startup");
      //Do something with builder    
    }
  }
}