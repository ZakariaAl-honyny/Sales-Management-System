using SalesSystem.DesktopPWF.Messaging.Messages;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Enums;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using SalesSystem.DesktopPWF.Helpers;
using System.ComponentModel;
using System.Windows.Data;

namespace SalesSystem.DesktopPWF.ViewModels.Payments;

/// <summary>
/// ViewModel for Customer Payments List
/// </summary>
public class CustomerPaymentsListViewModel : ViewModelBase
{
    private readonly ICustomerPaymentApiService _paymentService;
    private readonly ICustomerApiService _customerService;
    private readonly IDialogService _dialogService;
    private readonly INavigationService _navigationService;
    private readonly IPaymentPrinter _paymentPrinter;
    private readonly ISettingsApiService _settingsService;

    private string _searchText = string.Empty;
    private DateTime? _dateFrom;
    private DateTime? _dateTo;
    private bool _isLoading;
    private string _errorMessage = string.Empty;
    private bool _isEmpty;
    private CustomerPaymentDto? _selectedPayment;
    private ObservableCollection<CustomerPaymentDto> _payments = new();
    private ICollectionView? _paymentsView;

    public CustomerPaymentsListViewModel()
    {
        _paymentService = App.GetService<ICustomerPaymentApiService>();
        _customerService = App.GetService<ICustomerApiService>();
        _dialogService = App.GetService<IDialogService>();
        _navigationService = App.GetService<INavigationService>();
        _paymentPrinter = App.GetService<IPaymentPrinter>();
        _settingsService = App.GetService<ISettingsApiService>();

        NewCommand = new RelayCommand(OnNew);
        ViewCommand = new RelayCommand(OnView, () => SelectedPayment != null);
        EditCommand = new RelayCommand(OnEdit, () => SelectedPayment != null);
        DeleteCommand = new AsyncRelayCommand(OnDelete, () => SelectedPayment != null);
        PrintCommand = new AsyncRelayCommand(OnPrint, () => SelectedPayment != null);
        RefreshCommand = new AsyncRelayCommand(LoadPaymentsAsync);
        SearchCommand = new AsyncRelayCommand(LoadPaymentsAsync);
    }

    public string SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value);
    }

    public DateTime? DateFrom
    {
        get => _dateFrom;
        set => SetProperty(ref _dateFrom, value);
    }

    public DateTime? DateTo
    {
        get => _dateTo;
        set => SetProperty(ref _dateTo, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public bool IsEmpty
    {
        get => _isEmpty;
        private set => SetProperty(ref _isEmpty, value);
    }

    public CustomerPaymentDto? SelectedPayment
    {
        get => _selectedPayment;
        set
        {
            SetProperty(ref _selectedPayment, value);
            CommandManager.InvalidateRequerySuggested();
        }
    }

    public ObservableCollection<CustomerPaymentDto> Payments
    {
        get => _payments;
        set => SetProperty(ref _payments, value);
    }

    public ICollectionView? PaymentsView
    {
        get => _paymentsView;
        private set => SetProperty(ref _paymentsView, value);
    }

    public int PaymentsCount => Payments.Count;

    public ICommand NewCommand { get; private set; } = null!;
    public ICommand ViewCommand { get; private set; } = null!;
    public ICommand EditCommand { get; private set; } = null!;
    public ICommand DeleteCommand { get; private set; } = null!;
    public ICommand PrintCommand { get; private set; } = null!;
    public ICommand RefreshCommand { get; private set; } = null!;
    public ICommand SearchCommand { get; private set; } = null!;

    public async Task LoadPaymentsAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            var result = await _paymentService.GetAllAsync(SearchText, DateFrom, DateTo);

            if (result.IsSuccess && result.Value != null)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Payments.Clear();
                    foreach (var item in result.Value)
                    {
                        Payments.Add(item);
                    }
                    SetupCollectionView();
                    IsEmpty = Payments.Count == 0;
                    OnPropertyChanged(nameof(PaymentsCount));
                });
            }
            else
            {
                ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل مدفوعات العملاء", "CustomerPaymentsListViewModel.LoadPaymentsAsync", "[CustomerPaymentsListViewModel.LoadPaymentsAsync] Failed to load customer payments list.");
                IsEmpty = Payments.Count == 0;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = HandleException(ex, "CustomerPaymentsListViewModel.LoadPaymentsAsync", "[CustomerPaymentsListViewModel.LoadPaymentsAsync] Failed to load customer payments list.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void SetupCollectionView()
    {
        PaymentsView = CollectionViewSource.GetDefaultView(Payments);
        // Add filtering if needed in the future
    }

    private void OnNew()
    {
        var vm = new CustomerPaymentEditorViewModel();
        if (_dialogService.ShowDialog(vm))
        {
            _ = LoadPaymentsAsync();
        }
    }

    private void OnView()
    {
        if (SelectedPayment == null) return;
        var vm = new CustomerPaymentEditorViewModel(SelectedPayment.Id, isReadOnly: true);
        _dialogService.ShowDialog(vm);
    }

    private void OnEdit()
    {
        if (SelectedPayment == null) return;
        var vm = new CustomerPaymentEditorViewModel(SelectedPayment.Id);
        if (_dialogService.ShowDialog(vm))
        {
            _ = LoadPaymentsAsync();
        }
    }

    private async Task OnDelete()
    {
        if (SelectedPayment == null) return;

        var result = await _dialogService.ShowConfirmationAsync("تأكيد الحذف", "هل أنت متأكد من حذف هذا السداد؟");

        if (!result) return;

        try
        {
            IsLoading = true;
            var deleteResult = await _paymentService.DeleteAsync(SelectedPayment.Id);

            if (deleteResult.IsSuccess)
            {
                await LoadPaymentsAsync();
            }
            else
            {
                ErrorMessage = deleteResult.Error ?? "فشل في حذف السداد";
                await _dialogService.ShowErrorAsync("خطأ في الحذف", ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"حدث خطأ: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task OnPrint()
    {
        if (SelectedPayment == null) return;

        IsLoading = true;
        try
        {
            var settingsResult = await _settingsService.GetSettingsAsync();
            if (!settingsResult.IsSuccess || settingsResult.Value == null) return;

            _paymentPrinter.PrintPreview(SelectedPayment.ToPrintDto(), settingsResult.Value.ToPrintDto());
        }
        catch (Exception ex)
        {
            ErrorMessage = $"خطأ في الطباعة: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
