using ConnectCore.Shared.Services;
using ConnectCore.Shared.Middleware;
using ConnectCore.Shared.Extensions;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console()
    .WriteTo.File("logs/gateway-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "ConnectCore API Gateway", Version = "v1" });
});

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddSingleton<IServiceDiscoveryClient, InMemoryServiceDiscoveryClient>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddHealthChecks();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "ConnectCore API Gateway v1"));
}

app.UseErrorHandling();
app.UseRequestLogging();

app.UseCors("AllowAll");
app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();
app.MapReverseProxy();
app.MapHealthChecks("/health");

Log.Information("ConnectCore API Gateway starting up...");

try
{
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "ConnectCore API Gateway terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
