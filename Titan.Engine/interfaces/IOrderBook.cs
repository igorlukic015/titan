using Titan.Core.Models;

namespace Titan.Engine.Interfaces;

public interface IOrderBook
{
    IReadOnlyList<Trade> ProcessOrder(Order order);
    IReadOnlyList<Order> GetBids();
    IReadOnlyList<Order> GetAsks();
    string Symbol { get; }
}
