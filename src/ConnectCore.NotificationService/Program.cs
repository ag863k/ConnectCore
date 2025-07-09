using ConnectCore.NotificationService.Data;
using ConnectCore.NotificationService.Services;
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
    .WriteTo.File("logs/notification-service-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "ConnectCore Notification Service API", Version = "v1" });
});

builder.Services.AddDbContext<NotificationDbContext>(options =>
    options.UseInMemoryDatabase("NotificationDb"));

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<SendNotificationDtoValidator>();

builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IEmailService, MockEmailService>();
builder.Services.AddScoped<ISmsService, MockSmsService>();
builder.Services.AddScoped<IPushNotificationService, MockPushNotificationService>();
builder.Services.AddHttpClient<IServiceDiscoveryClient, InMemoryServiceDiscoveryClient>();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<NotificationDbContext>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "ConnectCore Notification Service API v1"));
}

app.UseErrorHandling();
app.UseRequestLogging();

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

var serviceDiscovery = app.Services.GetRequiredService<IServiceDiscoveryClient>();

var serviceName = "notification-service";
var serviceId = $"{serviceName}-{Environment.MachineName}-{Guid.NewGuid():N}";
var host = "localhost";
var port = 5005;

_ = Task.Run(async () =>
{
    await Task.Delay(2000);
    await serviceDiscovery.RegisterServiceAsync(serviceName, serviceId, host, port, new List<string> { "notifications", "api" });
});

Log.Information("ConnectCore Notification Service starting up...");

try
{
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "ConnectCore Notification Service terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
