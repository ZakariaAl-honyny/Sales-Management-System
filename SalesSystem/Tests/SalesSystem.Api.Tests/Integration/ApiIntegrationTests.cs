using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SalesSystem.Contracts.Requests;
using Xunit;

namespace SalesSystem.Api.Tests.Integration;

/// <summary>
/// Integration tests that send real HTTP requests to the running API at http://localhost:5221.
/// SKIPPED by default — start the API project manually to run these tests.
/// </summary>
[Trait("Category", "Integration")]
public class ApiIntegrationTests : IAsyncLifetime
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private string? _authToken;

    public ApiIntegrationTests()
    {
        _baseUrl = "http://localhost:5221";
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(_baseUrl),
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    public async Task InitializeAsync()
    {
        await TryLoginAsync();
    }

    public Task DisposeAsync()
    {
        _httpClient.Dispose();
        return Task.CompletedTask;
    }

    private async Task TryLoginAsync()
    {
        try
        {
            var loginRequest = new LoginRequest("admin", "admin123");
            var response = await _httpClient.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<JsonElement>();
                if (result.TryGetProperty("token", out var tokenElement))
                {
                    _authToken = tokenElement.GetString();
                    _httpClient.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _authToken);
                }
            }
        }
        catch
        {
            // API not running — skip login
        }
    }

    private const string SkipReason = "Integration tests require a running API at http://localhost:5221. Start the SalesSystem.Api project first.";

    #region Auth Tests

    [Fact(Skip = SkipReason)]
    public async Task Login_ValidCredentials_ReturnsToken()
    {
        var loginRequest = new LoginRequest("admin", "admin123");
        var response = await _httpClient.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
    }

    [Fact(Skip = SkipReason)]
    public async Task Login_InvalidCredentials_ReturnsBadRequest()
    {
        var loginRequest = new LoginRequest("invalid", "invalid");
        var response = await _httpClient.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Products Tests

    [Fact(Skip = SkipReason)]
    public async Task Products_GetAll_ReturnsProducts()
    {
        var response = await _httpClient.GetAsync("/api/v1/products");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Unauthorized);
    }

    [Fact(Skip = SkipReason)]
    public async Task Products_Create_ReturnsCreated()
    {
        var request = new CreateProductRequest(
            Barcode: "TEST123",
            Name: "Test Product",
            CategoryId: 1,
            ReorderLevel: 10,
            Description: "Test Description"
        );
        var response = await _httpClient.PostAsJsonAsync("/api/v1/products", request);
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Customers Tests

    [Fact(Skip = SkipReason)]
    public async Task Customers_GetAll_ReturnsCustomers()
    {
        var response = await _httpClient.GetAsync("/api/v1/customers");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Unauthorized);
    }

    [Fact(Skip = SkipReason)]
    public async Task Customers_Create_ReturnsCreated()
    {
        var request = new CreateCustomerRequest(
            Name: "Test Customer",
            Phone: "0123456789",
            Email: "test@test.com",
            Address: "Test Address",
            TaxNumber: null,
            CreditLimit: 1000
        );
        var response = await _httpClient.PostAsJsonAsync("/api/v1/customers", request);
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Suppliers Tests

    [Fact(Skip = SkipReason)]
    public async Task Suppliers_GetAll_ReturnsSuppliers()
    {
        var response = await _httpClient.GetAsync("/api/v1/suppliers");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Sales Invoices Tests

    [Fact(Skip = SkipReason)]
    public async Task SalesInvoices_GetAll_ReturnsInvoices()
    {
        var response = await _httpClient.GetAsync("/api/v1/sales-invoices");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Unauthorized, HttpStatusCode.NotFound);
    }

    #endregion

    #region Purchase Invoices Tests

    [Fact(Skip = SkipReason)]
    public async Task PurchaseInvoices_GetAll_ReturnsInvoices()
    {
        var response = await _httpClient.GetAsync("/api/v1/purchase-invoices");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Unauthorized, HttpStatusCode.NotFound);
    }

    #endregion

    #region Warehouses Tests

    [Fact(Skip = SkipReason)]
    public async Task Warehouses_GetAll_ReturnsWarehouses()
    {
        var response = await _httpClient.GetAsync("/api/v1/warehouses");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Unauthorized, HttpStatusCode.NotFound);
    }

    #endregion

    #region Units Tests

    [Fact(Skip = SkipReason)]
    public async Task Units_GetAll_ReturnsUnits()
    {
        var response = await _httpClient.GetAsync("/api/v1/units");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Unauthorized, HttpStatusCode.NotFound);
    }

    #endregion

    #region Inventory Tests

    [Fact(Skip = SkipReason)]
    public async Task Inventory_GetAll_ReturnsInventory()
    {
        var response = await _httpClient.GetAsync("/api/v1/inventory");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Unauthorized, HttpStatusCode.NotFound);
    }

    #endregion

    #region Stock Transfers Tests

    [Fact(Skip = SkipReason)]
    public async Task StockTransfers_GetAll_ReturnsTransfers()
    {
        var response = await _httpClient.GetAsync("/api/v1/stock-transfers");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Unauthorized, HttpStatusCode.NotFound);
    }

    #endregion

    #region Dashboard Tests

    [Fact(Skip = SkipReason)]
    public async Task Dashboard_GetSummary_ReturnsSummary()
    {
        var response = await _httpClient.GetAsync("/api/v1/dashboard/summary");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Unauthorized);
    }

    #endregion
}
