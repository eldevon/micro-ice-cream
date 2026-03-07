// using CatalogService.Data;
// using CatalogService.Services;
// using Microsoft.EntityFrameworkCore;
// using Microsoft.OpenApi.Models;
// using System.Text.Json.Serialization;

// var builder = WebApplication.CreateBuilder(args);

// // Add services to the container.
// builder.Services.AddControllers()
//     .AddJsonOptions(options =>
//     {
//         options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
//         options.JsonSerializerOptions.WriteIndented = true;
//     });

// // Configure Database
// builder.Services.AddDbContext<CatalogDbContext>(options =>
//     options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// // Register Services
// builder.Services.AddScoped<IProductService, ProductService>();
// builder.Services.AddScoped<ICategoryService, CategoryService>();

// // Configure AutoMapper
// builder.Services.AddAutoMapper(typeof(Program));

// // Configure Swagger
// builder.Services.AddEndpointsApiExplorer();
// builder.Services.AddSwaggerGen(c =>
// {
//     c.SwaggerDoc("v1", new OpenApiInfo 
//     { 
//         Title = "Catalog Service API", 
//         Version = "v1",
//         Description = "A product catalog management service",
//         Contact = new OpenApiContact
//         {
//             Name = "Your Name",
//             Email = "your.email@example.com"
//         }
//     });
// });

// // Configure CORS
// builder.Services.AddCors(options =>
// {
//     options.AddPolicy("AllowAll", builder =>
//     {
//         builder.AllowAnyOrigin()
//                .AllowAnyMethod()
//                .AllowAnyHeader();
//     });
// });

// // Add health checks
// builder.Services.AddHealthChecks()
//     .AddDbContextCheck<CatalogDbContext>();

// var app = builder.Build();

// // Configure the HTTP request pipeline.
// if (app.Environment.IsDevelopment())
// {
//     app.UseSwagger();
//     app.UseSwaggerUI();
// }
// else
// {
//     app.UseHsts();
// }

// app.UseHttpsRedirection();
// app.UseCors("AllowAll");
// app.UseAuthorization();
// app.MapControllers();
// app.MapHealthChecks("/health");

// // Ensure database is created and migrations are applied
// using (var scope = app.Services.CreateScope())
// {
//     var dbContext = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
//     dbContext.Database.Migrate();
// }

// app.Run();


using CatalogService.Data;
using CatalogService.Services;
using CatalogService.MessageBus;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.WriteIndented = true;
    });

// Configure Database
builder.Services.AddDbContext<CatalogDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register Services
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddSingleton<IMessageBus, RabbitMQMessageBus>();

// Configure AutoMapper
builder.Services.AddAutoMapper(typeof(Program));

// Configure Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "Ice Cream Catalog Service", 
        Version = "v1",
        Description = "Manages ice cream products, flavors, and inventory",
        Contact = new OpenApiContact
        {
            Name = "Support Team",
            Email = "support@icecream.com"
        }
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
    .AddDbContextCheck<CatalogDbContext>();

// Add HTTP client for service communication
builder.Services.AddHttpClient();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseCors("GatewayOnly");
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

// Ensure database is created and migrations are applied
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
    await dbContext.Database.MigrateAsync();
}

app.Run();
