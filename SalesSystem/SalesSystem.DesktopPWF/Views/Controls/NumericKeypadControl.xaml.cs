using System.Windows;
using System.Windows.Controls;

namespace SalesSystem.DesktopPWF.Views.Controls;

/// <summary>
/// A reusable touch-friendly numeric keypad control with a DependencyProperty
/// for two-way binding to a string input value.
/// </summary>
public partial class NumericKeypadControl : UserControl
{
    /// <summary>
    /// Identifies the <see cref="InputValue"/> dependency property.
    /// Default binding mode is TwoWay.
    /// </summary>
    public static readonly DependencyProperty InputValueProperty =
        DependencyProperty.Register(
            nameof(InputValue),
            typeof(string),
            typeof(NumericKeypadControl),
            new FrameworkPropertyMetadata(
                string.Empty,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnInputValueChanged));

    /// <summary>
    /// Gets or sets the current input value displayed and manipulated by the keypad.
    /// </summary>
    public string InputValue
    {
        get => (string)GetValue(InputValueProperty);
        set => SetValue(InputValueProperty, value ?? string.Empty);
    }

    public NumericKeypadControl()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Called when a digit button (0–9) is clicked.
    /// Appends the button's content digit to <see cref="InputValue"/>.
    /// </summary>
    private void NumberButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Content: string digit })
        {
            InputValue += digit;
        }
    }

    /// <summary>
    /// Called when the Backspace (⌫) button is clicked.
    /// Removes the last character from <see cref="InputValue"/>.
    /// </summary>
    private void BackspaceButton_Click(object sender, RoutedEventArgs e)
    {
        if (InputValue.Length > 0)
        {
            InputValue = InputValue[..^1];
        }
    }

    /// <summary>
    /// Called when the Clear (مسح) button is clicked.
    /// Sets <see cref="InputValue"/> to an empty string.
    /// </summary>
    private void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        InputValue = string.Empty;
    }

    /// <summary>
    /// Optional callback for derived or attached behaviours when the value changes.
    /// </summary>
    private static void OnInputValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        // Reserved for future use — no current logic required.
    }
}
