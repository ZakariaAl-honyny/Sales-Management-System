using System.Windows.Input;
using SalesSystem.Application.Interfaces.Repositories;
using SalesSystem.Domain.Enums;

namespace SalesSystem.DesktopPWF.ViewModels.Settings;

public class CostingMethodSettingsViewModel : ViewModelBase
{
    private readonly ISystemSettingsRepository _settingsRepository;
    private readonly IDialogService _dialogService;
    private CostingMethod _currentMethod;

    public CostingMethodSettingsViewModel(
        ISystemSettingsRepository settingsRepository,
        IDialogService dialogService)
    {
        _settingsRepository = settingsRepository;
        _dialogService = dialogService;
        SaveCommand = new RelayCommand(async () => await SaveAsync());
        CancelCommand = new RelayCommand(Cancel);
        _ = LoadAsync();
    }

    public bool IsWeightedAverage => _currentMethod == CostingMethod.WeightedAverage;
    public bool IsLastPurchasePrice => _currentMethod == CostingMethod.LastPurchasePrice;
    public bool IsSupplierPrice => _currentMethod == CostingMethod.SupplierPrice;
    public bool HasChanges { get; private set; }
    public bool ShowWarning { get; private set; } = true;

    private CostingMethod _selectedMethod;
    public CostingMethod SelectedMethod
    {
        get => _selectedMethod;
        set
        {
            if (SetProperty(ref _selectedMethod, value))
            {
                HasChanges = value != _currentMethod;
                OnPropertyChanged(nameof(IsWeightedAverage));
                OnPropertyChanged(nameof(IsLastPurchasePrice));
                OnPropertyChanged(nameof(IsSupplierPrice));
            }
        }
    }

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    private async Task LoadAsync()
    {
        _currentMethod = await _settingsRepository.GetCostingMethodAsync();
        _selectedMethod = _currentMethod;
        OnPropertyChanged(string.Empty);
    }

    private async Task SaveAsync()
    {
        try
        {
            await _settingsRepository.SetCostingMethodAsync(_selectedMethod);
            _currentMethod = _selectedMethod;
            HasChanges = false;
            await _dialogService.ShowSuccessAsync("تم", "تم حفظ طريقة احتساب التكلفة بنجاح");
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync("خطأ", ex.Message);
        }
    }

    private void Cancel()
    {
        _selectedMethod = _currentMethod;
        OnPropertyChanged(string.Empty);
        HasChanges = false;
    }
}