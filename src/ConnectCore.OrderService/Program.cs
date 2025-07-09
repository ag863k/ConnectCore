using ConnectCore.OrderService.Data;
using ConnectCore.OrderService.Services;
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
    .WriteTo.File("logs/order-service-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "ConnectCore Order Service API", Version = "v1" });
});

builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseInMemoryDatabase("OrderDb"));

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<CreateOrderDtoValidator>();

builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddHttpClient<IServiceDiscoveryClient, InMemoryServiceDiscoveryClient>();
builder.Services.AddHttpClient();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<OrderDbContext>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "ConnectCore Order Service API v1"));
}

app.UseErrorHandling();
app.UseRequestLogging();

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

var serviceDiscovery = app.Services.GetRequiredService<IServiceDiscoveryClient>();

var serviceName = "order-service";
var serviceId = $"{serviceName}-{Environment.MachineName}-{Guid.NewGuid():N}";
var host = "localhost";
var port = 5004;

_ = Task.Run(async () =>
{
    await Task.Delay(2000);
    await serviceDiscovery.RegisterServiceAsync(serviceName, serviceId, host, port, new List<string> { "orders", "api" });
});

Log.Information("ConnectCore Order Service starting up...");

try
{
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "ConnectCore Order Service terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
