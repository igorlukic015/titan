using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Titan.Gateway.DTOs;
using Xunit;

namespace Titan.Gateway.Tests.Endpoints;

public class OrderEndpointsTests : IDisposable
{
    private readonly CustomWebApplicationFactory factory;
    private readonly HttpClient client;
    private readonly JsonSerializerOptions jsonOptions;

    public OrderEndpointsTests()
    {
        factory = new CustomWebApplicationFactory();
        client = factory.CreateClient();
        jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public void Dispose()
    {
        client.Dispose();
        factory.Dispose();
    }

    [Fact]
    public async Task SubmitOrder_WithValidLimitBuyOrder_ReturnsOkWithPendingStatus()
    {
        SubmitOrderRequest request = new()
        {
            Symbol = "BTC/USD",
            Price = 10000m,
            Quantity = 1.0m,
            Type = "Limit",
            Side = "Buy"
        };

        HttpResponseMessage response = await client.PostAsJsonAsync("/orders", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        SubmitOrderResponse? result = await response.Content.ReadFromJsonAsync<SubmitOrderResponse>(jsonOptions);
        Assert.NotNull(result);
        Assert.Equal("BTC/USD", result.Symbol);
        Assert.Equal("Pending", result.Status);
        Assert.Equal(1.0m, result.RemainingQuantity);
        Assert.Empty(result.Trades);
        Assert.NotEqual(Guid.Empty, result.OrderId);
    }

    [Fact]
    public async Task SubmitOrder_WithValidLimitSellOrder_ReturnsOkWithPendingStatus()
    {
        SubmitOrderRequest request = new()
        {
            Symbol = "BTC/USD",
            Price = 90000m,
            Quantity = 0.5m,
            Type = "Limit",
            Side = "Sell"
        };

        HttpResponseMessage response = await client.PostAsJsonAsync("/orders", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        SubmitOrderResponse? result = await response.Content.ReadFromJsonAsync<SubmitOrderResponse>(jsonOptions);
        Assert.NotNull(result);
        Assert.Equal("BTC/USD", result.Symbol);
        Assert.Equal("Pending", result.Status);
        Assert.Equal(0.5m, result.RemainingQuantity);
        Assert.Empty(result.Trades);
    }

    [Fact]
    public async Task SubmitOrder_WithMatchingBuyAndSellOrders_ReturnsFilledStatusWithTrade()
    {
        SubmitOrderRequest sellRequest = new()
        {
            Symbol = "BTC/USD",
            Price = 60000m,
            Quantity = 0.75m,
            Type = "Limit",
            Side = "Sell"
        };

        await client.PostAsJsonAsync("/orders", sellRequest);

        SubmitOrderRequest buyRequest = new()
        {
            Symbol = "BTC/USD",
            Price = 60000m,
            Quantity = 0.75m,
            Type = "Limit",
            Side = "Buy"
        };

        HttpResponseMessage response = await client.PostAsJsonAsync("/orders", buyRequest);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        SubmitOrderResponse? result = await response.Content.ReadFromJsonAsync<SubmitOrderResponse>(jsonOptions);
        Assert.NotNull(result);
        Assert.Equal("Filled", result.Status);
        Assert.Equal(0.0m, result.RemainingQuantity);
        Assert.Single(result.Trades);
        Assert.Equal(0.75m, result.Trades[0].Quantity);
        Assert.Equal(60000m, result.Trades[0].Price);
        Assert.Equal("BTC/USD", result.Trades[0].Symbol);
        Assert.Equal("MakerSell", result.Trades[0].Type);
    }

    [Fact]
    public async Task SubmitOrder_WithPartiallyMatchingOrders_ReturnsPartiallyFilledStatus()
    {
        SubmitOrderRequest sellRequest = new()
        {
            Symbol = "BTC/USD",
            Price = 61000m,
            Quantity = 0.5m,
            Type = "Limit",
            Side = "Sell"
        };

        await client.PostAsJsonAsync("/orders", sellRequest);

        SubmitOrderRequest buyRequest = new()
        {
            Symbol = "BTC/USD",
            Price = 61000m,
            Quantity = 2.0m,
            Type = "Limit",
            Side = "Buy"
        };

        HttpResponseMessage response = await client.PostAsJsonAsync("/orders", buyRequest);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        SubmitOrderResponse? result = await response.Content.ReadFromJsonAsync<SubmitOrderResponse>(jsonOptions);
        Assert.NotNull(result);
        Assert.Equal("PartiallyFilled", result.Status);
        Assert.Equal(1.5m, result.RemainingQuantity);
        Assert.Single(result.Trades);
        Assert.Equal(0.5m, result.Trades[0].Quantity);
    }

    [Theory]
    [InlineData(0, 1.0, "Price must be greater than 0")]
    [InlineData(-100, 1.0, "Price must be greater than 0")]
    public async Task SubmitOrder_WithInvalidPrice_ReturnsBadRequestWithErrorMessage(decimal price, decimal quantity, string expectedError)
    {
        SubmitOrderRequest request = new()
        {
            Symbol = "BTC/USD",
            Price = price,
            Quantity = quantity,
            Type = "Limit",
            Side = "Buy"
        };

        HttpResponseMessage response = await client.PostAsJsonAsync("/orders", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        ErrorResponse? error = await response.Content.ReadFromJsonAsync<ErrorResponse>(jsonOptions);
        Assert.NotNull(error);
        Assert.Equal(expectedError, error.Error);
    }

    [Theory]
    [InlineData(50000, 0, "Quantity must be greater than 0")]
    [InlineData(50000, -1.5, "Quantity must be greater than 0")]
    public async Task SubmitOrder_WithInvalidQuantity_ReturnsBadRequestWithErrorMessage(decimal price, decimal quantity, string expectedError)
    {
        SubmitOrderRequest request = new()
        {
            Symbol = "BTC/USD",
            Price = price,
            Quantity = quantity,
            Type = "Limit",
            Side = "Buy"
        };

        HttpResponseMessage response = await client.PostAsJsonAsync("/orders", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        ErrorResponse? error = await response.Content.ReadFromJsonAsync<ErrorResponse>(jsonOptions);
        Assert.NotNull(error);
        Assert.Equal(expectedError, error.Error);
    }

    [Theory]
    [InlineData("", "Symbol is required")]
    [InlineData("   ", "Symbol is required")]
    public async Task SubmitOrder_WithEmptyOrWhitespaceSymbol_ReturnsBadRequestWithErrorMessage(string symbol, string expectedError)
    {
        SubmitOrderRequest request = new()
        {
            Symbol = symbol,
            Price = 50000m,
            Quantity = 1.0m,
            Type = "Limit",
            Side = "Buy"
        };

        HttpResponseMessage response = await client.PostAsJsonAsync("/orders", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        ErrorResponse? error = await response.Content.ReadFromJsonAsync<ErrorResponse>(jsonOptions);
        Assert.NotNull(error);
        Assert.Equal(expectedError, error.Error);
    }

    [Theory]
    [InlineData("InvalidType")]
    [InlineData("Stop")]
    [InlineData("")]
    public async Task SubmitOrder_WithInvalidOrderType_ReturnsBadRequestWithErrorMessage(string orderType)
    {
        SubmitOrderRequest request = new()
        {
            Symbol = "BTC/USD",
            Price = 50000m,
            Quantity = 1.0m,
            Type = orderType,
            Side = "Buy"
        };

        HttpResponseMessage response = await client.PostAsJsonAsync("/orders", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        ErrorResponse? error = await response.Content.ReadFromJsonAsync<ErrorResponse>(jsonOptions);
        Assert.NotNull(error);
        Assert.Equal("Invalid order type. Must be 'Limit' or 'Market'", error.Error);
    }

    [Theory]
    [InlineData("InvalidSide")]
    [InlineData("Long")]
    [InlineData("")]
    public async Task SubmitOrder_WithInvalidOrderSide_ReturnsBadRequestWithErrorMessage(string orderSide)
    {
        SubmitOrderRequest request = new()
        {
            Symbol = "BTC/USD",
            Price = 50000m,
            Quantity = 1.0m,
            Type = "Limit",
            Side = orderSide
        };

        HttpResponseMessage response = await client.PostAsJsonAsync("/orders", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        ErrorResponse? error = await response.Content.ReadFromJsonAsync<ErrorResponse>(jsonOptions);
        Assert.NotNull(error);
        Assert.Equal("Invalid order side. Must be 'Buy' or 'Sell'", error.Error);
    }

    [Fact]
    public async Task SubmitOrder_WithMismatchedSymbol_ReturnsBadRequestWithErrorMessage()
    {
        SubmitOrderRequest request = new()
        {
            Symbol = "ETH/USD",
            Price = 3000m,
            Quantity = 1.0m,
            Type = "Limit",
            Side = "Buy"
        };

        HttpResponseMessage response = await client.PostAsJsonAsync("/orders", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        ErrorResponse? error = await response.Content.ReadFromJsonAsync<ErrorResponse>(jsonOptions);
        Assert.NotNull(error);
        Assert.Contains("symbol", error.Error.ToLower());
    }

    [Theory]
    [InlineData("limit")]
    [InlineData("LIMIT")]
    [InlineData("LiMiT")]
    public async Task SubmitOrder_WithCaseInsensitiveOrderType_ReturnsOk(string orderType)
    {
        SubmitOrderRequest request = new()
        {
            Symbol = "BTC/USD",
            Price = 50000m,
            Quantity = 1.0m,
            Type = orderType,
            Side = "Buy"
        };

        HttpResponseMessage response = await client.PostAsJsonAsync("/orders", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("buy")]
    [InlineData("BUY")]
    [InlineData("BuY")]
    public async Task SubmitOrder_WithCaseInsensitiveOrderSide_ReturnsOk(string orderSide)
    {
        SubmitOrderRequest request = new()
        {
            Symbol = "BTC/USD",
            Price = 50000m,
            Quantity = 1.0m,
            Type = "Limit",
            Side = orderSide
        };

        HttpResponseMessage response = await client.PostAsJsonAsync("/orders", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SubmitOrder_WithMarketOrderType_ReturnsOk()
    {
        SubmitOrderRequest request = new()
        {
            Symbol = "BTC/USD",
            Price = 50000m,
            Quantity = 1.0m,
            Type = "Market",
            Side = "Buy"
        };

        HttpResponseMessage response = await client.PostAsJsonAsync("/orders", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SubmitOrder_WithVerySmallQuantity_ReturnsOk()
    {
        SubmitOrderRequest request = new()
        {
            Symbol = "BTC/USD",
            Price = 50000m,
            Quantity = 0.00000001m,
            Type = "Limit",
            Side = "Buy"
        };

        HttpResponseMessage response = await client.PostAsJsonAsync("/orders", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        SubmitOrderResponse? result = await response.Content.ReadFromJsonAsync<SubmitOrderResponse>(jsonOptions);
        Assert.NotNull(result);
        Assert.Equal(0.00000001m, result.RemainingQuantity);
    }

    [Fact]
    public async Task SubmitOrder_WithVeryLargePrice_ReturnsOk()
    {
        SubmitOrderRequest request = new()
        {
            Symbol = "BTC/USD",
            Price = 999999999.99m,
            Quantity = 1.0m,
            Type = "Limit",
            Side = "Buy"
        };

        HttpResponseMessage response = await client.PostAsJsonAsync("/orders", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SubmitOrder_GeneratesUniqueOrderIds()
    {
        SubmitOrderRequest request = new()
        {
            Symbol = "BTC/USD",
            Price = 50000m,
            Quantity = 1.0m,
            Type = "Limit",
            Side = "Buy"
        };

        HttpResponseMessage response1 = await client.PostAsJsonAsync("/orders", request);
        HttpResponseMessage response2 = await client.PostAsJsonAsync("/orders", request);

        SubmitOrderResponse? result1 = await response1.Content.ReadFromJsonAsync<SubmitOrderResponse>(jsonOptions);
        SubmitOrderResponse? result2 = await response2.Content.ReadFromJsonAsync<SubmitOrderResponse>(jsonOptions);

        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.NotEqual(result1.OrderId, result2.OrderId);
    }
}
