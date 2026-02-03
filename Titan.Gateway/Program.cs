using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Titan.Engine.Data;
using Titan.Engine.interfaces;
using Titan.Engine.Interfaces;
using Titan.Engine.services;
using Titan.Gateway.Endpoints;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

string connectionString = builder.Configuration.GetConnectionString("TradeDb")
    ?? throw new InvalidOperationException("TradeDb connection string not configured");

builder.Services.AddDbContext<TradeDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddSingleton<IOrderBook>(sp =>
    new OrderBook("BTC/USD", sp.GetRequiredService<ILoggerFactory>().CreateLogger<OrderBook>()));
builder.Services.AddScoped<IOrderService, OrderService>();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

WebApplication app = builder.Build();

using (IServiceScope scope = app.Services.CreateScope())
{
    TradeDbContext db = scope.ServiceProvider.GetRequiredService<TradeDbContext>();
    db.Database.EnsureCreated();
}

app.MapPost("/orders", OrderEndpoints.SubmitOrder);

app.Run();

public partial class Program { }
