namespace Titan.Simulator.Services;

public class TelemetryCollector
{
    private long totalRequests;
    private long successfulRequests;
    private long failedRequests;
    private long totalLatencyTicks;
    private long latencySampleCount;

    public void RecordSuccess(TimeSpan latency)
    {
        Interlocked.Increment(ref totalRequests);
        Interlocked.Increment(ref successfulRequests);
        Interlocked.Add(ref totalLatencyTicks, latency.Ticks);
        Interlocked.Increment(ref latencySampleCount);
    }

    public void RecordFailure()
    {
        Interlocked.Increment(ref totalRequests);
        Interlocked.Increment(ref failedRequests);
    }

    public TelemetrySnapshot GetSnapshotAndReset()
    {
        long snapshotTotalRequests = Interlocked.Exchange(ref totalRequests, 0);
        long snapshotSuccessfulRequests = Interlocked.Exchange(ref successfulRequests, 0);
        long snapshotFailedRequests = Interlocked.Exchange(ref failedRequests, 0);
        long snapshotTotalLatencyTicks = Interlocked.Exchange(ref totalLatencyTicks, 0);
        long snapshotLatencySampleCount = Interlocked.Exchange(ref latencySampleCount, 0);

        double averageLatencyMs = snapshotLatencySampleCount > 0
            ? TimeSpan.FromTicks(snapshotTotalLatencyTicks).TotalMilliseconds / snapshotLatencySampleCount
            : 0.0;

        return new TelemetrySnapshot(
            snapshotTotalRequests,
            snapshotSuccessfulRequests,
            snapshotFailedRequests,
            averageLatencyMs
        );
    }
}

public record TelemetrySnapshot(
    long TotalRequests,
    long SuccessfulRequests,
    long FailedRequests,
    double AverageLatencyMs
);
