using SalesSystem.DesktopPWF.Messaging.Messages;
using SalesSystem.DesktopPWF.Services.App.Toast;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Responses;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;

namespace SalesSystem.DesktopPWF.ViewModels.Attachments;

/// <summary>
/// ViewModel for Attachments List View — Read-only display of system attachments
/// </summary>
public class AttachmentListViewModel : ViewModelBase
{
    private readonly IAttachmentApiService _attachmentService;
    private readonly IDialogService _dialogService;
    private readonly IToastNotificationService _toastService;

    private ObservableCollection<AttachmentDto> _attachments = new();
    private ICollectionView? _attachmentsView;
    private AttachmentDto? _selectedAttachment;
    private string _searchText = string.Empty;
    private string? _errorMessage;
    private bool _isEmpty;
    private string? _referenceTypeFilter;
    private int? _referenceIdFilter;

    public AttachmentListViewModel()
    {
        _attachmentService = App.GetService<IAttachmentApiService>();
        _dialogService = App.GetService<IDialogService>();
        _toastService = App.GetService<IToastNotificationService>();

        InitializeCommands();
    }

    public AttachmentListViewModel(
        IAttachmentApiService attachmentService,
        IDialogService dialogService,
        IToastNotificationService toastService)
    {
        _attachmentService = attachmentService ?? throw new ArgumentNullException(nameof(attachmentService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _toastService = toastService ?? throw new ArgumentNullException(nameof(toastService));

        InitializeCommands();
    }

    private void InitializeCommands()
    {
        RefreshCommand = new AsyncRelayCommand(LoadAttachmentsAsync);
        DeleteCommand = new AsyncRelayCommand(DeleteAttachmentAsync);
        SearchCommand = new RelayCommand(Search);
    }

    #region Properties
    public ObservableCollection<AttachmentDto> Attachments
    {
        get => _attachments;
        set => SetProperty(ref _attachments, value);
    }

    public ICollectionView? AttachmentsView
    {
        get => _attachmentsView;
        private set => SetProperty(ref _attachmentsView, value);
    }

    public AttachmentDto? SelectedAttachment
    {
        get => _selectedAttachment;
        set => SetProperty(ref _selectedAttachment, value);
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                AttachmentsView?.Refresh();
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

    public string? ReferenceTypeFilter
    {
        get => _referenceTypeFilter;
        set
        {
            if (SetProperty(ref _referenceTypeFilter, value))
                _ = LoadAttachmentsAsync();
        }
    }

    public int? ReferenceIdFilter
    {
        get => _referenceIdFilter;
        set
        {
            if (SetProperty(ref _referenceIdFilter, value))
                _ = LoadAttachmentsAsync();
        }
    }

    public int AttachmentsCount => Attachments.Count;
    #endregion

    #region Commands
    public ICommand RefreshCommand { get; private set; } = null!;
    public ICommand DeleteCommand { get; private set; } = null!;
    public ICommand SearchCommand { get; private set; } = null!;
    #endregion

    #region Methods
    public async Task LoadAttachmentsAsync()
    {
        await ExecuteAsync(LoadAttachmentsOperationAsync);
    }

    private async Task LoadAttachmentsOperationAsync()
    {
        ErrorMessage = null;
        var result = await _attachmentService.GetAllAsync(ReferenceTypeFilter, ReferenceIdFilter);

        if (result.IsSuccess && result.Value != null)
        {
            InvokeOnUIThread(() =>
            {
                Attachments.Clear();
                foreach (var item in result.Value.OrderByDescending(x => x.Id))
                {
                    Attachments.Add(item);
                }
                SetupCollectionView();
                IsEmpty = Attachments.Count == 0;
                OnPropertyChanged(nameof(AttachmentsCount));
            });
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل المرفقات", "AttachmentListViewModel.LoadAttachmentsAsync", "[AttachmentListViewModel.LoadAttachmentsAsync] Failed to load attachments.");
            await _dialogService.ShowErrorAsync("خطأ في تحميل البيانات", ErrorMessage!);
            IsEmpty = Attachments.Count == 0;
        }
    }

    private void SetupCollectionView()
    {
        AttachmentsView = new ListCollectionView(Attachments);
        AttachmentsView.Filter = FilterAttachments;
    }

    private bool FilterAttachments(object obj)
    {
        if (obj is not AttachmentDto attachment) return false;

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var searchLower = SearchText.Trim().ToLower();
            if (!attachment.FileName.ToLower().Contains(searchLower) &&
                !attachment.ReferenceType.ToLower().Contains(searchLower))
                return false;
        }

        return true;
    }

    public async Task DeleteAttachmentAsync()
    {
        if (SelectedAttachment == null)
        {
            await _dialogService.ShowWarningAsync("تنبيه", "الرجاء اختيار مرفق");
            return;
        }

        var confirmed = await _dialogService.ShowConfirmationAsync(
            "تأكيد الحذف",
            $"هل أنت متأكد من حذف المرفق \"{SelectedAttachment.FileName}\"؟");
        if (!confirmed) return;

        await ExecuteAsync(DeleteAttachmentOperationAsync);
    }

    private async Task DeleteAttachmentOperationAsync()
    {
        ErrorMessage = null;
        var result = await _attachmentService.DeleteAsync(SelectedAttachment!.Id);

        if (result.IsSuccess)
        {
            await LoadAttachmentsAsync();
            _toastService.ShowSuccess("تم حذف المرفق بنجاح");
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في حذف المرفق", "AttachmentListViewModel.DeleteAttachmentAsync", $"[AttachmentListViewModel.DeleteAttachmentAsync] Failed to delete attachment {SelectedAttachment.Id}.");
            await _dialogService.ShowErrorAsync("خطأ في الحذف", ErrorMessage!);
        }
    }

    private void Search()
    {
        AttachmentsView?.Refresh();
    }
    #endregion
}
