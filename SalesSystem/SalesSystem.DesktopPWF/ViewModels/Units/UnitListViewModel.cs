using SalesSystem.DesktopPWF.Messaging.Messages;
using SalesSystem.DesktopPWF.Services.App.Toast;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;

namespace SalesSystem.DesktopPWF.ViewModels.Units;

public class UnitListViewModel : ViewModelBase
{
    private readonly IUnitApiService _unitService;
    private readonly IEventBus _eventBus;
    private readonly IDialogService _dialogService;
    private readonly IToastNotificationService _toastService;

    private ObservableCollection<UnitDto> _units = new();
    private ICollectionView? _unitsView;
    private UnitDto? _selectedUnit;
    private string _searchText = string.Empty;
    private string? _errorMessage;
    private bool _isEmpty;
    private bool _includeInactive;

    public UnitListViewModel()
    {
        _unitService = App.GetService<IUnitApiService>();
        _eventBus = App.GetService<IEventBus>();
        _dialogService = App.GetService<IDialogService>();
        _toastService = App.GetService<IToastNotificationService>();

        InitializeCommands();
    }

    private void InitializeCommands()
    {
        RefreshCommand = new AsyncRelayCommand(LoadUnitsAsync);
        AddCommand = new RelayCommand(AddUnit);
        EditCommand = new RelayCommand(EditUnit, () => SelectedUnit != null);
        DeleteCommand = new AsyncRelayCommand(DeleteUnitAsync, () => SelectedUnit != null && SelectedUnit.IsActive);
        RestoreCommand = new AsyncRelayCommand(RestoreUnitAsync, () => SelectedUnit != null && !SelectedUnit.IsActive);
        SearchCommand = new RelayCommand(Search);
        
        // Subscribe to unit changes
        _eventBus.Subscribe<UnitChangedMessage>(OnUnitChanged);
    }

    #region Properties

    public ObservableCollection<UnitDto> Units
    {
        get => _units;
        set => SetProperty(ref _units, value);
    }

    public ICollectionView? UnitsView
    {
        get => _unitsView;
        private set => SetProperty(ref _unitsView, value);
    }

