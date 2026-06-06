using SalesSystem.DesktopPWF.Messaging.Messages;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using SalesSystem.DesktopPWF.Services.App.Toast;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;

namespace SalesSystem.DesktopPWF.ViewModels.Currencies;

public class CurrenciesListViewModel : ViewModelBase
{
    private readonly ICurrencyApiService _currencyService;
    private readonly IEventBus _eventBus;
    private readonly IDialogService _dialogService;
    private readonly IToastNotificationService _toastService;
    private readonly IScreenWindowService _screenWindowService;

    private ObservableCollection<CurrencyDto> _currencies = new();
    private ICollectionView? _currenciesView;
    private CurrencyDto? _selectedCurrency;
    private string _searchText = string.Empty;
    private string? _errorMessage;
    private bool _isEmpty;
    private bool _includeInactive;

    public CurrenciesListViewModel()
    {
        _currencyService = App.GetService<ICurrencyApiService>();
        _eventBus = App.GetService<IEventBus>();
        _dialogService = App.GetService<IDialogService>();
        _toastService = App.GetService<IToastNotificationService>();
        _screenWindowService = App.GetService<IScreenWindowService>();
        SetDialogService(_dialogService);

        InitializeCommands();
    }

    private void InitializeCommands()
    {
        RefreshCommand = new AsyncRelayCommand(LoadCurrenciesAsync);
        AddCommand = new RelayCommand(AddCurrency);
        EditCommand = new RelayCommand(EditCurrency, () => SelectedCurrency != null);
        DeleteCommand = new AsyncRelayCommand(DeleteCurrencyAsync, () => SelectedCurrency != null && SelectedCurrency.IsActive);
        RestoreCommand = new AsyncRelayCommand(RestoreCurrencyAsync, () => SelectedCurrency != null && !SelectedCurrency.IsActive);
        SearchCommand = new RelayCommand(Search);

        // Subscribe to currency changes
        _eventBus.Subscribe<CurrencyChangedMessage>(OnCurrencyChanged);
        _eventBus.Subscribe<CurrencyRateChangedMessage>(OnCurrencyRateChanged);
    }

    #region Properties

    public ObservableCollection<CurrencyDto> Currencies
    {
        get => _currencies;
        set => SetProperty(ref _currencies, value);
    }

    public ICollectionView? CurrenciesView
    {
        get => _currenciesView;
        private set => SetProperty(ref _currenciesView, value);
    }

