using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Titan.Engine.Interfaces;
using Titan.Engine.Services;

namespace Titan.Gateway.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            ServiceDescriptor? descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IOrderBook));

            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            services.AddSingleton<IOrderBook>(new OrderBook("BTC/USD"));
        });
    }
}
