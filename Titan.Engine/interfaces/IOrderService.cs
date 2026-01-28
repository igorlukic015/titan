using Titan.Core.models;
using Titan.Core.Models;

namespace Titan.Engine.interfaces;

public interface IOrderService
{
    Result<Order> CreateOrder(string symbol, decimal price, decimal quantity, string type, string side);

    Result<IReadOnlyList<Trade>> CreateTrade(Order order);
}
