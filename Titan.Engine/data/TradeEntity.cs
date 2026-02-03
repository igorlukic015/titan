using Titan.Core.Enums;

namespace Titan.Engine.Data;

public class TradeEntity
{
    public Guid Id { get; set; }
    public Guid BuyOrderId { get; set; }
    public Guid SellOrderId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal Quantity { get; set; }
    public DateTime Timestamp { get; set; }
    public TradeType Type { get; set; }
}
