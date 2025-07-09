using ConnectCore.ProductService.Data;
using ConnectCore.ProductService.Services;
using ConnectCore.Shared.Services;
using ConnectCore.Shared.Middleware;
using ConnectCore.Shared.Extensions;
using ConnectCore.Shared.Validators;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using FluentValidation.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console()
    .WriteTo.File("logs/product-service-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "ConnectCore Product Service API", Version = "v1" });
});

builder.Services.AddDbContext<ProductDbContext>(options =>
    options.UseInMemoryDatabase("ProductDb"));

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<CreateProductDtoValidator>();

builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddHttpClient<IServiceDiscoveryClient, InMemoryServiceDiscoveryClient>();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<ProductDbContext>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "ConnectCore Product Service API v1"));
}

app.UseErrorHandling();
app.UseRequestLogging();

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

var serviceDiscovery = app.Services.GetRequiredService<IServiceDiscoveryClient>();

var serviceName = "product-service";
var serviceId = $"{serviceName}-{Environment.MachineName}-{Guid.NewGuid():N}";
var host = "localhost";
var port = 5003;

_ = Task.Run(async () =>
{
    await Task.Delay(2000);
    await serviceDiscovery.RegisterServiceAsync(serviceName, serviceId, host, port, new List<string> { "products", "api" });
});

Log.Information("ConnectCore Product Service starting up...");

try
{
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "ConnectCore Product Service terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
