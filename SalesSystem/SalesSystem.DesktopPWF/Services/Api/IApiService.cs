using System.Net.Http;
using System.Net.Http.Json;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Enums;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;
using SalesSystem.DesktopPWF.Enums;
using SalesSystem.DesktopPWF.Models;
using System.Text.Json.Serialization;

namespace SalesSystem.DesktopPWF.Services.Api;

/// <summary>
/// Error response from API
/// </summary>
public record ErrorResponse(
    [property: JsonPropertyName("error")] string Error, 
    [property: JsonPropertyName("errorCode")] string? ErrorCode,
    [property: JsonPropertyName("userId")] int? UserId = null,
    [property: JsonPropertyName("token")] string? Token = null);

/// <summary>
/// Base class for all API services in WPF
/// </summary>
public abstract class ApiServiceBase
{
    protected readonly HttpClient _httpClient;
    protected readonly ISessionService _session;

    protected ApiServiceBase(HttpClient httpClient, ISessionService session)
    {
        _httpClient = httpClient;
        _session = session;
    }

    protected void AddAuthHeader()
    {
        var token = _session.GetToken();
        if (!string.IsNullOrEmpty(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }
    }

    protected async Task<Result<T>> HandleResponseAsync<T>(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            var data = await response.Content.ReadFromJsonAsync<T>();
            return Result<T>.Success(data!);
        }

        try
        {
            if (response.Content.Headers.ContentType?.MediaType == "application/json")
            {
                var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
                Serilog.Log.Warning("API failure: {StatusCode} - {Error} ({ErrorCode})", response.StatusCode, error?.Error, error?.ErrorCode);
                return Result<T>.Failure(error?.Error ?? "حدث خطأ", error?.ErrorCode ?? "Unknown");
            }
            
            var content = await response.Content.ReadAsStringAsync();
            Serilog.Log.Warning("API failure (non-JSON): {StatusCode} - {Content}", response.StatusCode, content);
            return Result<T>.Failure($"خطأ في الخادم: {response.StatusCode}", response.StatusCode.ToString());
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Unexpected error parsing API error response. StatusCode: {StatusCode}", response.StatusCode);
            return Result<T>.Failure("حدث خطأ غير متوقع", "Unknown");
        }
    }

    protected async Task<Result<List<T>>> HandlePagedResponseAsync<T>(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            var data = await response.Content.ReadFromJsonAsync<PagedResult<T>>();
            return Result<List<T>>.Success(data?.Items?.ToList() ?? new List<T>());
        }

        try
        {
            if (response.Content.Headers.ContentType?.MediaType == "application/json")
            {
                var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
                Serilog.Log.Warning("API Paged failure: {StatusCode} - {Error} ({ErrorCode})", response.StatusCode, error?.Error, error?.ErrorCode);
                return Result<List<T>>.Failure(error?.Error ?? "حدث خطأ", error?.ErrorCode ?? "Unknown");
            }

            var content = await response.Content.ReadAsStringAsync();
            Serilog.Log.Warning("API Paged failure (non-JSON): {StatusCode} - {Content}", response.StatusCode, content);
            return Result<List<T>>.Failure($"خطأ في الخادم: {response.StatusCode}", response.StatusCode.ToString());
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Unexpected error parsing API paged error response. StatusCode: {StatusCode}", response.StatusCode);
            return Result<List<T>>.Failure("حدث خطأ غير متوقع", "Unknown");
        }
    }

    protected async Task<Result> HandleResponseAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return Result.Success();
        }

        try
        {
            if (response.Content.Headers.ContentType?.MediaType == "application/json")
            {
                var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
                Serilog.Log.Warning("API Command failure: {StatusCode} - {Error} ({ErrorCode})", response.StatusCode, error?.Error, error?.ErrorCode);
                return Result.Failure(error?.Error ?? "حدث خطأ", error?.ErrorCode ?? "Unknown");
            }

            var content = await response.Content.ReadAsStringAsync();
            Serilog.Log.Warning("API Command failure (non-JSON): {StatusCode} - {Content}", response.StatusCode, content);
            return Result.Failure($"خطأ في الخادم: {response.StatusCode}", response.StatusCode.ToString());
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Unexpected error parsing API command error response. StatusCode: {StatusCode}", response.StatusCode);
            return Result.Failure("حدث خطأ غير متوقع", "Unknown");
        }
    }

    protected Result<T> HandleConnectionError<T>(Exception ex, string context)
    {
        Serilog.Log.Error(ex, "Connection error in {Context}", context);
        return Result<T>.Failure("فشل في الاتصال بالخادم. يرجى التحقق من الشبكة.", "ConnectionError");
    }

    protected Result HandleConnectionError(Exception ex, string context)
    {
        Serilog.Log.Error(ex, "Connection error in {Context}", context);
        return Result.Failure("فشل في الاتصال بالخادم. يرجى التحقق من الشبكة.", "ConnectionError");
    }

    protected async Task<Result<T>> ExecuteAsync<T>(Func<Task<HttpResponseMessage>> action, string context)
    {
        try
        {
            AddAuthHeader();
            var response = await action();
            return await HandleResponseAsync<T>(response);
        }
        catch (Exception ex)
        {
            return HandleConnectionError<T>(ex, context);
        }
    }

    protected async Task<Result<List<T>>> ExecutePagedAsync<T>(Func<Task<HttpResponseMessage>> action, string context)
    {
        try
        {
            AddAuthHeader();
            var response = await action();
            return await HandlePagedResponseAsync<T>(response);
        }
        catch (Exception ex)
        {
            return HandleConnectionError<List<T>>(ex, context);
        }
    }

    protected async Task<Result> ExecuteCommandAsync(Func<Task<HttpResponseMessage>> action, string context)
    {
        try
        {
            AddAuthHeader();
            var response = await action();
            return await HandleResponseAsync(response);
        }
        catch (Exception ex)
        {
            return HandleConnectionError(ex, context);
        }
    }
}

