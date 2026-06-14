using System.Collections.ObjectModel;
using System.Windows.Input;

namespace SalesSystem.DesktopPWF.ViewModels.Products;

public class ProductUnitBuilderViewModel : ViewModelBase
{
    public ObservableCollection<ProductUnitRowViewModel> Units { get; } = new();

    private string _validationSummary = string.Empty;
    public string ValidationSummary
    {
        get => _validationSummary;
        private set => SetProperty(ref _validationSummary, value);
    }

    private bool _hasValidationError;
    public bool HasValidationError
    {
        get => _hasValidationError;
        private set => SetProperty(ref _hasValidationError, value);
    }

    private bool _showOnboarding = true;

    public ICommand AddUnitCommand { get; }
    public ICommand RemoveUnitCommand { get; }
    public ICommand ShowHelpCommand { get; }

    public ProductUnitBuilderViewModel()
    {
        AddUnitCommand = new RelayCommand(AddNewUnit);
        RemoveUnitCommand = new RelayCommand<ProductUnitRowViewModel>(RemoveUnit);
        ShowHelpCommand = new RelayCommand(ShowOnboarding);

        Units.CollectionChanged += (_, _) => Validate();
    }

    public void Initialize(List<ProductUnitRowViewModel>? existingUnits = null)
    {
        Units.Clear();

        if (existingUnits?.Any() == true)
        {
            _showOnboarding = false;
            foreach (var unit in existingUnits.OrderBy(u => u.SortOrder))
                AddUnitWithChangeTracking(unit);
        }
        else
        {
            ShowOnboarding();
            AddBaseUnitRow();
        }
    }

    private void AddBaseUnitRow()
    {
        var baseRow = new ProductUnitRowViewModel
        {
            IsBaseUnit = true,
            Factor = 1,
            SortOrder = 0,
            Placeholder_UnitName = "حبة، قطعة، بيضة"
        };
        baseRow.PropertyChanged += (_, _) => Validate();
        Units.Add(baseRow);
    }

    private void AddNewUnit()
    {
        var row = new ProductUnitRowViewModel
        {
            IsBaseUnit = false,
            SortOrder = Units.Count,
            Placeholder_UnitName = "طبق، كرتون، صندق"
        };
        row.PropertyChanged += (_, _) => Validate();
        Units.Add(row);
        _showOnboarding = false;
    }

    private void AddUnitWithChangeTracking(ProductUnitRowViewModel unit)
    {
        unit.PropertyChanged += (_, _) => Validate();
        Units.Add(unit);
    }

    private void RemoveUnit(ProductUnitRowViewModel? unit)
    {
        if (unit == null) return;

        if (unit.IsBaseUnit && Units.Count > 1)
        {
            ValidationSummary = "لا يمكن حذف الوحدة الأساسية";
            HasValidationError = true;
            return;
        }

        Units.Remove(unit);
        Validate();
    }

    public bool Validate()
    {
        var errors = new List<string>();

        var baseUnits = Units.Where(u => u.IsBaseUnit).ToList();

        if (baseUnits.Count == 0)
            errors.Add("أضف وحدة صغرى واحدة (مثال: حبة)");

        if (baseUnits.Count > 1)
            errors.Add("لا يمكن تعريف أكثر من وحدة صغرى واحدة");

        foreach (var unit in Units)
        {
            if (string.IsNullOrWhiteSpace(unit.UnitName))
                errors.Add($"الصف {unit.SortOrder + 1}: اسم الوحدة مطلوب");

            if (!unit.IsBaseUnit && !unit.IsFactorValid)
                errors.Add($"'{unit.UnitName}': معامل التحويل يجب أن يكون أكبر من 1");
        }

        ValidationSummary = errors.Any()
            ? string.Join("\n", errors)
            : "✓ الوحدات صحيحة";

        HasValidationError = errors.Any();
        return !errors.Any();
    }

    private void ShowOnboarding()
    {
        if (!_showOnboarding) return;
        _showOnboarding = false;
    }
}

public class RelayCommand<T> : ICommand
{
    private readonly Action<T?> _execute;
    private readonly Func<T?, bool>? _canExecute;
    private EventHandler? _canExecuteChanged;

    public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged
    {
        add
        {
            CommandManager.RequerySuggested += value;
            _canExecuteChanged += value;
        }
        remove
        {
            CommandManager.RequerySuggested -= value;
            _canExecuteChanged -= value;
        }
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke((T?)parameter) ?? true;

    public void Execute(object? parameter) => _execute((T?)parameter);

    public void RaiseCanExecuteChanged()
    {
        CommandManager.InvalidateRequerySuggested();
        _canExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}