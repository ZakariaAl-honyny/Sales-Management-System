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

namespace SalesSystem.DesktopPWF.ViewModels.Categories;

public class CategoryListViewModel : ViewModelBase
{
    private readonly ICategoryApiService _categoryService;
    private readonly IEventBus _eventBus;
    private readonly IDialogService _dialogService;
    private readonly IToastNotificationService _toastService;

    private ObservableCollection<CategoryDto> _categories = new();
    private ICollectionView? _categoriesView;
    private CategoryDto? _selectedCategory;
    private string _searchText = string.Empty;
    private string? _errorMessage;
    private bool _isEmpty;
    private bool _includeInactive;

    public CategoryListViewModel()
    {
        _categoryService = App.GetService<ICategoryApiService>();
        _eventBus = App.GetService<IEventBus>();
        _dialogService = App.GetService<IDialogService>();
        _toastService = App.GetService<IToastNotificationService>();

        InitializeCommands();
    }

    private void InitializeCommands()
    {
        RefreshCommand = new AsyncRelayCommand(LoadCategoriesAsync);
        AddCommand = new RelayCommand(AddCategory);
        EditCommand = new RelayCommand(EditCategory, () => SelectedCategory != null);
        DeleteCommand = new AsyncRelayCommand(DeleteCategoryAsync, () => SelectedCategory != null && SelectedCategory.IsActive);
        RestoreCommand = new AsyncRelayCommand(RestoreCategoryAsync, () => SelectedCategory != null && !SelectedCategory.IsActive);
        SearchCommand = new RelayCommand(Search);
        
        // Subscribe to category changes
        _eventBus.Subscribe<CategoryChangedMessage>(OnCategoryChanged);
    }

    #region Properties

    public ObservableCollection<CategoryDto> Categories
    {
        get => _categories;
        set => SetProperty(ref _categories, value);
    }

    public ICollectionView? CategoriesView
    {
        get => _categoriesView;
        private set => SetProperty(ref _categoriesView, value);
    }

    public CategoryDto? SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (SetProperty(ref _selectedCategory, value))
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
                CategoriesView?.Refresh();
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
                _ = LoadCategoriesAsync();
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

