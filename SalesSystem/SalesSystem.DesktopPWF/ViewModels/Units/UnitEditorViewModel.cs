using SalesSystem.DesktopPWF.Messaging.Messages;
using System.Windows.Input;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;

namespace SalesSystem.DesktopPWF.ViewModels.Units;

public class UnitEditorViewModel : ViewModelBase
{
    private readonly IUnitApiService _unitService;
    private readonly IEventBus _eventBus;
    private string _name = string.Empty;
    private bool _isLoading;
    private string? _errorMessage;
    private string _windowTitle = "إضافة وحدة جديدة";

    public UnitEditorViewModel()
    {
        _unitService = App.GetService<IUnitApiService>();
        _eventBus = App.GetService<IEventBus>();
        InitializeCommands();
    }

    public void LoadUnit(UnitDto unit)
    {
        _unitDto = unit;
        Name = unit.Name;
        WindowTitle = $"تعديل وحدة: {unit.Name}";
    }

    private UnitDto? _unitDto;

    private void InitializeCommands()
    {
        SaveCommand = new AsyncRelayCommand(SaveAsync, () => !HasErrors && !string.IsNullOrWhiteSpace(Name));
        CancelCommand = new RelayCommand(() => RequestClose());
    }

    public bool CanSave => !HasErrors && !string.IsNullOrWhiteSpace(Name);

    #region Properties

    public string Name
    {
        get => _name;
        set
        {
            if (SetProperty(ref _name, value))
            {
                OnPropertyChanged(nameof(CanSave));
                (SaveCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public string WindowTitle
    {
        get => _windowTitle;
        set => SetProperty(ref _windowTitle, value);
    }

    #endregion

    #region Commands

    public ICommand SaveCommand { get; private set; } = null!;
    public ICommand CancelCommand { get; private set; } = null!;

    #endregion

    #region Methods

    private async Task SaveAsync()
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            Result<UnitDto> result;
            if (_unitDto == null)
            {
                var request = new CreateUnitRequest(Name, null);
                result = await _unitService.CreateAsync(request);
            }
            else
            {
                var request = new UpdateUnitRequest(Name, _unitDto.Symbol, _unitDto.IsActive);
                result = await _unitService.UpdateAsync(_unitDto.Id, request);
            }

            if (result.IsSuccess && result.Value != null)
            {
                _eventBus.Publish(new UnitChangedMessage(result.Value.Id));
                RequestClose();
            }
            else
            {
                ErrorMessage = HandleFailure(result.Error ?? "فشل في حفظ الوحدة", "UnitEditorViewModel.SaveAsync");
                System.Windows.MessageBox.Show(ErrorMessage, "خطأ في الحفظ", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = HandleException(ex, "UnitEditorViewModel.SaveAsync", "Failed to save unit data.");
            System.Windows.MessageBox.Show(ErrorMessage, "خطأ", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    #endregion
}
