using App.Configurations;
using App.Configurations.Extensions;
using Application.Configurations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

var configurationBuilder = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", false, true);

var configuration = configurationBuilder.Build();

// Configure Projects References Services
builder.Services
    .ConfigureConsoleApp(configuration)
    .ConfigureApplication(configuration);

using var host = builder.Build();
await host.StartAsync();

host.UseDispatchers();

await host.WaitForShutdownAsync();