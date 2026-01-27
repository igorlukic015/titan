namespace Titan.Simulator.Configuration;

public static class SimulatorConfig
{
    public const string DefaultGatewayUrl = "http://localhost:5000";
    public const int DefaultRequestsPerSecond = 1000;
    public const int ReportingIntervalSeconds = 2;

    public static (string gatewayUrl, int requestsPerSecond) ParseArguments(string[] args)
    {
        string gatewayUrl = args.Length > 0 ? args[0] : DefaultGatewayUrl;
        int requestsPerSecond = args.Length > 1 && int.TryParse(args[1], out int rps)
            ? rps
            : DefaultRequestsPerSecond;

        return (gatewayUrl, requestsPerSecond);
    }
}
