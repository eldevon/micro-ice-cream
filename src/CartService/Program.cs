using CartService.Services;
using CartService.MessageBus;
using StackExchange.Redis;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Configure Redis
var redisConnectionString = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
builder.Services.AddSingleton<IConnectionMultiplexer>(sp => 
    ConnectionMultiplexer.Connect(redisConnectionString));

// Register Services
builder.Services.AddScoped<ICartService, CartService.Services.CartService>();
builder.Services.AddSingleton<IMessageBus, RabbitMQMessageBus>();
builder.Services.AddHttpClient();

// Configure Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "Ice Cream Cart Service", 
        Version = "v1",
        Description = "Manages shopping carts using Redis cache"
    });
});

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("GatewayOnly", builder =>
    {
        builder.WithOrigins("http://localhost:5000", "https://localhost:5009")
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

// Add health checks
builder.Services.AddHealthChecks()
    .AddRedis(redisConnectionString);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("GatewayOnly");
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();