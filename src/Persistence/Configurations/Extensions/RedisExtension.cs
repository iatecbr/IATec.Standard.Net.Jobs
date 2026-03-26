using Domain.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Persistence.Configurations.Extensions;

public static class RedisExtension
{
    public static IServiceCollection AddRedis(this IServiceCollection services, IConfiguration configuration)
    {
        var redisOption = new RedisOption();
        configuration.GetSection(RedisOption.Key).Bind(redisOption);

        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisOption.ConnectionString));

        return services;
    }
}