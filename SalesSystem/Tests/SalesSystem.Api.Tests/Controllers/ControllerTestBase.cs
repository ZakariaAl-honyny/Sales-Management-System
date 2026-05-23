using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Tests.Controllers;

public abstract class ControllerTestBase
{
    #region Mocks

    protected Mock<ISalesService> SalesServiceMock { get; }
    protected Mock<IPurchaseService> PurchaseServiceMock { get; }
    protected Mock<ISalesReturnService> SalesReturnServiceMock { get; }
    protected Mock<IPurchaseReturnService> PurchaseReturnServiceMock { get; }
    protected Mock<IProductService> ProductServiceMock { get; }
    protected Mock<ICustomerService> CustomerServiceMock { get; }
    protected Mock<ISupplierService> SupplierServiceMock { get; }
    protected Mock<ICategoryService> CategoryServiceMock { get; }
    protected Mock<IUnitService> UnitServiceMock { get; }
    protected Mock<IWarehouseService> WarehouseServiceMock { get; }
    protected Mock<IInventoryService> InventoryServiceMock { get; }
    protected Mock<IPaymentService> PaymentServiceMock { get; }
    protected Mock<IReportService> ReportServiceMock { get; }
    protected Mock<IStoreSettingsService> StoreSettingsServiceMock { get; }
    protected Mock<IBackupService> BackupServiceMock { get; }
    protected Mock<IUserService> UserServiceMock { get; }
    protected Mock<IAuthService> AuthServiceMock { get; }

    #endregion

    protected ControllerTestBase()
    {
        SalesServiceMock = new Mock<ISalesService>();
        PurchaseServiceMock = new Mock<IPurchaseService>();
        SalesReturnServiceMock = new Mock<ISalesReturnService>();
        PurchaseReturnServiceMock = new Mock<IPurchaseReturnService>();
        ProductServiceMock = new Mock<IProductService>();
        CustomerServiceMock = new Mock<ICustomerService>();
        SupplierServiceMock = new Mock<ISupplierService>();
        CategoryServiceMock = new Mock<ICategoryService>();
        UnitServiceMock = new Mock<IUnitService>();
        WarehouseServiceMock = new Mock<IWarehouseService>();
        InventoryServiceMock = new Mock<IInventoryService>();
        PaymentServiceMock = new Mock<IPaymentService>();
        ReportServiceMock = new Mock<IReportService>();
        StoreSettingsServiceMock = new Mock<IStoreSettingsService>();
        BackupServiceMock = new Mock<IBackupService>();
        UserServiceMock = new Mock<IUserService>();
        AuthServiceMock = new Mock<IAuthService>();
    }

    #region Helper Methods

    protected static Result<T> CreateSuccessResult<T>(T value)
        => Result<T>.Success(value);

    protected static Result<T> CreateFailureResult<T>(string error)
        => Result<T>.Failure(error);

    protected static Result CreateSuccessResult()
        => Result.Success();

    protected static Result CreateFailureResult(string error)
        => Result.Failure(error);

    protected static void SetupUserId(ControllerBase controller, int userId)
    {
        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext { User = principal }
        };
    }

    protected static void SetupUserWithoutId(ControllerBase controller)
    {
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext { User = new ClaimsPrincipal() }
        };
    }

    #endregion
}
