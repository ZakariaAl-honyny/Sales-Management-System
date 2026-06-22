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

namespace SalesSystem.DesktopPWF.ViewModels.Party;

public class PartyListViewModel : ViewModelBase
{
    private readonly IPartyApiService _partyService;
    private readonly IDialogService _dialogService;
    private readonly IToastNotificationService _toastService;
    private readonly IScreenWindowService _screenWindowService;

    private ObservableCollection<PartyDto> _parties = new();
    private ICollectionView? _partiesView;
    private PartyDto? _selectedParty;
    private string _searchText = string.Empty;
    private string? _errorMessage;
    private bool _isEmpty;
    private bool _includeInactive;

    public PartyListViewModel()
    {
        _partyService = App.GetService<IPartyApiService>();
        _dialogService = App.GetService<IDialogService>();
        _toastService = App.GetService<IToastNotificationService>();
        _screenWindowService = App.GetService<IScreenWindowService>();
        SetDialogService(_dialogService);

        InitializeCommands();
    }

    private void InitializeCommands()
    {
        RefreshCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExecuteAsync(LoadPartiesOperationAsync,
                ex => ErrorMessage = HandleException(ex, "PartyListViewModel.LoadPartiesAsync"))));
        AddCommand = new RelayCommand(AddParty);
        EditCommand = new RelayCommand(EditParty);
        DeleteCommand = new AsyncRelayCommand(DeletePartyAsync);
        SearchCommand = new RelayCommand(Search);
    }

    #region Properties

    public ObservableCollection<PartyDto> Parties
    {
        get => _parties;
        set => SetProperty(ref _parties, value);
    }

    public ICollectionView? PartiesView
    {
        get => _partiesView;
        private set => SetProperty(ref _partiesView, value);
    }

    public PartyDto? SelectedParty
    {
        get => _selectedParty;
        set => SetProperty(ref _selectedParty, value);
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                PartiesView?.Refresh();
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
                _ = LoadPartiesAsync();
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

    public async Task LoadPartiesAsync()
    {
        await ExecuteAsync(LoadPartiesOperationAsync,
            ex => ErrorMessage = HandleException(ex, "PartyListViewModel.LoadPartiesAsync"));
    }

    private async Task LoadPartiesOperationAsync()
    {
        ErrorMessage = null;

        var result = await _partyService.GetAllAsync(IncludeInactive);

        if (result.IsSuccess && result.Value != null)
        {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                Parties.Clear();
                foreach (var item in result.Value.OrderByDescending(x => x.Id))
                {
                    Parties.Add(item);
                }
                SetupCollectionView();
                IsEmpty = Parties.Count == 0;
            });
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل الأشخاص", "PartyListViewModel.LoadPartiesAsync");
            await _dialogService.ShowErrorAsync("خطأ في تحميل البيانات", ErrorMessage!);
            IsEmpty = Parties.Count == 0;
        }
    }

    private void SetupCollectionView()
    {
        PartiesView = CollectionViewSource.GetDefaultView(Parties);
        PartiesView.Filter = FilterParties;
    }

    private bool FilterParties(object obj)
    {
        if (obj is not PartyDto party) return false;
        if (string.IsNullOrWhiteSpace(SearchText)) return true;

        var search = SearchText.Trim().ToLower();
        return party.Name.ToLower().Contains(search) ||
               (party.Phone?.ToLower().Contains(search) ?? false) ||
               (party.Email?.ToLower().Contains(search) ?? false);
    }

    private void AddParty()
    {
        var editorVm = App.GetService<PartyEditorViewModel>();
        _screenWindowService.OpenScreen(editorVm, new ScreenWindowOptions
        {
            Title = "شخص جديد",
            Width = 650,
            Height = 580,
            OnClosed = (_) =>
            {
                System.Windows.Application.Current.Dispatcher.InvokeAsync(() => _ = LoadPartiesAsync());
            }
        });
    }

    private void EditParty()
    {
        if (SelectedParty == null) return;

        var editorVm = App.GetService<PartyEditorViewModel>();
        editorVm.LoadParty(SelectedParty);

        _screenWindowService.OpenScreen(editorVm, new ScreenWindowOptions
        {
            Title = "تعديل شخص",
            Width = 650,
            Height = 580,
            OnClosed = (_) =>
            {
                System.Windows.Application.Current.Dispatcher.InvokeAsync(() => _ = LoadPartiesAsync());
            }
        });
    }

    public async Task DeletePartyAsync()
    {
        if (SelectedParty == null) return;

        var strategy = await _dialogService.ShowDeleteConfirmationAsync($"الشخص: {SelectedParty.Name}");

        if (strategy == DeleteStrategy.Cancel) return;

        var party = SelectedParty;
        await ExecuteAsync(() => DeletePartyOperationAsync(strategy, party),
            ex => ErrorMessage = HandleException(ex, "PartyListViewModel.DeletePartyAsync"));
    }

    private async Task DeletePartyOperationAsync(DeleteStrategy strategy, PartyDto party)
    {
        ErrorMessage = null;

        if (strategy == DeleteStrategy.Deactivate)
        {
            var result = await _partyService.DeactivateAsync(party.Id);
            if (result.IsSuccess)
            {
                await LoadPartiesAsync();
                _toastService.ShowSuccess("تم إلغاء تنشيط الشخص بنجاح");
            }
            else
            {
                ErrorMessage = HandleFailure(result.Error ?? "فشل في إلغاء تنشيط الشخص", "PartyListViewModel.DeletePartyAsync");
                await _dialogService.ShowErrorAsync("خطأ في إلغاء تنشيط الشخص", ErrorMessage!);
            }
        }
        else if (strategy == DeleteStrategy.Permanent)
        {
            await _dialogService.ShowErrorAsync("خطأ", "لا يمكن حذف الشخص بشكل نهائي. يمكنك إلغاء تنشيطه فقط.");
        }
    }

    private void Search()
    {
        PartiesView?.Refresh();
    }

    public override void Cleanup()
    {
        base.Cleanup();
    }

    #endregion
}