public interface ISessionService
{
    string? GetToken();
    string? GetUserName();
    int? GetUserId();
    List<int>? GetUserRoleIds();
    string? GetUserRoleName();
    bool HasRole(int roleId);
    bool IsAdmin { get; }
    bool IsManagerOrAbove { get; }
    void SetSession(string token, string userName, int userId, List<int> roleIds, string roleName);
    /// <summary>
    /// Updates the session with API-loaded permission codes after fetching CurrentUserDto.
    /// Overrides the hardcoded role-based permissions with actual DB-driven permissions.
    /// </summary>
    void SetApiPermissions(Permission permissions);
    void ClearSession();
    bool IsAuthenticated { get; }
    bool CanAccess(Permission permission);
    Permission GetPermissions();

    /// <summary>
    /// Gets the current UI view mode (Basic vs Advanced).
    /// Basic = operational screens only; Advanced = operational + accounting.
    /// Determined by user role at login time.
    /// </summary>
    ViewMode GetViewMode();
}

public interface ILogsApiService
{
    Task<Result> SendLogAsync(CreateLogRequest request);
    Task<Result<PagedResult<SystemLogDto>>> QueryLogsAsync(int? level = null, string? source = null, string? search = null, DateTime? from = null, DateTime? to = null, int page = 1, int pageSize = 50, CancellationToken ct = default);
}

public interface IProductApiService
{
    Task<Result<List<ProductDto>>> GetAllAsync(bool includeInactive = false);
    Task<Result<ProductDto>> GetByIdAsync(int id);
    Task<Result<ProductDto>> CreateAsync(CreateProductRequest request);
    Task<Result<ProductDto>> UpdateAsync(int id, UpdateProductRequest request);
    Task<Result> DeleteAsync(int id);
    Task<Result> DeletePermanentlyAsync(int id);
    Task<Result<List<ProductDto>>> SearchAsync(string searchTerm);
    Task<Result<ProductDto>> GetByBarcodeAsync(string barcode);
    Task<Result<ProductDto>> UploadImageAsync(int productId, byte[] imageBytes, string fileName);
    Task<Result<List<ProductDto>>> GetExpiringProductsAsync(int thresholdDays = 30);
}

public interface IUnitApiService
{
    Task<Result<List<UnitDto>>> GetAllAsync(bool includeInactive = false);
    Task<Result<UnitDto>> CreateAsync(CreateUnitRequest request);
    Task<Result<UnitDto>> UpdateAsync(int id, UpdateUnitRequest request);
    Task<Result> DeleteAsync(int id);
    Task<Result> DeletePermanentlyAsync(int id);
}

public interface ICustomerApiService
{
    Task<Result<List<CustomerDto>>> GetAllAsync(bool includeInactive = false);
    Task<Result<CustomerDto>> GetByIdAsync(int id);
    Task<Result<CustomerDto>> CreateAsync(CreateCustomerRequest request);
    Task<Result<CustomerDto>> UpdateAsync(int id, UpdateCustomerRequest request);
    Task<Result> DeleteAsync(int id);
    Task<Result> DeletePermanentlyAsync(int id);
}

