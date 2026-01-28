using Titan.Simulator.DTOs;

namespace Titan.Simulator.Services;

public static class OrderGenerator
{
    private const string Symbol = "BTC/USD";
    private const decimal BasePrice = 150.00m;
    private const decimal PriceVariancePercent = 0.05m;
    private const decimal MinQuantity = 1m;
    private const decimal MaxQuantity = 500m;

    private static readonly ThreadLocal<Random> RandomInstance = new(() => new Random());

    public static SubmitOrderRequest GenerateOrder()
    {
        Random random = RandomInstance.Value!;

        decimal priceVariance = BasePrice * PriceVariancePercent;
        decimal minPrice = BasePrice - priceVariance;
        decimal maxPrice = BasePrice + priceVariance;
        decimal price = Math.Round(minPrice + ((decimal)random.NextDouble() * (maxPrice - minPrice)), 2);

        decimal quantity = Math.Round(MinQuantity + ((decimal)random.NextDouble() * (MaxQuantity - MinQuantity)), 2);

        string type = random.Next(2) == 0 ? "Limit" : "Market";
        string side = random.Next(2) == 0 ? "Buy" : "Sell";

        return new SubmitOrderRequest
        {
            Symbol = Symbol,
            Price = price,
            Quantity = quantity,
            Type = type,
            Side = side
        };
    }
}
