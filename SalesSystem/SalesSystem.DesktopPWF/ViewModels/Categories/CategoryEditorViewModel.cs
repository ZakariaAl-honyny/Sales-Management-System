using SalesSystem.DesktopPWF.Messaging.Messages;
using System.Windows.Input;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using System.Collections.Generic;

namespace SalesSystem.DesktopPWF.ViewModels.Categories;

public class CategoryEditorViewModel : ViewModelBase
{
    private readonly ICategoryApiService _categoryService;
    private readonly IEventBus _eventBus;
    private readonly IDialogService _dialogService;
    private string _name = string.Empty;
    private string? _errorMessage;
    private string _windowTitle = "إضافة تصنيف جديد";

    public CategoryEditorViewModel()
    {
        _categoryService = App.GetService<ICategoryApiService>();
        _eventBus = App.GetService<IEventBus>();
        _dialogService = App.GetService<IDialogService>();
        SetDialogService(_dialogService);
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
        SaveCommand = new AsyncRelayCommand((Func<Task>)(async () => await ExecuteAsync(SaveOperationAsync, "جاري حفظ التصنيف...")));
        CancelCommand = new RelayCommand(() => RequestClose());
    }

    #region Properties

    public string Name
    {
        get => _name;
        set
        {
            if (SetProperty(ref _name, value))
            {
                if (string.IsNullOrWhiteSpace(value))
                    AddError(nameof(Name), "اسم التصنيف مطلوب");
                else
                    ClearErrors(nameof(Name));
            }
        }
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

    private async Task SaveOperationAsync()
    {
        if (!await ValidateAsync()) return;

        ErrorMessage = null;

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
            await _dialogService.ShowErrorAsync("خطأ في حفظ التصنيف", ErrorMessage!);
        }
    }

    private async Task<bool> ValidateAsync()
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(Name))
            errors.Add("• اسم التصنيف مطلوب");

        if (errors.Any())
        {
            await _dialogService.ShowValidationErrorsAsync("بيانات غير مكتملة", errors);
            RequestFocusFirstInvalidField();
            return false;
        }
        return true;
    }

    #endregion
}