public interface ISupplierApiService
{
    Task<Result<List<SupplierDto>>> GetAllAsync(bool includeInactive = false);
    Task<Result<SupplierDto>> GetByIdAsync(int id);
    Task<Result<SupplierDto>> CreateAsync(CreateSupplierRequest request);
    Task<Result<SupplierDto>> UpdateAsync(int id, UpdateSupplierRequest request);
    Task<Result> DeleteAsync(int id);
    Task<Result> DeletePermanentlyAsync(int id);
}

public interface IWarehouseApiService
{
    Task<Result<List<WarehouseDto>>> GetAllAsync(bool includeInactive = false);
    Task<Result<WarehouseDto>> GetByIdAsync(int id);
    Task<Result<WarehouseDto>> CreateAsync(CreateWarehouseRequest request);
    Task<Result<WarehouseDto>> UpdateAsync(int id, UpdateWarehouseRequest request);
    Task<Result> DeleteAsync(int id);
    Task<Result> DeletePermanentlyAsync(int id);
}

public interface IUserApiService
{
    Task<Result<List<UserDto>>> GetAllAsync(bool includeInactive = false);
    Task<Result<UserDto>> GetByIdAsync(int id);
    Task<Result<UserDto>> CreateAsync(CreateUserRequest request);
    Task<Result<UserDto>> UpdateAsync(int id, UpdateUserRequest request);
    Task<Result> DeleteAsync(int id);
    Task<Result> DeletePermanentlyAsync(int id);
    Task<Result<CurrentUserDto>> GetCurrentUserAsync();
    Task<Result<ResetPasswordResponse>> ResetPasswordAsync(int id);
}

public interface IAuthApiService
{
    Task<Result<LoginResponse>> LoginAsync(LoginRequest request);
    Task<LoginResult> LoginWithDetailsAsync(LoginRequest request);
    Task<Result> SetPasswordAsync(SetPasswordRequest request);
    Task<Result> ChangePasswordAsync(ChangePasswordRequest request);
}

public interface IDashboardApiService
{
    Task<Result<DashboardSummaryDto>> GetSummaryAsync(CancellationToken ct = default);
}

public interface IReportApiService
{
    Task<Result<List<SalesReportDto>>> GetSalesReportAsync(int? warehouseId, DateTime from, DateTime to, CancellationToken ct = default);
    Task<Result<List<PurchaseReportDto>>> GetPurchasesReportAsync(int? warehouseId, DateTime from, DateTime to, CancellationToken ct = default);
    Task<Result<List<StockReportDto>>> GetStockReportAsync(int? warehouseId = null, CancellationToken ct = default);
    Task<Result<List<CustomerFinancialBalanceDto>>> GetCustomerBalancesReportAsync(int? customerId = null, CancellationToken ct = default);
    Task<Result<List<SupplierBalanceReportDto>>> GetSupplierBalancesReportAsync(int? supplierId = null, CancellationToken ct = default);
    Task<Result<List<ProductMovementReportDto>>> GetProductMovementsReportAsync(int productId, DateTime? from = null, DateTime? to = null, CancellationToken ct = default);
    Task<Result<List<LowStockReportDto>>> GetLowStockReportAsync(int? warehouseId = null, CancellationToken ct = default);
    Task<Result<List<ExpiredProductDto>>> GetExpiredProductsReportAsync(int thresholdDays = 0, CancellationToken ct = default);
    Task<Result<List<StockBalanceReportDto>>> GetStockBalanceReportAsync(int? warehouseId = null, CancellationToken ct = default);
    Task<Result<List<WarehouseMovementReportDto>>> GetWarehouseMovementsAsync(int? warehouseId = null, DateTime? from = null, DateTime? to = null, CancellationToken ct = default);
    Task<Result<List<DetailedStockLedgerDto>>> GetDetailedStockLedgerAsync(int? productId = null, int? warehouseId = null, DateTime? from = null, DateTime? to = null, CancellationToken ct = default);
    Task<Result<List<ReturnsReportDto>>> GetReturnsReportAsync(string? returnType = null, DateTime? from = null, DateTime? to = null, int? productId = null, CancellationToken ct = default);
    Task<Result<List<AgingReportDto>>> GetAgingReportAsync(string partyType = "Customers", int? partyId = null, CancellationToken ct = default);
}

