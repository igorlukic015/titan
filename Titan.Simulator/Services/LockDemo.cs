using Microsoft.Extensions.Logging.Abstractions;
using Titan.Core.Enums;
using Titan.Core.Models;
using Titan.Engine.services;

namespace Titan.Simulator.Services;

public static class LockDemo
{
    public static async Task RunAsync()
    {
        const int threadCount = 10;
        const int ordersPerThread = 1000;
        const decimal orderQty = 10m;
        const decimal price = 100m;

        Console.WriteLine("=== ORDERBOOK RACE CONDITION DEMO ===\n");

        await RunUnsafeDemo(threadCount, ordersPerThread, orderQty, price);
        Console.WriteLine();
        await RunSafeDemo(threadCount, ordersPerThread, orderQty, price);
    }

    private static async Task RunUnsafeDemo(int threadCount, int ordersPerThread, decimal orderQty, decimal price)
    {
        Console.WriteLine("--- UNSAFE (no locks) ---");

        UnsafeOrderBook book = new("TEST");
        decimal totalTraded = 0m;
        Lock tradeLock = new();
        int exceptionCount = 0;
        decimal inputQtyPerSide = threadCount * ordersPerThread * orderQty;

        Task[] tasks = new Task[threadCount * 2];

        for (int i = 0; i < threadCount; i++)
        {
            tasks[i * 2] = Task.Run(() =>
            {
                for (int j = 0; j < ordersPerThread; j++)
                {
                    try
                    {
                        Order order = new()
                        {
                            Id = Guid.NewGuid(),
                            Symbol = "TEST",
                            Type = OrderType.Limit,
                            Side = OrderSide.Buy,
                            Price = price,
                            Quantity = orderQty,
                            RemainingQuantity = orderQty,
                            Status = OrderStatus.Pending,
                            Timestamp = DateTime.UtcNow
                        };
                        IReadOnlyList<Trade> trades = book.ProcessOrder(order);
                        lock (tradeLock)
                        {
                            totalTraded += trades.Sum(t => t.Quantity);
                        }
                    }
                    catch
                    {
                        Interlocked.Increment(ref exceptionCount);
                    }
                }
            });

            tasks[i * 2 + 1] = Task.Run(() =>
            {
                for (int j = 0; j < ordersPerThread; j++)
                {
                    try
                    {
                        Order order = new()
                        {
                            Id = Guid.NewGuid(),
                            Symbol = "TEST",
                            Type = OrderType.Limit,
                            Side = OrderSide.Sell,
                            Price = price,
                            Quantity = orderQty,
                            RemainingQuantity = orderQty,
                            Status = OrderStatus.Pending,
                            Timestamp = DateTime.UtcNow
                        };
                        IReadOnlyList<Trade> trades = book.ProcessOrder(order);
                        lock (tradeLock)
                        {
                            totalTraded += trades.Sum(t => t.Quantity);
                        }
                    }
                    catch
                    {
                        Interlocked.Increment(ref exceptionCount);
                    }
                }
            });
        }

        await Task.WhenAll(tasks);

        try
        {
            decimal bidsRemaining = book.GetTotalBidQty();
            decimal asksRemaining = book.GetTotalAskQty();
            decimal buyAccountedFor = totalTraded + bidsRemaining;
            decimal sellAccountedFor = totalTraded + asksRemaining;

            Console.WriteLine($"Buy input qty:       {inputQtyPerSide:N0}");
            Console.WriteLine($"Sell input qty:      {inputQtyPerSide:N0}");
            Console.WriteLine($"Total traded:        {totalTraded:N0}");
            Console.WriteLine($"Bids remaining:      {bidsRemaining:N0}");
            Console.WriteLine($"Asks remaining:      {asksRemaining:N0}");
            Console.WriteLine($"Exceptions caught:   {exceptionCount}");

            bool buyInvariant = buyAccountedFor == inputQtyPerSide;
            bool sellInvariant = sellAccountedFor == inputQtyPerSide;

            if (!buyInvariant || !sellInvariant || exceptionCount > 0)
            {
                Console.WriteLine("INVARIANT VIOLATED - race conditions detected!");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Input qty per side:  {inputQtyPerSide:N0}");
            Console.WriteLine($"Exceptions caught:   {exceptionCount}");
            Console.WriteLine($"CRASHED reading book: {ex.GetType().Name}");
            Console.WriteLine("INVARIANT VIOLATED - race conditions corrupted data!");
        }
    }

    private static async Task RunSafeDemo(int threadCount, int ordersPerThread, decimal orderQty, decimal price)
    {
        Console.WriteLine("--- SAFE (with locks) ---");

        OrderBook book = new("TEST", NullLogger<OrderBook>.Instance);
        decimal totalTraded = 0m;
        Lock tradeLock = new();
        int exceptionCount = 0;
        decimal inputQtyPerSide = threadCount * ordersPerThread * orderQty;

        Task[] tasks = new Task[threadCount * 2];

        for (int i = 0; i < threadCount; i++)
        {
            tasks[i * 2] = Task.Run(() =>
            {
                for (int j = 0; j < ordersPerThread; j++)
                {
                    try
                    {
                        Order order = new()
                        {
                            Id = Guid.NewGuid(),
                            Symbol = "TEST",
                            Type = OrderType.Limit,
                            Side = OrderSide.Buy,
                            Price = price,
                            Quantity = orderQty,
                            RemainingQuantity = orderQty,
                            Status = OrderStatus.Pending,
                            Timestamp = DateTime.UtcNow
                        };
                        IReadOnlyList<Trade> trades = book.ProcessOrder(order);
                        lock (tradeLock)
                        {
                            totalTraded += trades.Sum(t => t.Quantity);
                        }
                    }
                    catch
                    {
                        Interlocked.Increment(ref exceptionCount);
                    }
                }
            });

            tasks[i * 2 + 1] = Task.Run(() =>
            {
                for (int j = 0; j < ordersPerThread; j++)
                {
                    try
                    {
                        Order order = new()
                        {
                            Id = Guid.NewGuid(),
                            Symbol = "TEST",
                            Type = OrderType.Limit,
                            Side = OrderSide.Sell,
                            Price = price,
                            Quantity = orderQty,
                            RemainingQuantity = orderQty,
                            Status = OrderStatus.Pending,
                            Timestamp = DateTime.UtcNow
                        };
                        IReadOnlyList<Trade> trades = book.ProcessOrder(order);
                        lock (tradeLock)
                        {
                            totalTraded += trades.Sum(t => t.Quantity);
                        }
                    }
                    catch
                    {
                        Interlocked.Increment(ref exceptionCount);
                    }
                }
            });
        }

        await Task.WhenAll(tasks);

        decimal bidsRemaining = book.GetBids().Sum(o => o.RemainingQuantity);
        decimal asksRemaining = book.GetAsks().Sum(o => o.RemainingQuantity);
        decimal buyAccountedFor = totalTraded + bidsRemaining;
        decimal sellAccountedFor = totalTraded + asksRemaining;

        Console.WriteLine($"Buy input qty:       {inputQtyPerSide:N0}");
        Console.WriteLine($"Sell input qty:      {inputQtyPerSide:N0}");
        Console.WriteLine($"Total traded:        {totalTraded:N0}");
        Console.WriteLine($"Bids remaining:      {bidsRemaining:N0}");
        Console.WriteLine($"Asks remaining:      {asksRemaining:N0}");
        Console.WriteLine($"Exceptions caught:   {exceptionCount}");

        bool buyInvariant = buyAccountedFor == inputQtyPerSide;
        bool sellInvariant = sellAccountedFor == inputQtyPerSide;

        if (buyInvariant && sellInvariant && exceptionCount == 0)
        {
            Console.WriteLine("INVARIANT HOLDS - locking works correctly!");
        }
    }
}
