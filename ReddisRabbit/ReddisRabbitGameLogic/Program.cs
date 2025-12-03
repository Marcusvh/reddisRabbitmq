using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ReddisRabbitGameLogic.Services;
using StackExchange.Redis;

Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        var redisConnStr = context.Configuration["Redis:ConnectionString"];
        services.AddSingleton(new RedisConnection(redisConnStr));

    })
    .Build()
    .Run();