public interface ISettingsApiService
{
    Task<Result<StoreSettingsDto>> GetSettingsAsync(CancellationToken ct = default);
    Task<Result<StoreSettingsDto>> UpdateSettingsAsync(UpdateSettingsRequest request, CancellationToken ct = default);
    Task<Result<PrintSettingsDto>> GetPrintSettingsAsync(CancellationToken ct = default);
    Task<Result> UpdatePrintSettingsAsync(UpdatePrintSettingsRequest request, CancellationToken ct = default);
    Task<Result<int>> GetCostingMethodAsync(CancellationToken ct = default);
    Task<Result> SetCostingMethodAsync(UpdateCostingMethodRequest request, CancellationToken ct = default);
    Task<Result<Dictionary<string, string>>> GetAllSystemSettingsAsync(CancellationToken ct = default);
    Task<Result> UpdateSystemSettingsAsync(Dictionary<string, string> settings, CancellationToken ct = default);
    void RefreshCache();
}

public interface IInventoryApiService
{
    Task<Result<decimal>> GetStockAsync(int productId, int warehouseId, CancellationToken ct = default);
    Task<Result<List<InventoryTransactionDto>>> GetMovementsAsync(int? productId = null, int? warehouseId = null, int? movementType = null, int page = 1, int pageSize = 50, CancellationToken ct = default);
    Task<Result<List<WarehouseStockDto>>> GetWarehouseStocksAsync(int? warehouseId = null, int? productId = null, int page = 1, int pageSize = 50, CancellationToken ct = default);
    Task<Result<InventoryTransactionDto>> CreateInventoryTransactionAsync(CreateInventoryTransactionRequest request, CancellationToken ct = default);
    Task<Result<InventoryTransactionDto>> PostInventoryTransactionAsync(int id, CancellationToken ct = default);
    Task<Result<InventoryTransactionDto>> CancelInventoryTransactionAsync(int id, CancellationToken ct = default);
    Task<Result<InventoryTransactionDto>> GetInventoryTransactionByIdAsync(int id, CancellationToken ct = default);
}

public interface ISalesInvoiceApiService
{
    Task<Result<List<SalesInvoiceDto>>> GetAllAsync(string? search = null, DateTime? from = null, DateTime? to = null, byte? status = null, bool includeInactive = false, int page = 1, int pageSize = 100, int? customerId = null, CancellationToken ct = default);
    Task<Result<SalesInvoiceDto>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Result<SalesInvoiceDto>> CreateAsync(CreateSalesInvoiceRequest request, CancellationToken ct = default);
    Task<Result<SalesInvoiceDto>> UpdateAsync(int id, UpdateSalesInvoiceRequest request, CancellationToken ct = default);
    Task<Result<SalesInvoiceDto>> PostAsync(int id, PostSalesInvoiceRequest? request = null, CancellationToken ct = default);
    Task<Result<SalesInvoiceDto>> CancelAsync(int id, CancellationToken ct = default);
}

public interface IPurchaseInvoiceApiService
{
    Task<Result<List<PurchaseInvoiceDto>>> GetAllAsync(string? search = null, DateTime? from = null, DateTime? to = null, byte? status = null, bool includeInactive = false, int page = 1, int pageSize = 100, int? supplierId = null, CancellationToken ct = default);
    Task<Result<PurchaseInvoiceDto>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Result<PurchaseInvoiceDto>> CreateAsync(CreatePurchaseInvoiceRequest request, CancellationToken ct = default);
    Task<Result<PurchaseInvoiceDto>> UpdateAsync(int id, UpdatePurchaseInvoiceRequest request, CancellationToken ct = default);
    Task<Result<PurchaseInvoiceDto>> PostAsync(int id, CancellationToken ct = default);
    Task<Result<PurchaseInvoiceDto>> CancelAsync(int id, CancellationToken ct = default);
    Task<Result<string>> UploadAttachmentAsync(int id, string base64Content, string fileName, CancellationToken ct = default);
    Task<Result> DeleteAttachmentAsync(int id, CancellationToken ct = default);
}

public interface ISalesReturnApiService
{
    Task<Result<List<SalesReturnDto>>> GetAllAsync(string? search = null, DateTime? from = null, DateTime? to = null, bool includeInactive = false, int page = 1, int pageSize = 100, CancellationToken ct = default);
    Task<Result<SalesReturnDto>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Result<SalesReturnDto>> CreateAsync(CreateSalesReturnRequest request, CancellationToken ct = default);
    Task<Result<SalesReturnDto>> PostAsync(int id, PostSalesReturnRequest? request = null, CancellationToken ct = default);
    Task<Result<SalesReturnDto>> CancelAsync(int id, CancellationToken ct = default);
}