    public UnitDto? SelectedUnit
    {
        get => _selectedUnit;
        set
        {
            if (SetProperty(ref _selectedUnit, value))
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
                UnitsView?.Refresh();
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
                _ = LoadUnitsAsync();
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

    public async Task LoadUnitsAsync()
    {
        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var result = await _unitService.GetAllAsync(IncludeInactive);

            if (result.IsSuccess && result.Value != null)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    Units.Clear();
                    foreach (var item in result.Value.OrderByDescending(x => x.Id))
                    {
                        Units.Add(item);
                    }
                    SetupCollectionView();
                    IsEmpty = Units.Count == 0;
                });
            }
            else
            {
                ErrorMessage = HandleFailure(result.Error ?? "ظپط´ظ„ ظپظٹ طھط­ظ…ظٹظ„ ط§ظ„ظˆط­ط¯ط§طھ", "UnitListViewModel.LoadUnitsAsync", "[UnitListViewModel.LoadUnitsAsync] Failed to load units list.");
                IsEmpty = Units.Count == 0;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = HandleException(ex, "UnitListViewModel.LoadUnitsAsync", "[UnitListViewModel.LoadUnitsAsync] Failed to load units list.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void SetupCollectionView()
    {
        UnitsView = CollectionViewSource.GetDefaultView(Units);
        UnitsView.Filter = FilterUnits;
    }

    private bool FilterUnits(object obj)
    {
        if (obj is not UnitDto unit) return false;

        if (string.IsNullOrWhiteSpace(SearchText)) return true;

        var searchLower = SearchText.Trim().ToLower();
        return unit.Name.ToLower().Contains(searchLower);
    }

    private void AddUnit()
    {
        var editorVm = new UnitEditorViewModel();
        if (_dialogService.ShowDialog(editorVm))
        {
            _ = LoadUnitsAsync();
        }
    }

    private void EditUnit()
    {
        if (SelectedUnit == null) return;

        var editorVm = new UnitEditorViewModel();
        editorVm.LoadUnit(SelectedUnit);

        if (_dialogService.ShowDialog(editorVm))
        {
            _ = LoadUnitsAsync();
        }
    }

public async Task DeleteUnitAsync()
    {
        if (SelectedUnit == null) return;

        var strategy = await _dialogService.ShowDeleteConfirmationAsync($"ط§ظ„ظˆط­ط¯ط©: {SelectedUnit.Name}");

        if (strategy == DeleteStrategy.Cancel) return;

        IsBusy = true;
        ErrorMessage = null;

        try
        {
            if (strategy == DeleteStrategy.Deactivate)
            {
                var deleteResult = await _unitService.DeleteAsync(SelectedUnit.Id);
                if (deleteResult.IsSuccess)
                {
                    await LoadUnitsAsync();
                    _toastService.ShowSuccess("طھظ… ط¥ظ„ط؛ط§ط، طھظ†ط´ظٹط· ط§ظ„ظˆط­ط¯ط© ط¨ظ†ط¬ط§ط­");
                }
                else
                {
                    ErrorMessage = deleteResult.Error ?? "ظپط´ظ„ ظپظٹ ط¥ظ„ط؛ط§ط، طھظ†ط´ظٹط· ط§ظ„ظˆط­ط¯ط©";
                }
            }
            else if (strategy == DeleteStrategy.Permanent)
            {
                var deleteResult = await _unitService.DeletePermanentlyAsync(SelectedUnit.Id);
                if (deleteResult.IsSuccess)
                {
                    await LoadUnitsAsync();
                    _toastService.ShowSuccess("طھظ… ط­ط°ظپ ط§ظ„ظˆط­ط¯ط© ظ†ظ‡ط§ط¦ظٹط§ظ‹");
                }
                else
                {
                    var error = deleteResult.Error ?? "ظپط´ظ„ ظپظٹ ط­ط°ظپ ط§ظ„ظˆط­ط¯ط©";
                    ErrorMessage = error;
                    LogSystemError($"Hard delete failed for Unit {SelectedUnit.Id}: {error}", "UnitListViewModel.DeleteUnitAsync");
                }
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = "ط­ط¯ط« ط®ط·ط£ ط؛ظٹط± ظ…طھظˆظ‚ط¹ ط£ط«ظ†ط§ط، ط§ظ„ط­ط°ظپ";
            HandleException(ex, "UnitListViewModel.DeleteUnitAsync", $"[UnitListViewModel.DeleteUnitAsync] Failed to delete unit with ID {SelectedUnit?.Id}.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task RestoreUnitAsync()
    {
        if (SelectedUnit == null) return;

        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var request = new UpdateUnitRequest(
                Name: SelectedUnit.Name,
                Symbol: SelectedUnit.Symbol,
                IsActive: true
            );

            var result = await _unitService.UpdateAsync(SelectedUnit.Id, request);

            if (result.IsSuccess)
            {
                await LoadUnitsAsync();
                await _dialogService.ShowSuccessAsync("ظ†ط¬ط§ط­", "طھظ… ط§ط³طھط¹ط§ط¯ط© ط§ظ„ظˆط­ط¯ط© ط¨ظ†ط¬ط§ط­");
            }
            else
            {
                ErrorMessage = result.Error ?? "ظپط´ظ„ ظپظٹ ط§ط³طھط¹ط§ط¯ط© ط§ظ„ظˆط­ط¯ط©";
                await _dialogService.ShowErrorAsync("ط®ط·ط£ ظپظٹ ط§ظ„ط§ط³طھط¹ط§ط¯ط©", ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = "ط­ط¯ط« ط®ط·ط£ ط؛ظٹط± ظ…طھظˆظ‚ط¹ ط£ط«ظ†ط§ط، ط§ط³طھط¹ط§ط¯ط© ط§ظ„ظˆط­ط¯ط©";
            HandleException(ex, "UnitListViewModel.RestoreUnitAsync", $"[UnitListViewModel.RestoreUnitAsync] Failed to restore unit with ID {SelectedUnit?.Id}.");
        }
finally
        {
            IsBusy = false;
        }
    }

    private void Search()
    {
        UnitsView?.Refresh();
    }

    private void OnUnitChanged(UnitChangedMessage msg)
    {
        System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
        {
            await LoadUnitsAsync();
        });
    }

    public override void Cleanup()
    {
        _eventBus.Unsubscribe<UnitChangedMessage>(OnUnitChanged);
    }
    #endregion
}




