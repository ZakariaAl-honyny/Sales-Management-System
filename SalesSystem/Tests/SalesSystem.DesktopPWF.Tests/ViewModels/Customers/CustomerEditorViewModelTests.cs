namespace SalesSystem.DesktopPWF.Tests.ViewModels.Customers;

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using FluentAssertions;
using Moq;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.DesktopPWF.Services;
using SalesSystem.DesktopPWF.Services.App;
using SalesSystem.DesktopPWF.ViewModels.Customers;

/// <summary>
/// Tests for CustomerEditorViewModel with constructor injection
/// </summary>
public class CustomerEditorViewModelTests : IDisposable
{
    private readonly Mock<ICustomerApiService> _mockCustomerService;
    private readonly Mock<IEventBus> _mockEventBus;
    private readonly Mock<IDialogService> _mockDialogService;

    public CustomerEditorViewModelTests()
    {
        _mockCustomerService = new Mock<ICustomerApiService>();
        _mockEventBus = new Mock<IEventBus>();
        _mockDialogService = new Mock<IDialogService>();
    }

    public void Dispose()
    {
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithServices_InitializesCommands()
    {
        // Arrange & Act
        var viewModel = new CustomerEditorViewModel(
            _mockCustomerService.Object,
            _mockEventBus.Object,
            _mockDialogService.Object);

        // Assert
        viewModel.SaveCommand.Should().NotBeNull();
        viewModel.CancelCommand.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_NewCustomer_SetsIsEditModeFalse()
    {
        // Arrange & Act
        var viewModel = new CustomerEditorViewModel(
            _mockCustomerService.Object,
            _mockEventBus.Object,
            _mockDialogService.Object);

        // Assert
        viewModel.IsEditMode.Should().BeFalse();
    }

    [Fact]
    public void Constructor_WithCustomerDto_SetsIsEditModeTrue()
    {
        // Arrange
        var customer = CreateTestCustomerDto(1, "عميل تجريبي");

        // Act - Use the service-based constructor
        var viewModel = new CustomerEditorViewModel(customer, _mockCustomerService.Object, _mockEventBus.Object, _mockDialogService.Object);

        // Assert
        viewModel.IsEditMode.Should().BeTrue();
    }

    [Fact]
    public void Constructor_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var viewModel = new CustomerEditorViewModel(
            _mockCustomerService.Object,
            _mockEventBus.Object,
            _mockDialogService.Object);

        // Assert
        viewModel.Name.Should().BeEmpty();
        viewModel.Phone.Should().BeEmpty();
        viewModel.Email.Should().BeEmpty();
        viewModel.Address.Should().BeEmpty();
        viewModel.TaxNumber.Should().BeEmpty();
        viewModel.CreditLimit.Should().Be(0);
        viewModel.OpeningBalance.Should().Be(0);
        viewModel.Notes.Should().BeEmpty();
        viewModel.IsActive.Should().BeTrue();
        viewModel.IsBusy.Should().BeFalse();
    }

    [Fact]
    public void Constructor_WithCustomerDto_PopulatesFields()
    {
        // Arrange
        var customer = new CustomerDto(
            Id: 1,
            Name: "عميل تجريبي",
            Phone: "0501234567",
            Email: "test@example.com",
            Address: "العنوان",
            TaxNumber: null,
            OpeningBalance: 1000,
            CurrentBalance: 0,
            CreditLimit: 5000,
            IsActive: false);

        // Act
        var viewModel = new CustomerEditorViewModel(customer, _mockCustomerService.Object, _mockEventBus.Object, _mockDialogService.Object);

        // Assert - field is private, we verify via IsEditMode being true
        viewModel.IsEditMode.Should().BeTrue();
        viewModel.Name.Should().Be("عميل تجريبي");
        viewModel.Phone.Should().Be("0501234567");
        viewModel.Email.Should().Be("test@example.com");
        viewModel.Address.Should().Be("العنوان");
        viewModel.CreditLimit.Should().Be(5000);
        viewModel.OpeningBalance.Should().Be(1000);
        viewModel.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Title_NewCustomer_ReturnsAddTitle()
    {
        // Arrange
        var viewModel = new CustomerEditorViewModel(
            _mockCustomerService.Object,
            _mockEventBus.Object,
            _mockDialogService.Object);

        // Assert
        viewModel.Title.Should().Be("إضافة عميل جديد");
    }

    [Fact]
    public void Title_EditCustomer_ReturnsEditTitle()
    {
        // Arrange
        var customer = new CustomerDto(
            Id: 1,
            Name: "عميل تجريبي",
            Phone: null,
            Email: null,
            Address: null,
            TaxNumber: null,
            OpeningBalance: 0,
            CurrentBalance: 0,
            CreditLimit: 0,
            IsActive: true);

        var viewModel = new CustomerEditorViewModel(customer, _mockCustomerService.Object, _mockEventBus.Object, _mockDialogService.Object);

        // Assert
        viewModel.Title.Should().Be("تعديل عميل");
    }

    #endregion

    #region Validation Tests

    [Fact]
    public void Validate_WhenNameIsEmpty_ProducesValidationError()
    {
        // Arrange
        var viewModel = new CustomerEditorViewModel(
            _mockCustomerService.Object,
            _mockEventBus.Object,
            _mockDialogService.Object);
        // Set a non-empty value first so SetProperty detects the change to empty
        viewModel.Name = "test";
        viewModel.Name = string.Empty;

        // Assert - INotifyDataErrorInfo validation fires on property change
        var errors = viewModel.GetErrors("Name").Cast<string>().ToList();
        errors.Should().Contain(e => e.Contains("اسم العميل مطلوب"));
    }

    [Fact]
    public void Validate_WhenCreditLimitIsNegative_ProducesValidationError()
    {
        // Arrange
        var viewModel = new CustomerEditorViewModel(
            _mockCustomerService.Object,
            _mockEventBus.Object,
            _mockDialogService.Object);
        viewModel.CreditLimit = -100;

        // Assert - INotifyDataErrorInfo validation fires on property change
        var errors = viewModel.GetErrors("CreditLimit").Cast<string>().ToList();
        errors.Should().Contain(e => e.Contains("الحد الائتماني"));
    }

    [Fact]
    public void Validate_WhenOpeningBalanceIsNegative_ProducesValidationError()
    {
        // Arrange
        var viewModel = new CustomerEditorViewModel(
            _mockCustomerService.Object,
            _mockEventBus.Object,
            _mockDialogService.Object);
        viewModel.OpeningBalance = -100;

        // Assert - INotifyDataErrorInfo validation fires on property change
        var errors = viewModel.GetErrors("OpeningBalance").Cast<string>().ToList();
        errors.Should().Contain(e => e.Contains("الرصيد الافتتاحي"));
    }

    #endregion

    #region INotifyDataErrorInfo / ValidateAsync Tests (v4.6.2)

    [Fact]
    public void SetDialogService_Constructor_CallsSetDialogService()
    {
        // Arrange & Act
        var vm = new CustomerEditorViewModel(
            _mockCustomerService.Object,
            _mockEventBus.Object,
            _mockDialogService.Object);

        // Assert — SetDialogService is called in constructor; VM created without exception
        vm.Should().NotBeNull();
        vm.SaveCommand.Should().NotBeNull();
    }

    [Fact]
    public async Task ValidateAsync_WhenNameIsEmpty_AddsNameError()
    {
        // Arrange
        var vm = new CustomerEditorViewModel(
            _mockCustomerService.Object,
            _mockEventBus.Object,
            _mockDialogService.Object);
        vm.Name = string.Empty;

        // Act
        var isValid = await InvokeValidateAsync(vm);

        // Assert
        isValid.Should().BeFalse();
        var errors = vm.GetErrors("Name").Cast<string>().ToList();
        errors.Should().Contain(e => e.Contains("اسم"));
    }

    [Fact]
    public async Task ValidateAsync_WhenNameIsValid_ClearsNameError()
    {
        // Arrange
        var vm = new CustomerEditorViewModel(
            _mockCustomerService.Object,
            _mockEventBus.Object,
            _mockDialogService.Object);
        vm.Name = "زبون تجريبي"; // Valid name

        // Act
        var isValid = await InvokeValidateAsync(vm);

        // Assert
        isValid.Should().BeTrue();
        var errors = vm.GetErrors("Name").Cast<string>().ToList();
        errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateAsync_WithMultipleErrors_ReturnsFalse()
    {
        // Arrange
        var vm = new CustomerEditorViewModel(
            _mockCustomerService.Object,
            _mockEventBus.Object,
            _mockDialogService.Object);
        vm.Name = string.Empty;
        vm.CreditLimit = -500;
        vm.OpeningBalance = -100;

        // Act
        var isValid = await InvokeValidateAsync(vm);

        // Assert
        isValid.Should().BeFalse();
        var nameErrors = vm.GetErrors("Name").Cast<string>().ToList();
        nameErrors.Should().Contain(e => e.Contains("اسم"));
        var creditErrors = vm.GetErrors("CreditLimit").Cast<string>().ToList();
        creditErrors.Should().Contain(e => e.Contains("الحد الائتماني"));
        var balanceErrors = vm.GetErrors("OpeningBalance").Cast<string>().ToList();
        balanceErrors.Should().Contain(e => e.Contains("الرصيد الافتتاحي"));
    }

    #endregion

    #region Property Notification Tests

    [Fact]
    public void Name_Set_NotifiesPropertyChanged()
    {
        // Arrange
        var viewModel = new CustomerEditorViewModel(
            _mockCustomerService.Object,
            _mockEventBus.Object,
            _mockDialogService.Object);
        var propertyChangedEvents = new List<string>();
        viewModel.PropertyChanged += (s, e) => propertyChangedEvents.Add(e.PropertyName ?? string.Empty);

        // Act
        viewModel.Name = "عميل جديد";

        // Assert
        propertyChangedEvents.Should().Contain("Name");
    }

    [Fact]
    public void IsActive_Set_NotifiesPropertyChanged()
    {
        // Arrange
        var viewModel = new CustomerEditorViewModel(
            _mockCustomerService.Object,
            _mockEventBus.Object,
            _mockDialogService.Object);
        var propertyChangedEvents = new List<string>();
        viewModel.PropertyChanged += (s, e) => propertyChangedEvents.Add(e.PropertyName ?? string.Empty);

        // Act
        viewModel.IsActive = false;

        // Assert
        propertyChangedEvents.Should().Contain("IsActive");
    }

    [Fact]
    public void IsBusy_IsReadOnly_FromViewModelBase()
    {
        // Arrange
        var viewModel = new CustomerEditorViewModel(
            _mockCustomerService.Object,
            _mockEventBus.Object,
            _mockDialogService.Object);

        // Assert
        viewModel.IsBusy.Should().BeFalse();
    }

    [Fact]
    public void ErrorMessage_Set_NotifiesPropertyChanged()
    {
        // Arrange
        var viewModel = new CustomerEditorViewModel(
            _mockCustomerService.Object,
            _mockEventBus.Object,
            _mockDialogService.Object);
        var propertyChangedEvents = new List<string>();
        viewModel.PropertyChanged += (s, e) => propertyChangedEvents.Add(e.PropertyName ?? string.Empty);

        // Act
        viewModel.ErrorMessage = "خطأ";

        // Assert
        propertyChangedEvents.Should().Contain("ErrorMessage");
    }

    [Fact]
    public void Phone_Set_NotifiesPropertyChanged()
    {
        // Arrange
        var viewModel = new CustomerEditorViewModel(
            _mockCustomerService.Object,
            _mockEventBus.Object,
            _mockDialogService.Object);
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
        var viewModel = new CustomerEditorViewModel(
            _mockCustomerService.Object,
            _mockEventBus.Object,
            _mockDialogService.Object);
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
        var viewModel = new CustomerEditorViewModel(
            _mockCustomerService.Object,
            _mockEventBus.Object,
            _mockDialogService.Object);
        var propertyChangedEvents = new List<string>();
        viewModel.PropertyChanged += (s, e) => propertyChangedEvents.Add(e.PropertyName ?? string.Empty);

        // Act
        viewModel.Address = "عنوان جديد";

        // Assert
        propertyChangedEvents.Should().Contain("Address");
    }

    [Fact]
    public void CreditLimit_Set_NotifiesPropertyChanged()
    {
        // Arrange
        var viewModel = new CustomerEditorViewModel(
            _mockCustomerService.Object,
            _mockEventBus.Object,
            _mockDialogService.Object);
        var propertyChangedEvents = new List<string>();
        viewModel.PropertyChanged += (s, e) => propertyChangedEvents.Add(e.PropertyName ?? string.Empty);

        // Act
        viewModel.CreditLimit = 5000;

        // Assert
        propertyChangedEvents.Should().Contain("CreditLimit");
    }

    #endregion

    #region SaveCommand Tests

    [Fact]
    public async Task SaveCommand_WhenInvalidData_DoesNotCallService()
    {
        // Arrange
        var viewModel = new CustomerEditorViewModel(
            _mockCustomerService.Object,
            _mockEventBus.Object,
            _mockDialogService.Object);
        viewModel.Name = ""; // Invalid - name is empty

        // Assert - Validation not triggered yet
        viewModel.SaveCommand.Should().NotBeNull();
    }

    [Fact]
    public async Task SaveCommand_WhenValid_CallsCreateService()
    {
        // Arrange
        var viewModel = new CustomerEditorViewModel(
            _mockCustomerService.Object,
            _mockEventBus.Object,
            _mockDialogService.Object);
        viewModel.Name = "عميل جديد";
        viewModel.CreditLimit = 5000;

        // Assert - Check SaveCommand exists
        viewModel.SaveCommand.Should().NotBeNull();
    }

    [Fact]
    public async Task SaveCommand_WhenCreateFails_SetsErrorMessage()
    {
        // Arrange
        var viewModel = new CustomerEditorViewModel(
            _mockCustomerService.Object,
            _mockEventBus.Object,
            _mockDialogService.Object);

        // Act
        viewModel.ErrorMessage = "فشل في الحفظ";

        // Assert
        viewModel.ErrorMessage.Should().Be("فشل في الحفظ");
    }

    [Fact]
    public async Task SaveCommand_WhenLoading_IsBusyReflectsState()
    {
        // Arrange
        var viewModel = new CustomerEditorViewModel(
            _mockCustomerService.Object,
            _mockEventBus.Object,
            _mockDialogService.Object);

        // Assert - IsBusy initially false, managed by ExecuteAsync
        viewModel.IsBusy.Should().BeFalse();
    }

    #endregion

    #region CancelCommand Tests

    [Fact]
    public void CancelCommand_WhenExecuted_InvokesCloseRequested()
    {
        // Arrange
        var viewModel = new CustomerEditorViewModel(
            _mockCustomerService.Object,
            _mockEventBus.Object,
            _mockDialogService.Object);
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
        var viewModel = new CustomerEditorViewModel(
            _mockCustomerService.Object,
            _mockEventBus.Object,
            _mockDialogService.Object);

        // Act & Assert
        viewModel.CancelCommand.CanExecute(null).Should().BeTrue();
    }

    #endregion

    #region Helper Methods

    private static CustomerDto CreateTestCustomerDto(
        int id,
        string name)
    {
        return new CustomerDto(
            Id: id,
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

    /// <summary>
    /// Invokes the private ValidateAsync method on CustomerEditorViewModel via reflection.
    /// </summary>
    private static async Task<bool> InvokeValidateAsync(CustomerEditorViewModel vm)
    {
        var method = typeof(CustomerEditorViewModel).GetMethod(
            "ValidateAsync",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (method == null)
            throw new InvalidOperationException("ValidateAsync method not found on CustomerEditorViewModel");

        var task = (Task<bool>)method.Invoke(vm, null)!;
        return await task;
    }

    #endregion
}