public interface IPurchaseReturnApiService
{
    Task<Result<List<PurchaseReturnDto>>> GetAllAsync(string? search = null, DateTime? from = null, DateTime? to = null, bool includeInactive = false, int page = 1, int pageSize = 100, CancellationToken ct = default);
    Task<Result<PurchaseReturnDto>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Result<PurchaseReturnDto>> CreateAsync(CreatePurchaseReturnRequest request, CancellationToken ct = default);
    Task<Result<PurchaseReturnDto>> PostAsync(int id, CancellationToken ct = default);
    Task<Result<PurchaseReturnDto>> CancelAsync(int id, CancellationToken ct = default);
    Task<Result<Dictionary<int, decimal>>> GetReturnedQuantitiesByInvoiceAsync(int invoiceId, CancellationToken ct = default);
}

public interface IWarehouseTransferApiService
{
    Task<Result<List<WarehouseTransferDto>>> GetAllAsync(string? search = null, DateTime? from = null, DateTime? to = null, byte? status = null, bool includeInactive = false, int page = 1, int pageSize = 100, CancellationToken ct = default);
    Task<Result<WarehouseTransferDto>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Result<WarehouseTransferDto>> CreateAsync(CreateWarehouseTransferRequest request, CancellationToken ct = default);
    Task<Result<WarehouseTransferDto>> UpdateAsync(int id, CreateWarehouseTransferRequest request, CancellationToken ct = default);
    Task<Result<WarehouseTransferDto>> PostAsync(int id, CancellationToken ct = default);
    Task<Result<WarehouseTransferDto>> CancelAsync(int id, CancellationToken ct = default);
}

public interface ISupplierPaymentApiService
{
    Task<Result<List<SupplierPaymentDto>>> GetAllAsync(string? search = null, DateTime? from = null, DateTime? to = null, bool includeInactive = false, int page = 1, int pageSize = 100, CancellationToken ct = default);
    Task<Result<SupplierPaymentDto>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Result<SupplierPaymentDto>> CreateAsync(CreateSupplierPaymentRequest request, CancellationToken ct = default);
    Task<Result<SupplierPaymentDto>> UpdateAsync(int id, UpdateSupplierPaymentRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(int id, CancellationToken ct = default);
}

public interface IBackupApiService
{
    Task<Result<string>> CreateBackupAsync(CancellationToken ct = default);
    Task<Result<List<string>>> GetBackupListAsync(CancellationToken ct = default);
    Task<Result> RestoreBackupAsync(string fileName, CancellationToken ct = default);
}

public interface IPrintApiService
{
    Task<Result> PrintSalesA4Async(int invoiceId, CancellationToken ct = default);
    Task<Result> PrintSalesThermalAsync(int invoiceId, CancellationToken ct = default);
    Task<Result> PrintPurchaseA4Async(int invoiceId, CancellationToken ct = default);
    Task<Result> PrintPurchaseThermalAsync(int invoiceId, CancellationToken ct = default);
    Task<Result> TestPrintAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets A4 PDF bytes for a sales invoice from the API and saves to a temp file.
    /// Returns the temp file path on success.
    /// </summary>
    Task<Result<string>> GetSalesA4PdfAsync(int invoiceId, CancellationToken ct = default);

    /// <summary>
    /// Gets A4 PDF bytes for a purchase invoice from the API and saves to a temp file.
    /// Returns the temp file path on success.
    /// </summary>
    Task<Result<string>> GetPurchaseA4PdfAsync(int invoiceId, CancellationToken ct = default);
}

public interface IDatabaseHealthCheckService
{
    Task<HealthCheckResult> CheckAsync(CancellationToken ct = default);
}

public record HealthCheckResult
{
    public bool IsDatabaseConnected { get; init; }
    public string? ErrorMessage { get; init; }
    public bool IsApiReachable { get; init; }
}

public interface ITaxesApiService
{
    Task<Result<List<TaxDto>>> GetAllAsync(bool includeInactive = false);
    Task<Result<TaxDto>> GetByIdAsync(int id);
    Task<Result<TaxDto>> CreateAsync(CreateTaxRequest request);
    Task<Result<TaxDto>> UpdateAsync(int id, UpdateTaxRequest request);
    Task<Result> DeleteAsync(int id);
    Task<Result> DeletePermanentlyAsync(int id);
}

public interface ICurrencyApiService
{
    Task<Result<List<CurrencyDto>>> GetAllAsync(bool includeInactive = false);
    Task<Result<CurrencyDto>> GetByIdAsync(int id);
    Task<Result<CurrencyDto>> CreateAsync(CreateCurrencyRequest request);
    Task<Result<CurrencyDto>> UpdateAsync(int id, UpdateCurrencyRequest request);
    Task<Result> DeleteAsync(int id);
    Task<Result> DeletePermanentlyAsync(int id);
}

public interface IFinancialReportApiService
{
    Task<Result<List<IncomeStatementDto>>> GetIncomeStatementAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<Result<CashFlowReportDto>> GetCashFlowReportAsync(DateTime from, DateTime to, int? cashBoxId = null, CancellationToken ct = default);
    Task<Result<List<VatReportDto>>> GetVatReportAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<Result<List<AccountStatementDto>>> GetCustomerAccountStatementAsync(int customerId, DateTime from, DateTime to, CancellationToken ct = default);
    Task<Result<List<AccountStatementDto>>> GetSupplierAccountStatementAsync(int supplierId, DateTime from, DateTime to, CancellationToken ct = default);