    public async Task LoadCategoriesAsync()
    {
        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var result = await _categoryService.GetAllAsync(IncludeInactive);

            if (result.IsSuccess && result.Value != null)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    Categories.Clear();
                    foreach (var item in result.Value.OrderByDescending(x => x.Id))
                    {
                        Categories.Add(item);
                    }
                    SetupCollectionView();
                    IsEmpty = Categories.Count == 0;
                });
            }
            else
            {
                ErrorMessage = HandleFailure(result.Error ?? "ظپط´ظ„ ظپظٹ طھط­ظ…ظٹظ„ ط§ظ„طھطµظ†ظٹظپط§طھ", "CategoryListViewModel.LoadCategoriesAsync", "[CategoryListViewModel.LoadCategoriesAsync] Failed to load categories list.");
                IsEmpty = Categories.Count == 0;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = HandleException(ex, "CategoryListViewModel.LoadCategoriesAsync", "[CategoryListViewModel.LoadCategoriesAsync] Failed to load categories list.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void SetupCollectionView()
    {
        CategoriesView = CollectionViewSource.GetDefaultView(Categories);
        CategoriesView.Filter = FilterCategories;
    }

    private bool FilterCategories(object obj)
    {
        if (obj is not CategoryDto category) return false;

        if (string.IsNullOrWhiteSpace(SearchText)) return true;

        var searchLower = SearchText.Trim().ToLower();
        return category.Name.ToLower().Contains(searchLower);
    }

    private void AddCategory()
    {
        var editorVm = new CategoryEditorViewModel();
        if (_dialogService.ShowDialog(editorVm))
        {
            _ = LoadCategoriesAsync();
        }
    }

    private void EditCategory()
    {
        if (SelectedCategory == null) return;

        var editorVm = new CategoryEditorViewModel();
        editorVm.LoadCategory(SelectedCategory);

        if (_dialogService.ShowDialog(editorVm))
        {
            _ = LoadCategoriesAsync();
        }
    }

public async Task DeleteCategoryAsync()
    {
        if (SelectedCategory == null) return;

        var strategy = await _dialogService.ShowDeleteConfirmationAsync($"ط§ظ„طھطµظ†ظٹظپ: {SelectedCategory.Name}");

        if (strategy == DeleteStrategy.Cancel) return;

        IsBusy = true;
        ErrorMessage = null;

        try
        {
            if (strategy == DeleteStrategy.Deactivate)
            {
                var deleteResult = await _categoryService.DeleteAsync(SelectedCategory.Id);
                if (deleteResult.IsSuccess)
                {
                    await LoadCategoriesAsync();
                    _toastService.ShowSuccess("طھظ… ط¥ظ„ط؛ط§ط، طھظ†ط´ظٹط· ط§ظ„طھطµظ†ظٹظپ ط¨ظ†ط¬ط§ط­");
                }
                else
                {
                    ErrorMessage = deleteResult.Error ?? "ظپط´ظ„ ظپظٹ ط¥ظ„ط؛ط§ط، طھظ†ط´ظٹط· ط§ظ„طھطµظ†ظٹظپ";
                }
            }
            else if (strategy == DeleteStrategy.Permanent)
            {
                var deleteResult = await _categoryService.DeletePermanentlyAsync(SelectedCategory.Id);
                if (deleteResult.IsSuccess)
                {
                    await LoadCategoriesAsync();
                    _toastService.ShowSuccess("طھظ… ط­ط°ظپ ط§ظ„طھطµظ†ظٹظپ ظ†ظ‡ط§ط¦ظٹط§ظ‹");
                }
                else
                {
                    var error = deleteResult.Error ?? "ظپط´ظ„ ظپظٹ ط­ط°ظپ ط§ظ„طھطµظ†ظٹظپ";
                    ErrorMessage = error;
                    LogSystemError($"Hard delete failed for Category {SelectedCategory.Id}: {error}", "CategoryListViewModel.DeleteCategoryAsync");
                }
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = "ط­ط¯ط« ط®ط·ط£ ط؛ظٹط± ظ…طھظˆظ‚ط¹ ط£ط«ظ†ط§ط، ط§ظ„ط­ط°ظپ";
            HandleException(ex, "CategoryListViewModel.DeleteCategoryAsync", $"[CategoryListViewModel.DeleteCategoryAsync] Failed to delete category with ID {SelectedCategory?.Id}.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task RestoreCategoryAsync()
    {
        if (SelectedCategory == null) return;

        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var request = new UpdateCategoryRequest(
                Name: SelectedCategory.Name,
                Description: SelectedCategory.Description,
                IsActive: true
            );

            var result = await _categoryService.UpdateAsync(SelectedCategory.Id, request);

            if (result.IsSuccess)
            {
                await LoadCategoriesAsync();
                await _dialogService.ShowSuccessAsync("ظ†ط¬ط§ط­", "طھظ… ط§ط³طھط¹ط§ط¯ط© ط§ظ„طھطµظ†ظٹظپ ط¨ظ†ط¬ط§ط­");
            }
            else
            {
                ErrorMessage = result.Error ?? "ظپط´ظ„ ظپظٹ ط§ط³طھط¹ط§ط¯ط© ط§ظ„طھطµظ†ظٹظپ";
                await _dialogService.ShowErrorAsync("ط®ط·ط£ ظپظٹ ط§ظ„ط§ط³طھط¹ط§ط¯ط©", ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = "ط­ط¯ط« ط®ط·ط£ ط؛ظٹط± ظ…طھظˆظ‚ط¹ ط£ط«ظ†ط§ط، ط§ط³طھط¹ط§ط¯ط© ط§ظ„طھطµظ†ظٹظپ";
            HandleException(ex, "CategoryListViewModel.RestoreCategoryAsync", $"[CategoryListViewModel.RestoreCategoryAsync] Failed to restore category with ID {SelectedCategory?.Id}.");
        }
finally
        {
            IsBusy = false;
        }
    }

    private void Search()
    {
        CategoriesView?.Refresh();
    }

    private void OnCategoryChanged(CategoryChangedMessage msg)
    {
        System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
        {
            await LoadCategoriesAsync();
        });
    }

    public override void Cleanup()
    {
        _eventBus.Unsubscribe<CategoryChangedMessage>(OnCategoryChanged);
    }
    #endregion
}




