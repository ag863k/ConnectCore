using ConnectCore.Shared.Services;
using ConnectCore.Shared.Middleware;
using ConnectCore.Shared.Extensions;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console()
    .WriteTo.File("logs/service-discovery-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "ConnectCore Service Discovery API", Version = "v1" });
});

builder.Services.AddSingleton<IServiceDiscoveryClient, InMemoryServiceDiscoveryClient>();

builder.Services.AddHealthChecks();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "ConnectCore Service Discovery API v1"));
}

app.UseErrorHandling();
app.UseRequestLogging();

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

Log.Information("ConnectCore Service Discovery starting up...");

try
{
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "ConnectCore Service Discovery terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