    // Phase 31 — Financial Reports
    Task<Result<BalanceSheetDto>> GetBalanceSheetAsync(DateTime asOfDate, CancellationToken ct = default);
    Task<Result<List<TrialBalanceDto>>> GetTrialBalanceAsync(DateTime asOfDate, CancellationToken ct = default);
    Task<Result<AccountLedgerDto>> GetGeneralLedgerAsync(int accountId, DateTime from, DateTime to, CancellationToken ct = default);
    Task<Result<WorkingCapitalSummaryDto>> GetWorkingCapitalAsync(DateTime asOfDate, CancellationToken ct = default);
    Task<Result<List<AccountBalanceReportDto>>> GetAccountBalancesAsync(byte? accountType = null, string? search = null, CancellationToken ct = default);
}

public interface IFiscalYearApiService
{
    Task<Result<List<FiscalYearDto>>> GetAllAsync(CancellationToken ct = default);
    Task<Result<FiscalYearDto>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Result<FiscalYearDto>> GetByYearAsync(int year, CancellationToken ct = default);
    Task<Result<FiscalYearDto>> CreateAsync(CreateFiscalYearRequest request, CancellationToken ct = default);
    Task<Result<FiscalYearDto>> OpenAsync(int id, CancellationToken ct = default);
    Task<Result<FiscalYearDto>> CloseAsync(int id, CancellationToken ct = default);
}

public interface IAuditLogApiService
{
    Task<Result<PagedResult<AuditLogDto>>> QueryAsync(AuditLogQuery query);
    Task<Result<List<AuditLogDto>>> GetUserHistoryAsync(int userId, int limit = 50);
    Task<Result<List<AuditLogDto>>> GetLoginHistoryAsync(int? userId, int limit = 50);
}

public interface IPermissionApiService
{
    Task<Result<List<PermissionDto>>> GetAllAsync();
    Task<Result<Dictionary<byte, List<int>>>> GetRolePermissionsAsync();
    Task<Result> UpdateRolePermissionsAsync(byte role, List<int> permissionIds);
}

public interface IBranchApiService
{
    Task<Result<List<BranchDto>>> GetAllAsync(bool includeInactive = false);
    Task<Result<BranchDto>> GetByIdAsync(int id);
    Task<Result<BranchDto>> CreateAsync(CreateBranchRequest request);
    Task<Result<BranchDto>> UpdateAsync(int id, UpdateBranchRequest request);
    Task<Result> DeactivateAsync(int id);
}

public interface IDepartmentApiService
{
    Task<Result<List<DepartmentDto>>> GetAllAsync(bool includeInactive = false);
    Task<Result<DepartmentDto>> GetByIdAsync(int id);
    Task<Result<DepartmentDto>> CreateAsync(CreateDepartmentRequest request);
    Task<Result<DepartmentDto>> UpdateAsync(int id, UpdateDepartmentRequest request);
    Task<Result> DeactivateAsync(int id);
}

public interface IBankApiService
{
    Task<Result<List<BankDto>>> GetAllAsync(bool includeInactive = false);
    Task<Result<BankDto>> GetByIdAsync(int id);
    Task<Result<BankDto>> CreateAsync(CreateBankRequest request);
    Task<Result<BankDto>> UpdateAsync(int id, UpdateBankRequest request);
    Task<Result> DeactivateAsync(int id);
}

public interface IPartyApiService
{
    Task<Result<List<PartyDto>>> GetAllAsync(bool includeInactive = false);
    Task<Result<PartyDto>> GetByIdAsync(int id);
    Task<Result<PartyDto>> CreateAsync(CreatePartyRequest request);
    Task<Result<PartyDto>> UpdateAsync(int id, UpdatePartyRequest request);
    Task<Result> DeactivateAsync(int id);
}

public interface IEmployeeApiService
{
    Task<Result<List<EmployeeDto>>> GetAllAsync(bool includeInactive = false);
    Task<Result<EmployeeDto>> GetByIdAsync(int id);
    Task<Result<EmployeeDto>> CreateAsync(CreateEmployeeRequest request);
    Task<Result<EmployeeDto>> UpdateAsync(int id, UpdateEmployeeRequest request);
    Task<Result> DeactivateAsync(int id);
}

public interface IExpenseApiService
{
    Task<Result<List<ExpenseDto>>> GetAllAsync(string? search = null, DateTime? from = null, DateTime? to = null, int page = 1, int pageSize = 100, CancellationToken ct = default);
    Task<Result<ExpenseDto>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Result<ExpenseDto>> CreateAsync(CreateExpenseRequest request, CancellationToken ct = default);
    Task<Result<ExpenseDto>> UpdateAsync(int id, UpdateExpenseRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(int id, CancellationToken ct = default);
    Task<Result<ExpenseDto>> PostAsync(int id, CancellationToken ct = default);
    Task<Result<ExpenseDto>> CancelAsync(int id, CancellationToken ct = default);
}

public interface IProductCategoryApiService
{
    Task<Result<List<ProductCategoryDto>>> GetAllAsync(bool includeInactive = false);
    Task<Result<ProductCategoryDto>> GetByIdAsync(int id);
    Task<Result<ProductCategoryDto>> CreateAsync(CreateProductCategoryRequest request);
    Task<Result<ProductCategoryDto>> UpdateAsync(int id, UpdateProductCategoryRequest request);
    Task<Result> DeactivateAsync(int id);
    Task<Result> ReactivateAsync(int id);
}

public interface INotificationApiService
{
    Task<Result<List<NotificationDto>>> GetAllAsync(int? userId = null, bool unreadOnly = false, int page = 1, int pageSize = 50);
    Task<Result> MarkAsReadAsync(int id);
    Task<Result> MarkAllAsReadAsync(int userId);
}

public interface IInventoryCountApiService
{
    Task<Result<List<InventoryCountDto>>> GetAllAsync(bool includeInactive = false);
    Task<Result<InventoryCountDto>> GetByIdAsync(int id);
    Task<Result<InventoryCountDto>> CreateAsync(CreateInventoryCountRequest request);
    Task<Result<InventoryCountDto>> PostAsync(int id);
    Task<Result> CancelAsync(int id);
    Task<Result<InventoryCountDto>> AddLineAsync(int id, AddInventoryCountLineRequest request);
}

public interface IInventoryAdjustmentApiService
{
    Task<Result<List<InventoryAdjustmentDto>>> GetAllAsync(bool includeInactive = false);
    Task<Result<InventoryAdjustmentDto>> GetByIdAsync(int id);
    Task<Result<InventoryAdjustmentDto>> CreateAsync(CreateInventoryAdjustmentRequest request);
    Task<Result<InventoryAdjustmentDto>> PostAsync(int id);
    Task<Result> CancelAsync(int id);
    Task<Result<InventoryAdjustmentDto>> AddLineAsync(int id, AddInventoryAdjustmentLineRequest request);
}

public interface ICustomerReceiptApiService
{
    Task<Result<List<CustomerReceiptDto>>> GetAllAsync(bool includeInactive = false);
    Task<Result<CustomerReceiptDto>> GetByIdAsync(int id);
    Task<Result<CustomerReceiptDto>> CreateAsync(CreateCustomerReceiptRequest request);
    Task<Result<CustomerReceiptDto>> PostAsync(int id);
    Task<Result> CancelAsync(int id);
    Task<Result<CustomerReceiptDto>> AddApplicationAsync(int id, AddReceiptApplicationRequest request);
}

public interface IAttachmentApiService
{
    Task<Result<List<AttachmentDto>>> GetAllAsync(string? referenceType = null, int? referenceId = null);
    Task<Result<AttachmentDto>> GetByIdAsync(int id);
    Task<Result<AttachmentDto>> CreateAsync(CreateAttachmentRequest request);
    Task<Result<AttachmentDto>> UpdateAsync(int id, UpdateAttachmentRequest request);
    Task<Result> DeleteAsync(int id);
}

public interface ISupplierPaymentApplicationApiService
{
    Task<Result<List<SupplierPaymentApplicationDto>>> GetAllAsync(int? supplierPaymentId = null, int? purchaseInvoiceId = null);
    Task<Result<SupplierPaymentApplicationDto>> GetByIdAsync(int id);
    Task<Result<SupplierPaymentApplicationDto>> CreateAsync(CreateSupplierPaymentApplicationRequest request);
    Task<Result> DeleteAsync(int id);
}

public interface IRoleApiService
{
    Task<Result<List<RoleDto>>> GetAllAsync(bool includeInactive = false, CancellationToken ct = default);
    Task<Result<RoleDto>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Result<RoleDto>> CreateAsync(CreateRoleRequest request, CancellationToken ct = default);
    Task<Result<RoleDto>> UpdateAsync(int id, UpdateRoleRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(int id, CancellationToken ct = default);
}

public interface IUserSessionApiService
{
    Task<Result<List<UserSessionDto>>> GetAllAsync(int? userId = null, bool includeRevoked = false, CancellationToken ct = default);
    Task<Result> RevokeAsync(long sessionId, CancellationToken ct = default);
}


public interface ICustomerContactApiService
{
    Task<Result<List<CustomerContactDto>>> GetAllAsync(int customerId);
    Task<Result<CustomerContactDto>> GetByIdAsync(int id);
    Task<Result<CustomerContactDto>> CreateAsync(CreateCustomerContactRequest request);
    Task<Result<CustomerContactDto>> UpdateAsync(int id, UpdateCustomerContactRequest request);
    Task<Result> DeactivateAsync(int id);
}

public interface ISupplierContactApiService
{
    Task<Result<List<SupplierContactDto>>> GetAllAsync(int supplierId);
    Task<Result<SupplierContactDto>> GetByIdAsync(int id);
    Task<Result<SupplierContactDto>> CreateAsync(CreateSupplierContactRequest request);
    Task<Result<SupplierContactDto>> UpdateAsync(int id, UpdateSupplierContactRequest request);
    Task<Result> DeactivateAsync(int id);
}

public interface ICompanySettingsApiService
{
    Task<Result<CompanySettingsDto>> GetAsync(CancellationToken ct = default);
    Task<Result<CompanySettingsDto>> UpdateAsync(UpdateCompanySettingsRequest request, CancellationToken ct = default);
}

public interface IDocumentSequenceApiService
{
    Task<Result<List<DocumentSequenceDto>>> GetAllAsync(CancellationToken ct = default);
    Task<Result<DocumentSequenceDto>> UpdateAsync(int id, UpdateDocumentSequenceRequest request, CancellationToken ct = default);
}

public interface IAccountCategoryApiService
{
    Task<Result<List<AccountCategoryDto>>> GetAllAsync(CancellationToken ct = default);
    Task<Result<AccountCategoryDto>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Result<AccountCategoryDto>> CreateAsync(CreateAccountCategoryRequest request, CancellationToken ct = default);
    Task<Result<AccountCategoryDto>> UpdateAsync(int id, UpdateAccountCategoryRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(int id, CancellationToken ct = default);
}

public interface ISystemAccountMappingApiService
{
    Task<Result<List<SystemAccountMappingDto>>> GetAllAsync(int? branchId = null, CancellationToken ct = default);
    Task<Result<SystemAccountMappingDto>> GetByKeyAsync(string key, int? branchId = null, CancellationToken ct = default);
    Task<Result<SystemAccountMappingDto>> CreateAsync(CreateSystemAccountMappingRequest request, CancellationToken ct = default);
    Task<Result<SystemAccountMappingDto>> UpdateAsync(int id, UpdateSystemAccountMappingRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(int id, CancellationToken ct = default);
}

public interface IPaymentVoucherApiService
{
    Task<Result<List<PaymentVoucherDto>>> GetAllAsync(string? search = null, DateTime? from = null, DateTime? to = null, int page = 1, int pageSize = 100, CancellationToken ct = default);
    Task<Result<PaymentVoucherDto>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Result<PaymentVoucherDto>> CreateAsync(CreatePaymentVoucherRequest request, CancellationToken ct = default);
    Task<Result<PaymentVoucherDto>> UpdateAsync(int id, UpdatePaymentVoucherRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(int id, CancellationToken ct = default);
    Task<Result<PaymentVoucherDto>> PostAsync(int id, CancellationToken ct = default);
    Task<Result<PaymentVoucherDto>> CancelAsync(int id, CancellationToken ct = default);
}



