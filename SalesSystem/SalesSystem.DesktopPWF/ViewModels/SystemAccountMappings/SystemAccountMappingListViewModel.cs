using System.Collections.ObjectModel;
using System.Windows.Input;
using SalesSystem.Contracts.Responses;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;

namespace SalesSystem.DesktopPWF.ViewModels.SystemAccountMappings;

/// <summary>
/// ViewModel for managing system account mappings (key-value pattern).
/// Shows all mappings with their linked account names.
/// Supports editing the account assignment.
/// RULE-059: All buttons always enabled — validates/confirms on click.
/// </summary>
public class SystemAccountMappingListViewModel : ViewModelBase, IDisposable
{
    private readonly ISystemAccountMappingApiService _mappingApi;
    private readonly IDialogService _dialogService;
    private readonly IScreenWindowService _screenWindowService;

    private ObservableCollection<SystemAccountMappingDto> _mappings = new();
    private SystemAccountMappingDto? _selectedMapping;
    private bool _isEmpty;
    private string? _errorMessage;

    public SystemAccountMappingListViewModel()
        : this(
            App.GetService<ISystemAccountMappingApiService>(),
            App.GetService<IDialogService>(),
            App.GetService<IScreenWindowService>())
    {
    }

    public SystemAccountMappingListViewModel(
        ISystemAccountMappingApiService mappingApi,
        IDialogService dialogService,
        IScreenWindowService screenWindowService)
    {
        _mappingApi = mappingApi ?? throw new ArgumentNullException(nameof(mappingApi));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _screenWindowService = screenWindowService ?? throw new ArgumentNullException(nameof(screenWindowService));
        SetDialogService(dialogService);

        RefreshCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExecuteAsync(LoadMappingsOperationAsync)));
        EditCommand = new RelayCommand(EditMapping);
        CloseCommand = new RelayCommand(RequestClose);

        _ = ExecuteAsync(LoadMappingsOperationAsync);
    }

    // ═══════════════════════════════════════════════════════════════
    // Properties
    // ═══════════════════════════════════════════════════════════════

    public ObservableCollection<SystemAccountMappingDto> Mappings
    {
        get => _mappings;
        set => SetProperty(ref _mappings, value);
    }

    public SystemAccountMappingDto? SelectedMapping
    {
        get => _selectedMapping;
        set
        {
            if (SetProperty(ref _selectedMapping, value))
                OnPropertyChanged(nameof(HasSelection));
        }
    }

    public bool IsEmpty
    {
        get => _isEmpty;
        private set => SetProperty(ref _isEmpty, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public bool HasSelection => SelectedMapping != null;
    public bool HasNoMappings => Mappings.Count == 0;

    // ═══════════════════════════════════════════════════════════════
    // Commands
    // ═══════════════════════════════════════════════════════════════

    public ICommand RefreshCommand { get; private set; } = null!;
    public ICommand EditCommand { get; private set; } = null!;
    public ICommand CloseCommand { get; private set; } = null!;

    // ═══════════════════════════════════════════════════════════════
    // Operations
    // ═══════════════════════════════════════════════════════════════

    private async Task LoadMappingsOperationAsync()
    {
        ErrorMessage = null;
        var result = await _mappingApi.GetAllAsync();

        if (result.IsSuccess && result.Value != null)
        {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                Mappings.Clear();
                foreach (var dto in result.Value.OrderBy(x => x.MappingKeyName ?? x.MappingKey.ToString()))
                {
                    Mappings.Add(dto);
                }
                IsEmpty = Mappings.Count == 0;
                OnPropertyChanged(nameof(HasNoMappings));
            });
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل حسابات النظام", "SystemAccountMappingListViewModel.Load");
            IsEmpty = Mappings.Count == 0;
            OnPropertyChanged(nameof(HasNoMappings));
        }
    }

    private void EditMapping()
    {
        if (SelectedMapping == null)
        {
            _ = _dialogService.ShowWarningAsync("تنبيه", "يرجى تحديد حساب نظام من القائمة.");
            return;
        }

        var editorVm = new SystemAccountMappingEditorViewModel(_mappingApi, _dialogService, SelectedMapping);
        editorVm.OnSaved += () =>
        {
            _ = ExecuteAsync(LoadMappingsOperationAsync);
        };
        _screenWindowService.OpenScreen(editorVm, new ScreenWindowOptions
        {
            Title = "تعديل حساب النظام",
            Width = 550,
            Height = 450
        });
    }

    // ═══════════════════════════════════════════════════════════════
    // Cleanup
    // ═══════════════════════════════════════════════════════════════

    public override void Cleanup()
    {
        Mappings.Clear();
        base.Cleanup();
    }

    public void Dispose()
    {
        Cleanup();
        GC.SuppressFinalize(this);
    }
}
