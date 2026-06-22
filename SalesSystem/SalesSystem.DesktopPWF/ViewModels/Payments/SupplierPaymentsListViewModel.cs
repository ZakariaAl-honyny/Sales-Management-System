using SalesSystem.DesktopPWF.Messaging.Messages;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using SalesSystem.Contracts.DTOs;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using SalesSystem.DesktopPWF.Helpers;
using System.ComponentModel;
using System.Windows.Data;

namespace SalesSystem.DesktopPWF.ViewModels.Payments;

/// <summary>
/// ViewModel for Supplier Payments List
/// </summary>
public class SupplierPaymentsListViewModel : ViewModelBase
{
    private ISupplierPaymentApiService? _paymentService;
    private ISupplierApiService? _supplierService;
    private IPaymentPrinter? _paymentPrinter;
    private ISettingsApiService? _settingsService;
    private IScreenWindowService? _screenWindowService;

    private ISupplierPaymentApiService PaymentService => _paymentService ??= App.GetService<ISupplierPaymentApiService>();
    private ISupplierApiService SupplierService => _supplierService ??= App.GetService<ISupplierApiService>();
    private IPaymentPrinter PaymentPrinter => _paymentPrinter ??= App.GetService<IPaymentPrinter>();
    private ISettingsApiService SettingsService => _settingsService ??= App.GetService<ISettingsApiService>();
    private IScreenWindowService ScreenWindowService => _screenWindowService ??= App.GetService<IScreenWindowService>();

    // Uses 'new' to suppress CS0108 (inherited member hiding).
    // Test uses SetField("_dialogService", mock) before property is accessed.
    private new IDialogService DialogService => _dialogService ??= App.GetService<IDialogService>();
    private IDialogService? _dialogService;

    private string _searchText = string.Empty;
    private DateTime? _dateFrom;
    private DateTime? _dateTo;
    private string _errorMessage = string.Empty;
    private bool _isEmpty;
    private SupplierPaymentDto? _selectedPayment;
    private ObservableCollection<SupplierPaymentDto> _payments = new();
    private ICollectionView? _paymentsView;

    public SupplierPaymentsListViewModel()
    {
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

    public SupplierPaymentDto? SelectedPayment
    {
        get => _selectedPayment;
        set
        {
            SetProperty(ref _selectedPayment, value);
            CommandManager.InvalidateRequerySuggested();
        }
    }

    public ObservableCollection<SupplierPaymentDto> Payments
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
            IsBusy = true;
            ErrorMessage = string.Empty;

            var result = await PaymentService.GetAllAsync(SearchText, DateFrom, DateTo);

            if (result.IsSuccess && result.Value != null)
            {
                InvokeOnUIThread(() =>
                {
                    Payments.Clear();
                    foreach (var item in result.Value.OrderByDescending(x => x.Id))
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
                ErrorMessage = result.Error ?? "حدث خطأ غير معروف";
                await DialogService.ShowErrorAsync("خطأ في تحميل البيانات", ErrorMessage!);
                IsEmpty = Payments.Count == 0;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = HandleException(ex, "SupplierPaymentsListViewModel.LoadPaymentsAsync", "[SupplierPaymentsListViewModel.LoadPaymentsAsync] Failed to load supplier payments list.");
            await DialogService.ShowErrorAsync("خطأ في تحميل البيانات", ErrorMessage!);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void SetupCollectionView()
    {
        PaymentsView = CollectionViewSource.GetDefaultView(Payments);
        // Add filtering if needed in the future
    }

    private void OnNew()
    {
        var vm = App.GetService<SupplierPaymentEditorViewModel>();
        ScreenWindowService.OpenScreen(vm, new ScreenWindowOptions
        {
            Title = "سداد مورد جديد",
            OnClosed = (vm) =>
            {
                System.Windows.Application.Current.Dispatcher.InvokeAsync(() => _ = LoadPaymentsAsync());
            }
        });
    }

    private void OnView()
    {
        if (SelectedPayment == null) return;
        var vm = new SupplierPaymentEditorViewModel(SelectedPayment.Id, isReadOnly: true);
        ScreenWindowService.OpenScreen(vm, new ScreenWindowOptions
        {
            Title = "عرض سداد مورد"
        });
    }

    private void OnEdit()
    {
        if (SelectedPayment == null) return;
        var vm = new SupplierPaymentEditorViewModel(SelectedPayment.Id);
        ScreenWindowService.OpenScreen(vm, new ScreenWindowOptions
        {
            Title = "تعديل سداد مورد",
            OnClosed = (vm) =>
            {
                System.Windows.Application.Current.Dispatcher.InvokeAsync(() => _ = LoadPaymentsAsync());
            }
        });
    }

    private async Task OnDelete()
    {
        if (SelectedPayment == null) return;

        var result = await DialogService.ShowConfirmationAsync("تأكيد الحذف", "هل أنت متأكد من حذف هذا السداد؟");

        if (!result) return;

        try
        {
            IsBusy = true;
            var deleteResult = await PaymentService.DeleteAsync(SelectedPayment.Id);

            if (deleteResult.IsSuccess)
            {
                await LoadPaymentsAsync();
                await DialogService.ShowSuccessAsync("تم الحذف", "تم حذف سند الدفع بنجاح");
            }
            else
            {
                ErrorMessage = deleteResult.Error ?? "فشل في حذف السداد";
                await DialogService.ShowErrorAsync("خطأ في الحذف", ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            LogSystemError($"Failed to delete supplier payment {SelectedPayment?.Id}", "SupplierPaymentsListViewModel.OnDelete", ex);
            ErrorMessage = "حدث خطأ غير متوقع أثناء الحذف";
            await DialogService.ShowErrorAsync("خطأ في الحذف", ErrorMessage);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task OnPrint()
    {
        if (SelectedPayment == null) return;

        IsBusy = true;
        try
        {
            var settingsResult = await SettingsService.GetSettingsAsync();
            if (!settingsResult.IsSuccess || settingsResult.Value == null) return;

            PaymentPrinter.PrintPreview(SelectedPayment.ToPrintDto(), settingsResult.Value.ToPrintDto());
        }
        catch (Exception ex)
        {
            LogSystemError($"Failed to print supplier payment {SelectedPayment?.Id}", "SupplierPaymentsListViewModel.OnPrint", ex);
            ErrorMessage = "حدث خطأ غير متوقع أثناء الطباعة";
        }
        finally
        {
            IsBusy = false;
        }
    }
}




