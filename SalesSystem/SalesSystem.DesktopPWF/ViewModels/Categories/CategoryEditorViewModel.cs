using SalesSystem.DesktopPWF.Messaging.Messages;
using System.Windows.Input;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;

namespace SalesSystem.DesktopPWF.ViewModels.Categories;

public class CategoryEditorViewModel : ViewModelBase
{
    private readonly ICategoryApiService _categoryService;
    private readonly IEventBus _eventBus;
    private string _name = string.Empty;
    private bool _isLoading;
    private string? _errorMessage;
    private string _windowTitle = "إضافة تصنيف جديد";

    public CategoryEditorViewModel()
    {
        _categoryService = App.GetService<ICategoryApiService>();
        _eventBus = App.GetService<IEventBus>();
        InitializeCommands();
    }

    public void LoadCategory(CategoryDto category)
    {
        _categoryDto = category;
        Name = category.Name;
        WindowTitle = $"تعديل تصنيف: {category.Name}";
    }

    private CategoryDto? _categoryDto;

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
            Result<CategoryDto> result;
            if (_categoryDto == null)
            {
                var request = new CreateCategoryRequest(Name, null);
                result = await _categoryService.CreateAsync(request);
            }
            else
            {
                var request = new UpdateCategoryRequest(Name, _categoryDto.Description, _categoryDto.IsActive);
                result = await _categoryService.UpdateAsync(_categoryDto.Id, request);
            }

            if (result.IsSuccess && result.Value != null)
            {
                _eventBus.Publish(new CategoryChangedMessage(result.Value.Id));
                RequestClose();
            }
            else
            {
                ErrorMessage = HandleFailure(result.Error ?? "فشل في حفظ التصنيف", "CategoryEditorViewModel.SaveAsync");
                System.Windows.MessageBox.Show(ErrorMessage, "خطأ في الحفظ", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = HandleException(ex, "CategoryEditorViewModel.SaveAsync", "Failed to save category data.");
            System.Windows.MessageBox.Show(ErrorMessage, "خطأ", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    #endregion
}