    public CurrencyDto? SelectedCurrency
    {
        get => _selectedCurrency;
        set
        {
            if (SetProperty(ref _selectedCurrency, value))
            {
                (EditCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (DeleteCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
                (RestoreCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                CurrenciesView?.Refresh();
            }
        }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public bool IsEmpty
    {
        get => _isEmpty;
        private set => SetProperty(ref _isEmpty, value);
    }

    public bool IncludeInactive
    {
        get => _includeInactive;
        set
        {
            if (SetProperty(ref _includeInactive, value))
            {
                _ = LoadCurrenciesAsync();
            }
        }
    }

    #endregion

    #region Commands

    public ICommand RefreshCommand { get; private set; } = null!;
    public ICommand AddCommand { get; private set; } = null!;
    public ICommand EditCommand { get; private set; } = null!;
    public ICommand DeleteCommand { get; private set; } = null!;
    public ICommand RestoreCommand { get; private set; } = null!;
    public ICommand SearchCommand { get; private set; } = null!;

    #endregion

    #region Methods

    public async Task LoadCurrenciesAsync()
    {
        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var result = await _currencyService.GetAllAsync(IncludeInactive);

            if (result.IsSuccess && result.Value != null)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    Currencies.Clear();
                    foreach (var item in result.Value.OrderByDescending(x => x.Id))
                    {
                        Currencies.Add(item);
                    }
                    SetupCollectionView();
                    IsEmpty = Currencies.Count == 0;
                });
            }
            else
            {
                ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل العملات", "CurrenciesListViewModel.LoadCurrenciesAsync");
                IsEmpty = Currencies.Count == 0;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = HandleException(ex, "CurrenciesListViewModel.LoadCurrenciesAsync");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void SetupCollectionView()
    {
        CurrenciesView = CollectionViewSource.GetDefaultView(Currencies);
        CurrenciesView.Filter = FilterCurrencies;
    }

    private bool FilterCurrencies(object obj)
    {
        if (obj is not CurrencyDto currency) return false;

        if (string.IsNullOrWhiteSpace(SearchText)) return true;

        var searchLower = SearchText.Trim().ToLower();
        return currency.Name.ToLower().Contains(searchLower) ||
               currency.Code.ToLower().Contains(searchLower);
    }

    private void AddCurrency()
    {
        var editorVm = App.GetService<CurrencyEditorViewModel>();
        _screenWindowService.OpenScreen(editorVm, new ScreenWindowOptions
        {
            Title = "عملة جديدة",
            Width = 900,
            Height = 650,
            OnClosed = (_) =>
            {
                System.Windows.Application.Current.Dispatcher.InvokeAsync(() => _ = LoadCurrenciesAsync());
            }
        });
    }

    private void EditCurrency()
    {
        if (SelectedCurrency == null) return;

        var editorVm = App.GetService<CurrencyEditorViewModel>();
        editorVm.LoadCurrency(SelectedCurrency);

        _screenWindowService.OpenScreen(editorVm, new ScreenWindowOptions
        {
            Title = "تعديل عملة",
            Width = 900,
            Height = 650,
            OnClosed = (_) =>
            {
                System.Windows.Application.Current.Dispatcher.InvokeAsync(() => _ = LoadCurrenciesAsync());
            }
        });
    }

    public async Task DeleteCurrencyAsync()
    {
        if (SelectedCurrency == null) return;

        var strategy = await _dialogService.ShowDeleteConfirmationAsync($"العملة: {SelectedCurrency.Name} ({SelectedCurrency.Code})");

        if (strategy == DeleteStrategy.Cancel) return;

        IsBusy = true;
        ErrorMessage = null;

        try
        {
            if (strategy == DeleteStrategy.Deactivate)
            {
                var deleteResult = await _currencyService.DeleteAsync(SelectedCurrency.Id);
                if (deleteResult.IsSuccess)
                {
                    await LoadCurrenciesAsync();
                    _toastService.ShowSuccess("تم إلغاء تنشيط العملة بنجاح");
                }
                else
                {
                    ErrorMessage = deleteResult.Error ?? "فشل في إلغاء تنشيط العملة";
                }
            }
            else if (strategy == DeleteStrategy.Permanent)
            {
                var deleteResult = await _currencyService.DeletePermanentlyAsync(SelectedCurrency.Id);
                if (deleteResult.IsSuccess)
                {
                    await LoadCurrenciesAsync();
                    _toastService.ShowSuccess("تم حذف العملة نهائياً");
                }
                else
                {
                    var error = deleteResult.Error ?? "فشل في حذف العملة";
                    ErrorMessage = error;
                    LogSystemError($"Hard delete failed for Currency {SelectedCurrency.Id}: {error}", "CurrenciesListViewModel.DeleteCurrencyAsync");
                }
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = "حدث خطأ غير متوقع أثناء الحذف";
            HandleException(ex, "CurrenciesListViewModel.DeleteCurrencyAsync");
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task RestoreCurrencyAsync()
    {
        if (SelectedCurrency == null) return;

        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var request = new SalesSystem.Contracts.Requests.UpdateCurrencyRequest(
                SelectedCurrency.Name,
                SelectedCurrency.Symbol,
                SelectedCurrency.ExchangeRateToBase,
                SelectedCurrency.IsBaseCurrency,
                SelectedCurrency.FractionName,
                true // IsActive
            );

            var result = await _currencyService.UpdateAsync(SelectedCurrency.Id, request);

            if (result.IsSuccess)
            {
                await LoadCurrenciesAsync();
                await _dialogService.ShowSuccessAsync("نجاح", "تم استعادة العملة بنجاح");
            }
            else
            {
                ErrorMessage = result.Error ?? "فشل في استعادة العملة";
                await _dialogService.ShowErrorAsync("خطأ في الاستعادة", ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = "حدث خطأ غير متوقع أثناء استعادة العملة";
            HandleException(ex, "CurrenciesListViewModel.RestoreCurrencyAsync");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void Search()
    {
        CurrenciesView?.Refresh();
    }

    private void OnCurrencyChanged(CurrencyChangedMessage msg)
    {
        System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
        {
            await LoadCurrenciesAsync();
        });
    }

    private void OnCurrencyRateChanged(CurrencyRateChangedMessage msg)
    {
        System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
        {
            await LoadCurrenciesAsync();
        });
    }

    public override void Cleanup()
    {
        _eventBus.Unsubscribe<CurrencyChangedMessage>(OnCurrencyChanged);
        _eventBus.Unsubscribe<CurrencyRateChangedMessage>(OnCurrencyRateChanged);
    }

    #endregion
}
