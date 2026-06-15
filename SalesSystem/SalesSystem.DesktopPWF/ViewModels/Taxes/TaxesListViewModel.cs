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

namespace SalesSystem.DesktopPWF.ViewModels.Taxes;

public class TaxesListViewModel : ViewModelBase
{
    private readonly ITaxesApiService _taxesService;
    private readonly IEventBus _eventBus;
    private readonly IDialogService _dialogService;
    private readonly IToastNotificationService _toastService;
    private readonly IScreenWindowService _screenWindowService;

    private ObservableCollection<TaxDto> _taxes = new();
    private ICollectionView? _taxesView;
    private TaxDto? _selectedTax;
    private string _searchText = string.Empty;
    private string? _errorMessage;
    private bool _isEmpty;
    private bool _includeInactive;

    public TaxesListViewModel()
    {
        _taxesService = App.GetService<ITaxesApiService>();
        _eventBus = App.GetService<IEventBus>();
        _dialogService = App.GetService<IDialogService>();
        _toastService = App.GetService<IToastNotificationService>();
        _screenWindowService = App.GetService<IScreenWindowService>();

        InitializeCommands();
    }

    private void InitializeCommands()
    {
        RefreshCommand = new AsyncRelayCommand(LoadTaxesAsync);
        AddCommand = new RelayCommand(AddTax);
        EditCommand = new RelayCommand(EditTax, () => SelectedTax != null);
        DeleteCommand = new AsyncRelayCommand(DeleteTaxAsync, () => SelectedTax != null && SelectedTax.IsActive);
        RestoreCommand = new AsyncRelayCommand(RestoreTaxAsync, () => SelectedTax != null && !SelectedTax.IsActive);
        SearchCommand = new RelayCommand(Search);

        // Subscribe to tax changes
        _eventBus.Subscribe<TaxChangedMessage>(OnTaxChanged);
    }

    #region Properties

    public ObservableCollection<TaxDto> Taxes
    {
        get => _taxes;
        set => SetProperty(ref _taxes, value);
    }

    public ICollectionView? TaxesView
    {
        get => _taxesView;
        private set => SetProperty(ref _taxesView, value);
    }

    public TaxDto? SelectedTax
    {
        get => _selectedTax;
        set
        {
            if (SetProperty(ref _selectedTax, value))
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
                TaxesView?.Refresh();
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
                _ = LoadTaxesAsync();
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

    public async Task LoadTaxesAsync()
    {
        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var result = await _taxesService.GetAllAsync(IncludeInactive);

            if (result.IsSuccess && result.Value != null)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    Taxes.Clear();
                    foreach (var item in result.Value.OrderByDescending(x => x.Id))
                    {
                        Taxes.Add(item);
                    }
                    SetupCollectionView();
                    IsEmpty = Taxes.Count == 0;
                });
            }
            else
            {
                ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل الضرائب", "TaxesListViewModel.LoadTaxesAsync");
                IsEmpty = Taxes.Count == 0;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = HandleException(ex, "TaxesListViewModel.LoadTaxesAsync");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void SetupCollectionView()
    {
        TaxesView = CollectionViewSource.GetDefaultView(Taxes);
        TaxesView.Filter = FilterTaxes;
    }

    private bool FilterTaxes(object obj)
    {
        if (obj is not TaxDto tax) return false;

        if (string.IsNullOrWhiteSpace(SearchText)) return true;

        var searchLower = SearchText.Trim().ToLower();
        return tax.Name.ToLower().Contains(searchLower);
    }

    private void AddTax()
    {
        var editorVm = App.GetService<TaxEditorViewModel>();
        _screenWindowService.OpenScreen(editorVm, new ScreenWindowOptions
        {
            Title = "ضريبة جديدة",
            Width = 900,
            Height = 650,
            OnClosed = (_) =>
            {
                System.Windows.Application.Current.Dispatcher.InvokeAsync(() => _ = LoadTaxesAsync());
            }
        });
    }

    private void EditTax()
    {
        if (SelectedTax == null) return;

        var editorVm = App.GetService<TaxEditorViewModel>();
        editorVm.LoadTax(SelectedTax);

        _screenWindowService.OpenScreen(editorVm, new ScreenWindowOptions
        {
            Title = "تعديل ضريبة",
            Width = 900,
            Height = 650,
            OnClosed = (_) =>
            {
                System.Windows.Application.Current.Dispatcher.InvokeAsync(() => _ = LoadTaxesAsync());
            }
        });
    }

    public async Task DeleteTaxAsync()
    {
        if (SelectedTax == null) return;

        var strategy = await _dialogService.ShowDeleteConfirmationAsync($"الضريبة: {SelectedTax.Name}");

        if (strategy == DeleteStrategy.Cancel) return;

        IsBusy = true;
        ErrorMessage = null;

        try
        {
            if (strategy == DeleteStrategy.Deactivate)
            {
                var deleteResult = await _taxesService.DeleteAsync(SelectedTax.Id);
                if (deleteResult.IsSuccess)
                {
                    await LoadTaxesAsync();
                    _toastService.ShowSuccess("تم إلغاء تنشيط الضريبة بنجاح");
                }
                else
                {
                    ErrorMessage = deleteResult.Error ?? "فشل في إلغاء تنشيط الضريبة";
                }
            }
            else if (strategy == DeleteStrategy.Permanent)
            {
                var deleteResult = await _taxesService.DeletePermanentlyAsync(SelectedTax.Id);
                if (deleteResult.IsSuccess)
                {
                    await LoadTaxesAsync();
                    _toastService.ShowSuccess("تم حذف الضريبة نهائياً");
                }
                else
                {
                    var error = deleteResult.Error ?? "فشل في حذف الضريبة";
                    ErrorMessage = error;
                    LogSystemError($"Hard delete failed for Tax {SelectedTax.Id}: {error}", "TaxesListViewModel.DeleteTaxAsync");
                }
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = "حدث خطأ غير متوقع أثناء الحذف";
            HandleException(ex, "TaxesListViewModel.DeleteTaxAsync");
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task RestoreTaxAsync()
    {
        if (SelectedTax == null) return;

        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var request = new UpdateTaxRequest(
                Name: SelectedTax.Name,
                Code: SelectedTax.Code,
                Rate: SelectedTax.Rate,
                TaxType: SelectedTax.TaxType,
                IsDefault: SelectedTax.IsDefault,
                IsActive: true
            );

            var result = await _taxesService.UpdateAsync(SelectedTax.Id, request);

            if (result.IsSuccess)
            {
                await LoadTaxesAsync();
                await _dialogService.ShowSuccessAsync("نجاح", "تم استعادة الضريبة بنجاح");
            }
            else
            {
                ErrorMessage = result.Error ?? "فشل في استعادة الضريبة";
                await _dialogService.ShowErrorAsync("خطأ في الاستعادة", ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = "حدث خطأ غير متوقع أثناء استعادة الضريبة";
            HandleException(ex, "TaxesListViewModel.RestoreTaxAsync");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void Search()
    {
        TaxesView?.Refresh();
    }

    private void OnTaxChanged(TaxChangedMessage msg)
    {
        System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
        {
            await LoadTaxesAsync();
        });
    }

    public override void Cleanup()
    {
        _eventBus.Unsubscribe<TaxChangedMessage>(OnTaxChanged);
    }

    #endregion
}
