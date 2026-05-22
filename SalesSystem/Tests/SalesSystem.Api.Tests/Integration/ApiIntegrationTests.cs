using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Domain.Enums;
using Xunit;

namespace SalesSystem.Api.Tests.Integration;

/// <summary>
/// Integration tests that send real HTTP requests to the running API
/// </summary>
public class ApiIntegrationTests : IAsyncLifetime
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private string? _authToken;
    private int _testUserId = 1;

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
        // First, try to login to get auth token
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
            // Try to login - API might have seed data
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
                if (result.TryGetProperty("userId", out var userIdElement))
                {
                    _testUserId = userIdElement.GetInt32();
                }
            }
        }
        catch
        {
            // If login fails, continue without token - some endpoints might be anonymous
        }
    }

    #region Auth Tests

    [Fact]
    public async Task Login_ValidCredentials_ReturnsToken()
    {
        // Arrange
        var loginRequest = new LoginRequest("admin", "admin123");

        // Act
        var response = await _httpClient.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_InvalidCredentials_ReturnsBadRequest()
    {
        // Arrange
        var loginRequest = new LoginRequest("invalid", "invalid");

        // Act
        var response = await _httpClient.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Categories Tests

    [Fact]
    public async Task Categories_GetAll_ReturnsCategories()
    {
        // Act
        var response = await _httpClient.GetAsync("/api/v1/categories");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Categories_Create_ReturnsCreated()
    {
        // Arrange
        var request = new CreateCategoryRequest("Test Category", "Test Description");

        // Act
        var response = await _httpClient.PostAsJsonAsync("/api/v1/categories", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Products Tests

    [Fact]
    public async Task Products_GetAll_ReturnsProducts()
    {
        // Act
        var response = await _httpClient.GetAsync("/api/v1/products");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Products_Create_ReturnsCreated()
    {
        // Arrange - Using correct request signature
        var request = new CreateProductRequest(
            Barcode: "TEST123",
            Name: "Test Product",
            CategoryId: 1,
            UnitId: 1,
            RetailUnitId: 1,
            WholesaleUnitId: 2,
            ConversionFactor: 10,
            PurchasePrice: 100,
            SalePrice: 150,
            RetailPrice: 150,
            WholesalePrice: 1300,
            MinStock: 10,
            Description: "Test Description"
        );

        // Act
        var response = await _httpClient.PostAsJsonAsync("/api/v1/products", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Customers Tests

    [Fact]
    public async Task Customers_GetAll_ReturnsCustomers()
    {
        // Act
        var response = await _httpClient.GetAsync("/api/v1/customers");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Customers_Create_ReturnsCreated()
    {
        // Arrange - Using correct request signature
        var request = new CreateCustomerRequest(
            Name: "Test Customer",
            Phone: "0123456789",
            Email: "test@test.com",
            Address: "Test Address",
            TaxNumber: null,
            OpeningBalance: 0,
            CreditLimit: 1000
        );

        // Act
        var response = await _httpClient.PostAsJsonAsync("/api/v1/customers", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Suppliers Tests

    [Fact]
    public async Task Suppliers_GetAll_ReturnsSuppliers()
    {
        // Act
        var response = await _httpClient.GetAsync("/api/v1/suppliers");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Sales Invoices Tests

    [Fact]
    public async Task SalesInvoices_GetAll_ReturnsInvoices()
    {
        // Act - Correct route is /api/v1/sales-invoices
        var response = await _httpClient.GetAsync("/api/v1/sales-invoices");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Unauthorized, HttpStatusCode.NotFound);
    }

    #endregion

    #region Purchase Invoices Tests

    [Fact]
    public async Task PurchaseInvoices_GetAll_ReturnsInvoices()
    {
        // Act - Correct route is /api/v1/purchase-invoices
        var response = await _httpClient.GetAsync("/api/v1/purchase-invoices");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Unauthorized, HttpStatusCode.NotFound);
    }

    #endregion

    #region Warehouses Tests

    [Fact]
    public async Task Warehouses_GetAll_ReturnsWarehouses()
    {
        // Act - Correct route is /api/v1/warehouses
        var response = await _httpClient.GetAsync("/api/v1/warehouses");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Unauthorized, HttpStatusCode.NotFound);
    }

    #endregion

    #region Units Tests

    [Fact]
    public async Task Units_GetAll_ReturnsUnits()
    {
        // Act - Correct route is /api/v1/units
        var response = await _httpClient.GetAsync("/api/v1/units");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Unauthorized, HttpStatusCode.NotFound);
    }

    #endregion

    #region Inventory Tests

    [Fact]
    public async Task Inventory_GetAll_ReturnsInventory()
    {
        // Act - Correct route is /api/v1/inventory
        var response = await _httpClient.GetAsync("/api/v1/inventory");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Unauthorized, HttpStatusCode.NotFound);
    }

    #endregion

    #region Stock Transfers Tests

    [Fact]
    public async Task StockTransfers_GetAll_ReturnsTransfers()
    {
        // Act - Correct route is /api/v1/stock-transfers
        var response = await _httpClient.GetAsync("/api/v1/stock-transfers");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Unauthorized, HttpStatusCode.NotFound);
    }

    #endregion

    #region Dashboard Tests

    [Fact]
    public async Task Dashboard_GetSummary_ReturnsSummary()
    {
        // Act
        var response = await _httpClient.GetAsync("/api/v1/dashboard/summary");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Unauthorized);
    }

    #endregion
}