using ReddisRabbitAPI;
using ReddisRabbitAPI.Services;
using ReddisRabbitGameLogic.Services;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var redisConnStr = builder.Configuration["Redis:ConnectionString"];
builder.Services.AddSingleton(new RedisConnection(redisConnStr));
builder.Services.AddHostedService<GameLogicService>();
builder.Services.AddHostedService<RedisPubSubListener>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .WithOrigins("http://localhost:3000", "http://127.0.0.1:5001", "http://127.0.0.1:5500") // your frontend
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddSignalR();

var app = builder.Build();

// Enable serving static files from wwwroot
app.UseDefaultFiles();  // automatically serve index.html
app.UseStaticFiles();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors();

app.UseAuthorization();

app.MapControllers();
app.MapHub<GameHub>("/gamehub");


app.Run();
