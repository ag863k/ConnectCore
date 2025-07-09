using ConnectCore.UserService.Data;
using ConnectCore.UserService.Services;
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
    .WriteTo.File("logs/user-service-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "ConnectCore User Service API", Version = "v1" });
});

builder.Services.AddDbContext<UserDbContext>(options =>
    options.UseInMemoryDatabase("UserDb"));

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<CreateUserDtoValidator>();

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddHttpClient<IServiceDiscoveryClient, InMemoryServiceDiscoveryClient>();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<UserDbContext>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "ConnectCore User Service API v1"));
}

app.UseErrorHandling();
app.UseRequestLogging();

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

var serviceDiscovery = app.Services.GetRequiredService<IServiceDiscoveryClient>();
var configuration = app.Services.GetRequiredService<IConfiguration>();

var serviceName = "user-service";
var serviceId = $"{serviceName}-{Environment.MachineName}-{Guid.NewGuid():N}";
var host = "localhost";
var port = 5002;

_ = Task.Run(async () =>
{
    await Task.Delay(2000);
    await serviceDiscovery.RegisterServiceAsync(serviceName, serviceId, host, port, new List<string> { "users", "api" });
});

Log.Information("ConnectCore User Service starting up...");

try
{
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "ConnectCore User Service terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
