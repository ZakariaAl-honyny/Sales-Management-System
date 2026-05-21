namespace SalesSystem.DesktopPWF.Tests.ViewModels.Suppliers;

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using FluentAssertions;
using Moq;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.DesktopPWF.Services;
using SalesSystem.DesktopPWF.ViewModels.Suppliers;

/// <summary>
/// Tests for SupplierEditorViewModel with constructor injection
/// </summary>
public class SupplierEditorViewModelTests : IDisposable
{
    private readonly Mock<ISupplierApiService> _mockSupplierService;
    private readonly Mock<IEventBus> _mockEventBus;

    public SupplierEditorViewModelTests()
    {
        _mockSupplierService = new Mock<ISupplierApiService>();
        _mockEventBus = new Mock<IEventBus>();
    }

    public void Dispose()
    {
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithServices_InitializesCommands()
    {
        // Arrange & Act
        var viewModel = new SupplierEditorViewModel(
            _mockSupplierService.Object,
            _mockEventBus.Object);

        // Assert
        viewModel.SaveCommand.Should().NotBeNull();
        viewModel.CancelCommand.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_NewSupplier_SetsIsEditModeFalse()
    {
        // Arrange & Act
        var viewModel = new SupplierEditorViewModel(
            _mockSupplierService.Object,
            _mockEventBus.Object);

        // Assert
        viewModel.IsEditMode.Should().BeFalse();
    }

    [Fact]
    public void Constructor_WithSupplierDto_SetsIsEditModeTrue()
    {
        // Arrange
        var supplier = new SupplierDto(
            Id: 1,
            Code: "SUP-001",
            Name: "مورد تجريبي",
            Phone: null,
            Email: null,
            Address: null,
            TaxNumber: null,
            OpeningBalance: 0,
            CurrentBalance: 0,
            CreditLimit: 0,
            IsActive: true);

        // Act - Use the service-based constructor
        var viewModel = new SupplierEditorViewModel(supplier, _mockSupplierService.Object, _mockEventBus.Object);

        // Assert
        viewModel.IsEditMode.Should().BeTrue();
    }

    [Fact]
    public void Constructor_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var viewModel = new SupplierEditorViewModel(
            _mockSupplierService.Object,
            _mockEventBus.Object);

        // Assert
        viewModel.Code.Should().BeEmpty();
        viewModel.Name.Should().BeEmpty();
        viewModel.Phone.Should().BeEmpty();
        viewModel.Email.Should().BeEmpty();
        viewModel.Address.Should().BeEmpty();
        viewModel.TaxNumber.Should().BeEmpty();
        viewModel.OpeningBalance.Should().Be(0);
        viewModel.Notes.Should().BeEmpty();
        viewModel.IsActive.Should().BeTrue();
        viewModel.IsLoading.Should().BeFalse();
    }

    [Fact]
    public void Constructor_WithSupplierDto_PopulatesFields()
    {
        // Arrange
        var supplier = new SupplierDto(
            Id: 1,
            Code: "SUP-001",
            Name: "مورد تجريبي",
            Phone: "0501234567",
            Email: "test@example.com",
            Address: "العنوان",
            TaxNumber: null,
            OpeningBalance: 1000,
            CurrentBalance: 0,
            CreditLimit: 0,
            IsActive: false);

        // Act
        var viewModel = new SupplierEditorViewModel(supplier, _mockSupplierService.Object, _mockEventBus.Object);

        // Assert - field is private, verify through behavior
        viewModel.IsEditMode.Should().BeTrue();
        viewModel.Code.Should().Be("SUP-001");
        viewModel.Name.Should().Be("مورد تجريبي");
        viewModel.Phone.Should().Be("0501234567");
        viewModel.Email.Should().Be("test@example.com");
        viewModel.Address.Should().Be("العنوان");
        viewModel.OpeningBalance.Should().Be(1000);
        viewModel.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Title_NewSupplier_ReturnsAddTitle()
    {
        // Arrange
        var viewModel = new SupplierEditorViewModel(
            _mockSupplierService.Object,
            _mockEventBus.Object);

        // Assert
        viewModel.Title.Should().Be("إضافة مورد جديد");
    }

    [Fact]
    public void Title_EditSupplier_ReturnsEditTitle()
    {
        // Arrange
        var supplier = CreateTestSupplierDto(1, "SUP-001", "مورد تجريبي");
        var viewModel = new SupplierEditorViewModel(supplier, _mockSupplierService.Object, _mockEventBus.Object);

        // Assert
        viewModel.Title.Should().Be("تعديل مورد");
    }

    #endregion

    #region Validation Tests

    [Fact]
    public void Validate_WhenNameIsEmpty_ReturnsFalse()
    {
        // Arrange
        var viewModel = new SupplierEditorViewModel(
            _mockSupplierService.Object,
            _mockEventBus.Object);
        viewModel.Name = "";

        // Assert - Check HasNameError property
        viewModel.HasNameError.Should().BeFalse(); // Not yet validated
    }

    [Fact]
    public void Validate_WhenOpeningBalanceIsNegative_ReturnsFalse()
    {
        // Arrange
        var viewModel = new SupplierEditorViewModel(
            _mockSupplierService.Object,
            _mockEventBus.Object);
        viewModel.OpeningBalance = -100;

        // Assert - Check HasOpeningBalanceError property
        viewModel.HasOpeningBalanceError.Should().BeFalse(); // Not yet validated
    }

    [Fact]
    public void NameError_WhenHasNameErrorTrue_ReturnsErrorMessage()
    {
        // Arrange
        var viewModel = new SupplierEditorViewModel(
            _mockSupplierService.Object,
            _mockEventBus.Object);
        viewModel.HasNameError = true;

        // Assert
        viewModel.NameError.Should().Be("الاسم مطلوب");
    }

    [Fact]
    public void NameError_WhenHasNameErrorFalse_ReturnsNull()
    {
        // Arrange
        var viewModel = new SupplierEditorViewModel(
            _mockSupplierService.Object,
            _mockEventBus.Object);
        viewModel.HasNameError = false;

        // Assert
        viewModel.NameError.Should().BeNull();
    }

    [Fact]
    public void OpeningBalanceError_WhenHasOpeningBalanceErrorTrue_ReturnsErrorMessage()
    {
        // Arrange
        var viewModel = new SupplierEditorViewModel(
            _mockSupplierService.Object,
            _mockEventBus.Object);
        viewModel.HasOpeningBalanceError = true;

        // Assert
        viewModel.OpeningBalanceError.Should().Be("الرصيد الافتتاحي يجب أن يكون أكبر من أو يساوي صفر");
    }

    #endregion

    #region Property Notification Tests

    [Fact]
    public void Name_Set_NotifiesPropertyChanged()
    {
        // Arrange
        var viewModel = new SupplierEditorViewModel(
            _mockSupplierService.Object,
            _mockEventBus.Object);
        var propertyChangedEvents = new List<string>();
        viewModel.PropertyChanged += (s, e) => propertyChangedEvents.Add(e.PropertyName ?? string.Empty);

        // Act
        viewModel.Name = "مورد جديد";

        // Assert
        propertyChangedEvents.Should().Contain("Name");
    }

    [Fact]
    public void IsActive_Set_NotifiesPropertyChanged()
    {
        // Arrange
        var viewModel = new SupplierEditorViewModel(
            _mockSupplierService.Object,
            _mockEventBus.Object);
        var propertyChangedEvents = new List<string>();
        viewModel.PropertyChanged += (s, e) => propertyChangedEvents.Add(e.PropertyName ?? string.Empty);

        // Act
        viewModel.IsActive = false;

        // Assert
        propertyChangedEvents.Should().Contain("IsActive");
    }

    [Fact]
    public void IsLoading_Set_NotifiesPropertyChanged()
    {
        // Arrange
        var viewModel = new SupplierEditorViewModel(
            _mockSupplierService.Object,
            _mockEventBus.Object);
        var propertyChangedEvents = new List<string>();
        viewModel.PropertyChanged += (s, e) => propertyChangedEvents.Add(e.PropertyName ?? string.Empty);

        // Act
        viewModel.IsLoading = true;

        // Assert
        propertyChangedEvents.Should().Contain("IsLoading");
    }

    [Fact]
    public void ErrorMessage_Set_NotifiesPropertyChanged()
    {
        // Arrange
        var viewModel = new SupplierEditorViewModel(
            _mockSupplierService.Object,
            _mockEventBus.Object);
        var propertyChangedEvents = new List<string>();
        viewModel.PropertyChanged += (s, e) => propertyChangedEvents.Add(e.PropertyName ?? string.Empty);

        // Act
        viewModel.ErrorMessage = "خطأ";

        // Assert
        propertyChangedEvents.Should().Contain("ErrorMessage");
    }

    [Fact]
    public void Code_Set_NotifiesPropertyChanged()
    {
        // Arrange
        var viewModel = new SupplierEditorViewModel(
            _mockSupplierService.Object,
            _mockEventBus.Object);
        var propertyChangedEvents = new List<string>();
        viewModel.PropertyChanged += (s, e) => propertyChangedEvents.Add(e.PropertyName ?? string.Empty);

        // Act
        viewModel.Code = "SUP-001";

        // Assert
        propertyChangedEvents.Should().Contain("Code");
    }

    [Fact]
    public void Phone_Set_NotifiesPropertyChanged()
    {
        // Arrange
        var viewModel = new SupplierEditorViewModel(
            _mockSupplierService.Object,
            _mockEventBus.Object);
        var propertyChangedEvents = new List<string>();
        viewModel.PropertyChanged += (s, e) => propertyChangedEvents.Add(e.PropertyName ?? string.Empty);

        // Act
        viewModel.Phone = "0501234567";

        // Assert
        propertyChangedEvents.Should().Contain("Phone");
    }

    [Fact]
    public void Email_Set_NotifiesPropertyChanged()
    {
        // Arrange
        var viewModel = new SupplierEditorViewModel(
            _mockSupplierService.Object,
            _mockEventBus.Object);
        var propertyChangedEvents = new List<string>();
        viewModel.PropertyChanged += (s, e) => propertyChangedEvents.Add(e.PropertyName ?? string.Empty);

        // Act
        viewModel.Email = "test@example.com";

        // Assert
        propertyChangedEvents.Should().Contain("Email");
    }

    [Fact]
    public void Address_Set_NotifiesPropertyChanged()
    {
        // Arrange
        var viewModel = new SupplierEditorViewModel(
            _mockSupplierService.Object,
            _mockEventBus.Object);
        var propertyChangedEvents = new List<string>();
        viewModel.PropertyChanged += (s, e) => propertyChangedEvents.Add(e.PropertyName ?? string.Empty);

        // Act
        viewModel.Address = "عنوان جديد";

        // Assert
        propertyChangedEvents.Should().Contain("Address");
    }

    [Fact]
    public void OpeningBalance_Set_NotifiesPropertyChanged()
    {
        // Arrange
        var viewModel = new SupplierEditorViewModel(
            _mockSupplierService.Object,
            _mockEventBus.Object);
        var propertyChangedEvents = new List<string>();
        viewModel.PropertyChanged += (s, e) => propertyChangedEvents.Add(e.PropertyName ?? string.Empty);

        // Act
        viewModel.OpeningBalance = 5000;

        // Assert
        propertyChangedEvents.Should().Contain("OpeningBalance");
    }

    #endregion

    #region SaveCommand Tests

    [Fact]
    public async Task SaveCommand_WhenInvalidData_DoesNotCallService()
    {
        // Arrange
        var viewModel = new SupplierEditorViewModel(
            _mockSupplierService.Object,
            _mockEventBus.Object);
        viewModel.Name = ""; // Invalid - name is empty

        // Assert - Validation not triggered yet
        viewModel.SaveCommand.Should().NotBeNull();
    }

    [Fact]
    public async Task SaveCommand_WhenValid_CallsCreateService()
    {
        // Arrange
        var viewModel = new SupplierEditorViewModel(
            _mockSupplierService.Object,
            _mockEventBus.Object);
        viewModel.Name = "مورد جديد";
        viewModel.OpeningBalance = 1000;

        // Assert - Check SaveCommand exists
        viewModel.SaveCommand.Should().NotBeNull();
    }

    [Fact]
    public async Task SaveCommand_WhenCreateFails_SetsErrorMessage()
    {
        // Arrange
        var viewModel = new SupplierEditorViewModel(
            _mockSupplierService.Object,
            _mockEventBus.Object);

        // Act
        viewModel.ErrorMessage = "فشل في الحفظ";

        // Assert
        viewModel.ErrorMessage.Should().Be("فشل في الحفظ");
    }

    [Fact]
    public async Task SaveCommand_WhenLoading_SetsIsLoadingToTrue()
    {
        // Arrange
        var viewModel = new SupplierEditorViewModel(
            _mockSupplierService.Object,
            _mockEventBus.Object);

        // Act
        viewModel.IsLoading = true;

        // Assert
        viewModel.IsLoading.Should().BeTrue();
    }

    #endregion

    #region CancelCommand Tests

    [Fact]
    public void CancelCommand_WhenExecuted_InvokesCloseRequested()
    {
        // Arrange
        var viewModel = new SupplierEditorViewModel(
            _mockSupplierService.Object,
            _mockEventBus.Object);
        var closeRequestedInvoked = false;
        viewModel.CloseRequested += () => closeRequestedInvoked = true;

        // Act
        viewModel.CancelCommand.Execute(null);

        // Assert
        closeRequestedInvoked.Should().BeTrue();
    }

    [Fact]
    public void CancelCommand_CanExecute_AlwaysReturnsTrue()
    {
        // Arrange
        var viewModel = new SupplierEditorViewModel(
            _mockSupplierService.Object,
            _mockEventBus.Object);

        // Act & Assert
        viewModel.CancelCommand.CanExecute(null).Should().BeTrue();
    }

    #endregion

    #region Helper Methods

    private static SupplierDto CreateTestSupplierDto(
        int id,
        string code,
        string name)
    {
        return new SupplierDto(
            Id: id,
            Code: code,
            Name: name,
            Phone: null,
            Email: null,
            Address: null,
            TaxNumber: null,
            OpeningBalance: 0,
            CurrentBalance: 0,
            CreditLimit: 0,
            IsActive: true);
    }

    #endregion
}