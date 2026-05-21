using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Tests.Controllers;

/// <summary>
/// Base class for API Controller tests providing common mocks and helper methods
/// </summary>
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
    }

    #region Helper Methods

    protected static Result<T> CreateSuccessResult<T>(T value) where T : class
        => Result<T>.Success(value);

    protected static Result<T> CreateFailureResult<T>(string error) where T : class
        => Result<T>.Failure(error);

    protected static Result CreateSuccessResult()
        => Result.Success();

    protected static Result CreateFailureResult(string error)
        => Result.Failure(error);

    /// <summary>
    /// Sets User.Id via reflection for controller testing
    /// </summary>
    protected static void SetupUserId(object controller, int userId)
    {
        var controllerWithUser = controller as ControllerBase;
        controllerWithUser?.ControllerContext.HttpContext.Items["UserId"] = userId;
    }

    /// <summary>
    /// Sets controller without UserId
    /// </summary>
    protected static void SetupUserWithoutId(object controller)
    {
        var controllerWithUser = controller as ControllerBase;
        controllerWithUser?.ControllerContext.HttpContext.Items.Remove("UserId");
    }

    #endregion
}
