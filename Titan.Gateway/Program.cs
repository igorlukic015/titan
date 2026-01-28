using System.Text.Json.Serialization;
using Titan.Engine.interfaces;
using Titan.Engine.Interfaces;
using Titan.Engine.services;
using Titan.Gateway.Endpoints;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IOrderBook>(new OrderBook("BTC/USD"));
builder.Services.AddScoped<IOrderService, OrderService>();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

WebApplication app = builder.Build();

app.MapPost("/orders", OrderEndpoints.SubmitOrder);

app.Run();

public partial class Program { }
