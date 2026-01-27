using Titan.Simulator.Configuration;
using Titan.Simulator.Services;

(string gatewayUrl, int requestsPerSecond) = SimulatorConfig.ParseArguments(args);

TelemetryCollector telemetryCollector = new TelemetryCollector();
LoadGenerator loadGenerator = new LoadGenerator(gatewayUrl, requestsPerSecond, telemetryCollector);

CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

Console.CancelKeyPress += (sender, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellationTokenSource.Cancel();
};

Task loadTask = Task.Run(async () =>
{
    try
    {
        await loadGenerator.RunAsync(cancellationTokenSource.Token);
    }
    catch (OperationCanceledException)
    {
    }
}, cancellationTokenSource.Token);

Task reportingTask = Task.Run(async () =>
{
    try
    {
        while (!cancellationTokenSource.Token.IsCancellationRequested)
        {
            await Task.Delay(
                TimeSpan.FromSeconds(SimulatorConfig.ReportingIntervalSeconds),
                cancellationTokenSource.Token
            );

            TelemetrySnapshot snapshot = telemetryCollector.GetSnapshotAndReset();

            Console.Clear();
            Console.WriteLine("=== Titan Load Simulator ===");
            Console.WriteLine($"Target: {gatewayUrl}");
            Console.WriteLine($"Intensity: {requestsPerSecond} RPS");
            Console.WriteLine();

            double throughput = snapshot.SuccessfulRequests / (double)SimulatorConfig.ReportingIntervalSeconds;
            double errorRate = snapshot.TotalRequests > 0
                ? (snapshot.FailedRequests / (double)snapshot.TotalRequests) * 100.0
                : 0.0;

            Console.WriteLine($"Throughput:   {throughput:F2} successful orders/sec");
            Console.WriteLine($"Avg Latency:  {snapshot.AverageLatencyMs:F2} ms");
            Console.WriteLine($"Error Rate:   {errorRate:F2}%");
            Console.WriteLine($"Total Errors: {snapshot.FailedRequests}");
            Console.WriteLine();
            Console.WriteLine("Press Ctrl+C to stop");
        }
    }
    catch (OperationCanceledException)
    {
    }
}, cancellationTokenSource.Token);

await Task.WhenAll(loadTask, reportingTask);

Console.WriteLine();
Console.WriteLine("Simulator stopped gracefully.");
