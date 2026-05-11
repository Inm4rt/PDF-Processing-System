using Microsoft.EntityFrameworkCore;
using PdfApi.Data;
using PdfApi.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);


// Настройка Serilog для .NET 8 (новый синтаксис)
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("Logs/api-log-.txt", rollingInterval: RollingInterval.Day));

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
// Подключение PostgreSQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// RabbitMQ Publisher как синглтон
builder.Services.AddSingleton<IRabbitMqPublisher, RabbitMqPublisher>();

var app = builder.Build();

// Автоматическая миграция БД (только для ApiGateway)
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.UseSwagger();
app.UseSwaggerUI();

try
{
    Log.Information("API Gateway запущен");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Приложение завершилось с ошибкой");
}
finally
{
    Log.CloseAndFlush();
}
