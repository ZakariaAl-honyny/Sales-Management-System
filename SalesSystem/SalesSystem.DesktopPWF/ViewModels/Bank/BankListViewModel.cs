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

namespace SalesSystem.DesktopPWF.ViewModels.Bank;

public class BankListViewModel : ViewModelBase
{
    private readonly IBankApiService _bankService;
    private readonly IDialogService _dialogService;
    private readonly IToastNotificationService _toastService;
    private readonly IScreenWindowService _screenWindowService;

    private ObservableCollection<BankDto> _banks = new();
    private ICollectionView? _banksView;
    private BankDto? _selectedBank;
    private string _searchText = string.Empty;
    private string? _errorMessage;
    private bool _isEmpty;
    private bool _includeInactive;

    public BankListViewModel()
    {
        _bankService = App.GetService<IBankApiService>();
        _dialogService = App.GetService<IDialogService>();
        _toastService = App.GetService<IToastNotificationService>();
        _screenWindowService = App.GetService<IScreenWindowService>();
        SetDialogService(_dialogService);

        InitializeCommands();
    }

    private void InitializeCommands()
    {
        RefreshCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExecuteAsync(LoadBanksOperationAsync,
                ex => ErrorMessage = HandleException(ex, "BankListViewModel.LoadBanksAsync"))));
        AddCommand = new RelayCommand(AddBank);
        EditCommand = new RelayCommand(EditBank);
        DeleteCommand = new AsyncRelayCommand(DeleteBankAsync);
        SearchCommand = new RelayCommand(Search);
    }

    #region Properties

    public ObservableCollection<BankDto> Banks
    {
        get => _banks;
        set => SetProperty(ref _banks, value);
    }

    public ICollectionView? BanksView
    {
        get => _banksView;
        private set => SetProperty(ref _banksView, value);
    }

    public BankDto? SelectedBank
    {
        get => _selectedBank;
        set => SetProperty(ref _selectedBank, value);
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                BanksView?.Refresh();
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
                _ = LoadBanksAsync();
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

    public async Task LoadBanksAsync()
    {
        await ExecuteAsync(LoadBanksOperationAsync,
            ex => ErrorMessage = HandleException(ex, "BankListViewModel.LoadBanksAsync"));
    }

    private async Task LoadBanksOperationAsync()
    {
        ErrorMessage = null;

        var result = await _bankService.GetAllAsync(IncludeInactive);

        if (result.IsSuccess && result.Value != null)
        {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                Banks.Clear();
                foreach (var item in result.Value.OrderByDescending(x => x.Id))
                {
                    Banks.Add(item);
                }
                SetupCollectionView();
                IsEmpty = Banks.Count == 0;
            });
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل البنوك", "BankListViewModel.LoadBanksAsync");
            IsEmpty = Banks.Count == 0;
        }
    }

    private void SetupCollectionView()
    {
        BanksView = CollectionViewSource.GetDefaultView(Banks);
        BanksView.Filter = FilterBanks;
    }

    private bool FilterBanks(object obj)
    {
        if (obj is not BankDto bank) return false;
        if (string.IsNullOrWhiteSpace(SearchText)) return true;

        var search = SearchText.Trim().ToLower();
        return bank.Name.ToLower().Contains(search) ||
               (bank.AccountName?.ToLower().Contains(search) ?? false);
    }

    private void AddBank()
    {
        var editorVm = App.GetService<BankEditorViewModel>();
        _screenWindowService.OpenScreen(editorVm, new ScreenWindowOptions
        {
            Title = "بنك جديد",
            Width = 650,
            Height = 550,
            OnClosed = (_) =>
            {
                System.Windows.Application.Current.Dispatcher.InvokeAsync(() => _ = LoadBanksAsync());
            }
        });
    }

    private void EditBank()
    {
        if (SelectedBank == null) return;

        var editorVm = App.GetService<BankEditorViewModel>();
        editorVm.LoadBank(SelectedBank);

        _screenWindowService.OpenScreen(editorVm, new ScreenWindowOptions
        {
            Title = "تعديل بنك",
            Width = 650,
            Height = 550,
            OnClosed = (_) =>
            {
                System.Windows.Application.Current.Dispatcher.InvokeAsync(() => _ = LoadBanksAsync());
            }
        });
    }

    public async Task DeleteBankAsync()
    {
        if (SelectedBank == null) return;

        var strategy = await _dialogService.ShowDeleteConfirmationAsync($"البنك: {SelectedBank.Name}");

        if (strategy == DeleteStrategy.Cancel) return;

        var bank = SelectedBank;
        await ExecuteAsync(() => DeleteBankOperationAsync(strategy, bank),
            ex => ErrorMessage = HandleException(ex, "BankListViewModel.DeleteBankAsync"));
    }

    private async Task DeleteBankOperationAsync(DeleteStrategy strategy, BankDto bank)
    {
        ErrorMessage = null;

        if (strategy == DeleteStrategy.Deactivate)
        {
            var result = await _bankService.DeactivateAsync(bank.Id);
            if (result.IsSuccess)
            {
                await LoadBanksAsync();
                _toastService.ShowSuccess("تم إلغاء تنشيط البنك بنجاح");
            }
            else
            {
                ErrorMessage = HandleFailure(result.Error ?? "فشل في إلغاء تنشيط البنك", "BankListViewModel.DeleteBankAsync");
            }
        }
        else if (strategy == DeleteStrategy.Permanent)
        {
            await _dialogService.ShowErrorAsync("خطأ", "لا يمكن حذف البنك بشكل نهائي. يمكنك إلغاء تنشيطه فقط.");
        }
    }

    private void Search()
    {
        BanksView?.Refresh();
    }

    public override void Cleanup()
    {
        base.Cleanup();
    }

    #endregion
}
