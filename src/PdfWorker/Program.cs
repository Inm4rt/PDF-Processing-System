using Microsoft.EntityFrameworkCore;
using PdfWorker.Data;
using PdfWorker.Services;
using PdfWorker.Workers;
using Serilog;

var builder = WebApplication.CreateBuilder(args);


Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Logging.ClearProviders();
builder.Logging.AddSerilog(Log.Logger);


builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));


builder.Services.AddSingleton<IPdfTextExtractor, PdfTextExtractor>();
builder.Services.AddHostedService<RabbitMqWorker>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

try
{
    Log.Information("Background Worker запущен");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Worker завершился с ошибкой");
}
finally
{
    Log.CloseAndFlush();
}
