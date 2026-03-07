using CheckoutService.Data;
using CheckoutService.Services;
using CheckoutService.MessageBus;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Configure Database
builder.Services.AddDbContext<CheckoutDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register Services
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddSingleton<IMessageBus, RabbitMQMessageBus>();

// Configure HTTP clients for service communication
builder.Services.AddHttpClient("CartService", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:CartService"] ?? "http://localhost:5003");
});

builder.Services.AddHttpClient("UserService", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:UserService"] ?? "http://localhost:5007");
});

// Configure Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "Ice Cream Checkout Service", 
        Version = "v1",
        Description = "Manages orders, payments, and checkout process"
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
    .AddDbContextCheck<CheckoutDbContext>();

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

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<CheckoutDbContext>();
    dbContext.Database.Migrate();
}

app.Run();