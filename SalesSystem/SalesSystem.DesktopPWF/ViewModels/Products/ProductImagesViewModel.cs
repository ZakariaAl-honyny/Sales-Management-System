using Microsoft.Win32;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;
using SalesSystem.DesktopPWF.Messaging.Messages;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using SalesSystem.DesktopPWF.Services.App.Toast;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace SalesSystem.DesktopPWF.ViewModels.Products;

public class ProductImagesViewModel : ViewModelBase, IDisposable
{
    private readonly IProductImageApiService _imageService;
    private readonly IDialogService _dialogService;
    private readonly IEventBus _eventBus;
    private readonly IToastNotificationService _toastService;

    private ObservableCollection<ProductImageDto> _images = new();
    private ProductImageDto? _selectedImage;
    private string? _errorMessage;
    private bool _isEmpty;
    private int _productId;

    public ProductImagesViewModel()
        : this(
            App.GetService<IProductImageApiService>(),
            App.GetService<IDialogService>(),
            App.GetService<IEventBus>(),
            App.GetService<IToastNotificationService>())
    {
    }

    public ProductImagesViewModel(
        IProductImageApiService imageService,
        IDialogService dialogService,
        IEventBus eventBus,
        IToastNotificationService? toastService = null)
    {
        _imageService = imageService ?? throw new ArgumentNullException(nameof(imageService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _toastService = toastService ?? App.GetService<IToastNotificationService>();

        InitializeCommands();
    }

    private void InitializeCommands()
    {
        RefreshCommand = new AsyncRelayCommand(LoadImagesAsync);
        AddCommand = new AsyncRelayCommand(AddImageAsync);
        SetPrimaryCommand = new AsyncRelayCommand(SetPrimaryImageAsync);
        DeleteCommand = new AsyncRelayCommand(DeleteImageAsync);
    }

    public void OnNavigatedTo()
    {
        _eventBus.Subscribe<ProductImageChangedMessage>(OnImageChanged);
        _ = LoadImagesAsync();
    }

    #region Properties

    public ObservableCollection<ProductImageDto> Images
    {
        get => _images;
        set => SetProperty(ref _images, value);
    }

    public ProductImageDto? SelectedImage
    {
        get => _selectedImage;
        set => SetProperty(ref _selectedImage, value);
    }

    public int ProductId
    {
        get => _productId;
        set => SetProperty(ref _productId, value);
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

    #endregion

    #region Commands

    public ICommand RefreshCommand { get; private set; } = null!;
    public ICommand AddCommand { get; private set; } = null!;
    public ICommand SetPrimaryCommand { get; private set; } = null!;
    public ICommand DeleteCommand { get; private set; } = null!;

    #endregion

    #region Methods

    public async Task LoadImagesAsync()
    {
        await ExecuteAsync(LoadImagesOperationAsync);
    }

    private async Task LoadImagesOperationAsync()
    {
        ErrorMessage = null;
        var result = await _imageService.GetByProductAsync(ProductId);

        if (result.IsSuccess && result.Value != null)
        {
            InvokeOnUIThread(() =>
            {
                Images.Clear();
                foreach (var item in result.Value.OrderBy(x => x.SortOrder))
                {
                    Images.Add(item);
                }
                IsEmpty = Images.Count == 0;
            });
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل صور المنتج", "ProductImagesViewModel.LoadImagesOperationAsync", "[ProductImagesViewModel.LoadImagesOperationAsync] Failed to load product images from API.");
            IsEmpty = Images.Count == 0;
        }
    }

    public async Task AddImageAsync()
    {
        await ExecuteAsync(AddImageOperationAsync);
    }

    private async Task AddImageOperationAsync()
    {
        ErrorMessage = null;

        // Open file dialog on UI thread
        string? filePath = null;
        InvokeOnUIThread(() =>
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "اختيار صورة للمنتج",
                Filter = "صور (PNG, JPG, JPEG, GIF, BMP)|*.png;*.jpg;*.jpeg;*.gif;*.bmp|كل الملفات|*.*",
                Multiselect = false,
                CheckFileExists = true
            };

            if (dialog.ShowDialog() == true)
                filePath = dialog.FileName;
        });

        if (string.IsNullOrEmpty(filePath))
            return;

        var request = new CreateProductImageRequest(
            ProductId: ProductId,
            ImagePath: filePath,
            IsPrimary: false,
            SortOrder: 0);

        var result = await _imageService.CreateAsync(request);

        if (result.IsSuccess)
        {
            _eventBus.Publish(new ProductImageChangedMessage(result.Value!.Id));
            await LoadImagesAsync();
            _toastService.ShowSuccess("تمت إضافة الصورة بنجاح");
        }
        else
        {
            var error = result.Error ?? "فشل في إضافة الصورة";
            ErrorMessage = HandleFailure(error, "ProductImagesViewModel.AddImageOperationAsync", "[ProductImagesViewModel.AddImageOperationAsync] Failed to create product image.");
            _toastService.ShowError(ErrorMessage);
        }
    }

    public async Task SetPrimaryImageAsync()
    {
        if (SelectedImage == null) return;

        var imageId = SelectedImage.Id;
        await ExecuteAsync(() => SetPrimaryImageOperationAsync(imageId));
    }

    private async Task SetPrimaryImageOperationAsync(int imageId)
    {
        ErrorMessage = null;
        var result = await _imageService.SetPrimaryAsync(ProductId, imageId);

        if (result.IsSuccess)
        {
            _eventBus.Publish(new ProductImageChangedMessage(imageId));
            await LoadImagesAsync();
            _toastService.ShowSuccess("تم تعيين الصورة كصورة رئيسية");
        }
        else
        {
            var error = result.Error ?? "فشل في تعيين الصورة الرئيسية";
            ErrorMessage = HandleFailure(error, "ProductImagesViewModel.SetPrimaryImageOperationAsync", "[ProductImagesViewModel.SetPrimaryImageOperationAsync] Failed to set primary image.");
            _toastService.ShowError(ErrorMessage);
        }
    }

    public async Task DeleteImageAsync()
    {
        if (SelectedImage == null) return;

        var strategy = await _dialogService.ShowDeleteConfirmationAsync("حذف الصورة");
        if (strategy == DeleteStrategy.Cancel) return;

        var imageId = SelectedImage.Id;
        await ExecuteAsync(() => DeleteImageOperationAsync(imageId));
    }

    private async Task DeleteImageOperationAsync(int imageId)
    {
        ErrorMessage = null;
        var result = await _imageService.DeactivateAsync(imageId);

        if (result.IsSuccess)
        {
            _eventBus.Publish(new ProductImageChangedMessage(imageId));
            await LoadImagesAsync();
            _toastService.ShowSuccess("تم حذف الصورة بنجاح");
        }
        else
        {
            var error = result.Error ?? "فشل في حذف الصورة";
            ErrorMessage = HandleFailure(error, "ProductImagesViewModel.DeleteImageOperationAsync", "[ProductImagesViewModel.DeleteImageOperationAsync] Failed to delete product image.");
            _toastService.ShowError(ErrorMessage);
        }
    }

    private void OnImageChanged(ProductImageChangedMessage msg)
    {
        _ = InvokeOnUIThreadAsync(async () =>
        {
            await LoadImagesAsync();
        });
    }

    public override void Cleanup()
    {
        _eventBus.Unsubscribe<ProductImageChangedMessage>(OnImageChanged);
    }

    public void Dispose()
    {
        Cleanup();
        GC.SuppressFinalize(this);
    }

    #endregion
}
