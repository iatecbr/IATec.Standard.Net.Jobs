using AntiCorruption.Configurations;
using Api.Configurations;
using Application.Configurations;
using MessageQueue.Configurations;
using Persistence.Configurations;

var builder = WebApplication.CreateBuilder(args);

// Configure Projects References Services
builder.Services
    .ConfigureApi(builder.Configuration, builder.Environment)
    .ConfigureApplication()
    .ConfigureAntiCorruption()
    .ConfigureMessageQueue()
    .ConfigurePersistence(builder.Configuration);

var app = builder.Build();

// Configure Projects References App
app.UseApi()
    .Run();