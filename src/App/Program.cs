using AntiCorruption.Configurations;
using App.Configurations;
using Application.Configurations;
using MessageQueue.Configurations;
using Persistence.Configurations;

var builder = WebApplication.CreateBuilder(args);

// Configure Projects References Services
builder.Services
    .ConfigureApp(builder.Configuration, builder.Environment)
    .ConfigureApplication()
    .ConfigureAntiCorruption()
    .ConfigureMessageQueue(builder.Configuration)
    .ConfigurePersistence(builder.Configuration);

var app = builder.Build();

// Configure Projects References App
app.UseApp()
    .Run();