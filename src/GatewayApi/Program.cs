using GatewayApi.Services;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi.Models;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Configure HTTP clients for downstream services
builder.Services.AddHttpClient("CatalogService", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:CatalogService"] ?? "http://catalog-service:80");
});

builder.Services.AddHttpClient("CartService", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:CartService"] ?? "http://cart-service:80");
});

builder.Services.AddHttpClient("CheckoutService", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:CheckoutService"] ?? "http://checkout-service:80");
});

builder.Services.AddHttpClient("UserService", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:UserService"] ?? "http://user-service:80");
});

// Register gateway service
builder.Services.AddScoped<IGatewayService, GatewayService>();

// Configure Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "Ice Cream API Gateway", 
        Version = "v1",
        Description = "Unified API gateway for Ice Cream E-Commerce platform"
    });

    // Add JWT authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new List<string>()
        }
    });
});

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

// Configure Rate Limiting
builder.Services.AddRateLimiter(rateLimiterOptions =>
{
    // Set default rejection status code
    rateLimiterOptions.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    
    // Global OnRejected handler
    rateLimiterOptions.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "application/json";
        
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter = 
                ((int)retryAfter.TotalSeconds).ToString();
        }
        
        await context.HttpContext.Response.WriteAsync(
            "{\"error\": \"Rate limit exceeded. Please try again later.\"}", token);
    };
    
    // Add policy directly to rateLimiterOptions
    rateLimiterOptions.AddFixedWindowLimiter("fixed", options =>
    {
        options.PermitLimit = 100;
        options.Window = TimeSpan.FromMinutes(1);
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        options.QueueLimit = 10;
    });
    
    // Add another policy for stricter limits
    rateLimiterOptions.AddFixedWindowLimiter("strict", options =>
    {
        options.PermitLimit = 30;
        options.Window = TimeSpan.FromMinutes(1);
        options.QueueLimit = 5;
    });
    
    // Possible: Addding a sliding window policy
    rateLimiterOptions.AddSlidingWindowLimiter("sliding", options =>
    {
        options.PermitLimit = 1000;
        options.Window = TimeSpan.FromHours(1);
        options.SegmentsPerWindow = 6;
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        options.QueueLimit = 50;
    });
});

// Add health checks
builder.Services.AddHealthChecks();

// Add YARP reverse proxy
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseRateLimiter(); 
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

// Map reverse proxy routes
app.MapReverseProxy();

app.Run();