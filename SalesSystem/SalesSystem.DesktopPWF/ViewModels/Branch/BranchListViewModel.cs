using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using SalesSystem.DesktopPWF.Services.App.Toast;

namespace SalesSystem.DesktopPWF.ViewModels.Branch;

public class BranchListViewModel : ViewModelBase
{
    private readonly IBranchApiService _branchService;
    private readonly IDialogService _dialogService;
    private readonly IToastNotificationService _toastService;
    private readonly IScreenWindowService _screenWindowService;

    private ObservableCollection<BranchDto> _branches = new();
    private ICollectionView? _branchesView;
    private BranchDto? _selectedBranch;
    private string _searchText = string.Empty;
    private string? _errorMessage;
    private bool _isEmpty;
    private bool _includeInactive;

    public BranchListViewModel()
    {
        _branchService = App.GetService<IBranchApiService>();
        _dialogService = App.GetService<IDialogService>();
        _toastService = App.GetService<IToastNotificationService>();
        _screenWindowService = App.GetService<IScreenWindowService>();
        SetDialogService(_dialogService);

        InitializeCommands();
    }

    private void InitializeCommands()
    {
        RefreshCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExecuteAsync(LoadBranchesOperationAsync,
                ex => ErrorMessage = HandleException(ex, "BranchListViewModel.LoadBranchesAsync"))));
        AddCommand = new RelayCommand(AddBranch);
        EditCommand = new RelayCommand(EditBranch);
        DeleteCommand = new AsyncRelayCommand(DeleteBranchAsync);
        SearchCommand = new RelayCommand(Search);
    }

    #region Properties

    public ObservableCollection<BranchDto> Branches
    {
        get => _branches;
        set => SetProperty(ref _branches, value);
    }

    public ICollectionView? BranchesView
    {
        get => _branchesView;
        private set => SetProperty(ref _branchesView, value);
    }

    public BranchDto? SelectedBranch
    {
        get => _selectedBranch;
        set => SetProperty(ref _selectedBranch, value);
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                BranchesView?.Refresh();
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
                _ = LoadBranchesAsync();
            }
        }
    }

    #endregion

    #region Commands

    public ICommand RefreshCommand { get; private set; } = null!;
    public ICommand AddCommand { get; private set; } = null!;
    public ICommand EditCommand { get; private set; } = null!;
    public ICommand DeleteCommand { get; private set; } = null!;
    public ICommand SearchCommand { get; private set; } = null!;

    #endregion

    #region Methods

    public async Task LoadBranchesAsync()
    {
        await ExecuteAsync(LoadBranchesOperationAsync,
            ex => ErrorMessage = HandleException(ex, "BranchListViewModel.LoadBranchesAsync"));
    }

    private async Task LoadBranchesOperationAsync()
    {
        ErrorMessage = null;

        var result = await _branchService.GetAllAsync(IncludeInactive);

        if (result.IsSuccess && result.Value != null)
        {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                Branches.Clear();
                foreach (var item in result.Value.OrderByDescending(x => x.Id))
                {
                    Branches.Add(item);
                }
                SetupCollectionView();
                IsEmpty = Branches.Count == 0;
            });
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل الفروع", "BranchListViewModel.LoadBranchesAsync");
            IsEmpty = Branches.Count == 0;
        }
    }

    private void SetupCollectionView()
    {
        BranchesView = CollectionViewSource.GetDefaultView(Branches);
        BranchesView.Filter = FilterBranches;
    }

    private bool FilterBranches(object obj)
    {
        if (obj is not BranchDto branch) return false;
        if (string.IsNullOrWhiteSpace(SearchText)) return true;

        var search = SearchText.Trim().ToLower();
        return branch.Name.ToLower().Contains(search);
    }

    private void AddBranch()
    {
        var editorVm = App.GetService<BranchEditorViewModel>();
        _screenWindowService.OpenScreen(editorVm, new ScreenWindowOptions
        {
            Title = "فرع جديد",
            Width = 600,
            Height = 500,
            OnClosed = (_) =>
            {
                System.Windows.Application.Current.Dispatcher.InvokeAsync(() => _ = LoadBranchesAsync());
            }
        });
    }

    private void EditBranch()
    {
        if (SelectedBranch == null) return;

        var editorVm = App.GetService<BranchEditorViewModel>();
        editorVm.LoadBranch(SelectedBranch);

        _screenWindowService.OpenScreen(editorVm, new ScreenWindowOptions
        {
            Title = "تعديل فرع",
            Width = 600,
            Height = 500,
            OnClosed = (_) =>
            {
                System.Windows.Application.Current.Dispatcher.InvokeAsync(() => _ = LoadBranchesAsync());
            }
        });
    }

    public async Task DeleteBranchAsync()
    {
        if (SelectedBranch == null) return;

        var strategy = await _dialogService.ShowDeleteConfirmationAsync($"الفرع: {SelectedBranch.Name}");

        if (strategy == DeleteStrategy.Cancel) return;

        var branch = SelectedBranch;
        await ExecuteAsync(() => DeleteBranchOperationAsync(strategy, branch),
            ex => ErrorMessage = HandleException(ex, "BranchListViewModel.DeleteBranchAsync"));
    }

    private async Task DeleteBranchOperationAsync(DeleteStrategy strategy, BranchDto branch)
    {
        ErrorMessage = null;

        if (strategy == DeleteStrategy.Deactivate)
        {
            var result = await _branchService.DeactivateAsync(branch.Id);
            if (result.IsSuccess)
            {
                await LoadBranchesAsync();
                _toastService.ShowSuccess("تم إلغاء تنشيط الفرع بنجاح");
            }
            else
            {
                ErrorMessage = HandleFailure(result.Error ?? "فشل في إلغاء تنشيط الفرع", "BranchListViewModel.DeleteBranchAsync");
                await _dialogService.ShowErrorAsync("خطأ في حذف الفرع", ErrorMessage!);
            }
        }
        else if (strategy == DeleteStrategy.Permanent)
        {
            await _dialogService.ShowErrorAsync("خطأ", "لا يمكن حذف الفرع بشكل نهائي. يمكنك إلغاء تنشيطه فقط.");
        }
    }

    private void Search()
    {
        BranchesView?.Refresh();
    }

    public override void Cleanup()
    {
        base.Cleanup();
    }

    #endregion
}
