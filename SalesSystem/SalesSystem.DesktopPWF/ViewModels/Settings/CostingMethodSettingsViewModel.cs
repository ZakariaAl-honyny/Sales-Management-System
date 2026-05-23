using System.Windows.Input;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;

namespace SalesSystem.DesktopPWF.ViewModels.Settings;

public class CostingMethodSettingsViewModel : ViewModelBase
{
    private readonly ISettingsApiService _settingsApi;
    private readonly IDialogService _dialogService;
    private int _currentMethodValue;

    public CostingMethodSettingsViewModel(
        ISettingsApiService settingsApi,
        IDialogService dialogService)
    {
        _settingsApi = settingsApi;
        _dialogService = dialogService;
        SetDialogService(dialogService);
        SaveCommand = new AsyncRelayCommand(async () => await ExecuteAsync(SaveOperationAsync));
        CancelCommand = new RelayCommand(Cancel);
        _ = LoadAsync();
    }

    public bool HasChanges { get; private set; }
    public bool ShowWarning { get; private set; } = true;

    private int _selectedMethodValue = 1;
    public int SelectedMethodValue
    {
        get => _selectedMethodValue;
        set
        {
            if (SetProperty(ref _selectedMethodValue, value))
            {
                HasChanges = value != _currentMethodValue;
                OnPropertyChanged(nameof(IsWeightedAverageSelected));
                OnPropertyChanged(nameof(IsLastPurchasePriceSelected));
                OnPropertyChanged(nameof(IsSupplierPriceSelected));
            }
        }
    }

    public bool IsWeightedAverageSelected
    {
        get => _selectedMethodValue == 1;
        set { if (value) SelectedMethodValue = 1; }
    }

    public bool IsLastPurchasePriceSelected
    {
        get => _selectedMethodValue == 2;
        set { if (value) SelectedMethodValue = 2; }
    }

    public bool IsSupplierPriceSelected
    {
        get => _selectedMethodValue == 3;
        set { if (value) SelectedMethodValue = 3; }
    }

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    private async Task LoadAsync()
    {
        var result = await _settingsApi.GetSettingsAsync();
        if (result.IsSuccess && result.Value != null)
        {
            _currentMethodValue = result.Value.CostingMethod;
            _selectedMethodValue = _currentMethodValue;
            OnPropertyChanged(string.Empty);
        }
    }

    private async Task SaveOperationAsync()
    {
        var getResult = await _settingsApi.GetSettingsAsync();
        if (getResult.IsSuccess && getResult.Value != null)
        {
            var current = getResult.Value;
            var request = new UpdateSettingsRequest(
                current.StoreName, current.Address, current.Phone, current.Email,
                current.LogoPath, current.CurrencyCode, current.DefaultTaxRate,
                current.IsTaxEnabled, current.TaxNumber,
                current.EnableStockAlerts, current.AllowNegativeStock, current.AutoUpdatePrices,
                current.InvoicePrefix,
                _selectedMethodValue);

            var updateResult = await _settingsApi.UpdateSettingsAsync(request);
            if (updateResult.IsSuccess)
            {
                _currentMethodValue = _selectedMethodValue;
                HasChanges = false;
                await _dialogService.ShowSuccessAsync("تم", "تم حفظ طريقة احتساب التكلفة بنجاح");
            }
            else
            {
                await _dialogService.ShowErrorAsync("خطأ في الحفظ", "حدث خطأ غير متوقع أثناء حفظ الإعدادات");
            }
        }
        else
        {
            await _dialogService.ShowErrorAsync("خطأ في التحميل", "تعذر تحميل الإعدادات الحالية");
        }
    }

    private void Cancel()
    {
        _selectedMethodValue = _currentMethodValue;
        OnPropertyChanged(nameof(SelectedMethodValue));
        OnPropertyChanged(nameof(IsWeightedAverageSelected));
        OnPropertyChanged(nameof(IsLastPurchasePriceSelected));
        OnPropertyChanged(nameof(IsSupplierPriceSelected));
        HasChanges = false;
    }
}