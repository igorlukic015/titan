using System.Diagnostics;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using Titan.Simulator.DTOs;

namespace Titan.Simulator.Services;

public class LoadGenerator
{
    private readonly HttpClient httpClient;
    private readonly string gatewayUrl;
    private readonly int requestsPerSecond;
    private readonly TelemetryCollector telemetryCollector;

    public LoadGenerator(string gatewayUrl, int requestsPerSecond, TelemetryCollector telemetryCollector)
    {
        this.gatewayUrl = gatewayUrl;
        this.requestsPerSecond = requestsPerSecond;
        this.telemetryCollector = telemetryCollector;

        int maxConcurrency = requestsPerSecond * 2;

        SocketsHttpHandler handler = new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(1),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
            MaxConnectionsPerServer = maxConcurrency
        };

        httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(5)
        };
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        ParallelOptions options = new ParallelOptions
        {
            MaxDegreeOfParallelism = requestsPerSecond * 2,
            CancellationToken = cancellationToken
        };

        await Parallel.ForEachAsync(
            GenerateOrders(cancellationToken),
            options,
            async (order, ct) => await SendOrderAsync(order, ct)
        );
    }

    private async IAsyncEnumerable<SubmitOrderRequest> GenerateOrders([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            yield return OrderGenerator.GenerateOrder();
            await Task.Delay(TimeSpan.FromMilliseconds(1000.0 / requestsPerSecond), cancellationToken);
        }
    }

    private async Task SendOrderAsync(SubmitOrderRequest order, CancellationToken cancellationToken)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();

        try
        {
            HttpResponseMessage response = await httpClient.PostAsJsonAsync(
                $"{gatewayUrl}/orders",
                order,
                cancellationToken
            );

            stopwatch.Stop();

            if (response.IsSuccessStatusCode)
            {
                telemetryCollector.RecordSuccess(stopwatch.Elapsed);
            }
            else
            {
                telemetryCollector.RecordFailure();
            }
        }
        catch
        {
            stopwatch.Stop();
            telemetryCollector.RecordFailure();
        }
    }
}
