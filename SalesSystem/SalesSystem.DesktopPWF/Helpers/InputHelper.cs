using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SalesSystem.DesktopPWF.Helpers;

public static class InputHelper
{
    // ═══════════════════════════════════════════════════════════
    // EnterKeyCommand — existing behavior (unchanged)
    // ═══════════════════════════════════════════════════════════
    public static readonly DependencyProperty EnterKeyCommandProperty =
        DependencyProperty.RegisterAttached(
            "EnterKeyCommand",
            typeof(ICommand),
            typeof(InputHelper),
            new PropertyMetadata(null, OnEnterKeyCommandChanged));

    public static ICommand GetEnterKeyCommand(DependencyObject obj)
    {
        return (ICommand)obj.GetValue(EnterKeyCommandProperty);
    }

    public static void SetEnterKeyCommand(DependencyObject obj, ICommand value)
    {
        obj.SetValue(EnterKeyCommandProperty, value);
    }

    private static void OnEnterKeyCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is UIElement element)
        {
            if (e.NewValue != null)
            {
                element.KeyDown += Element_KeyDown;
            }
            else
            {
                element.KeyDown -= Element_KeyDown;
            }
        }
    }

    private static void Element_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && sender is DependencyObject depObj)
        {
            var command = GetEnterKeyCommand(depObj);
            if (command != null && command.CanExecute(null))
            {
                command.Execute(null);
                e.Handled = true;
            }
        }
    }

    // ═══════════════════════════════════════════════════════════
    // NumericOnly — blocks non-numeric keystrokes on a TextBox
    //
    // Usage:  <TextBox helpers:InputHelper.NumericOnly="True" .../>
    //
    // Allows: 0-9, Arabic digits ٠-٩, decimal point (.), comma (,),
    //         minus (-), backspace, delete, tab, arrows, home/end
    // ═══════════════════════════════════════════════════════════
    private static readonly Regex _numericRegex = new(@"^[0-9٠-٩.,\-]$", RegexOptions.Compiled);

    public static readonly DependencyProperty NumericOnlyProperty =
        DependencyProperty.RegisterAttached(
            "NumericOnly",
            typeof(bool),
            typeof(InputHelper),
            new PropertyMetadata(false, OnNumericOnlyChanged));

    public static bool GetNumericOnly(DependencyObject obj) => (bool)obj.GetValue(NumericOnlyProperty);

    public static void SetNumericOnly(DependencyObject obj, bool value) => obj.SetValue(NumericOnlyProperty, value);

    private static void OnNumericOnlyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TextBox textBox)
        {
            if ((bool)e.NewValue)
            {
                textBox.PreviewTextInput += NumericTextBox_PreviewTextInput;
                textBox.PreviewKeyDown += NumericTextBox_PreviewKeyDown;
            }
            else
            {
                textBox.PreviewTextInput -= NumericTextBox_PreviewTextInput;
                textBox.PreviewKeyDown -= NumericTextBox_PreviewKeyDown;
            }
        }
    }

    private static void NumericTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        // Block any character that is not a digit, decimal separator, comma, or minus
        e.Handled = !_numericRegex.IsMatch(e.Text);
    }

    private static void NumericTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        // Allow: Backspace, Delete, Tab, Escape, Enter, Arrow keys, Home, End
        // Allow: Ctrl+A (select all), Ctrl+C, Ctrl+V, Ctrl+X (clipboard)
        if (e.Key is Key.Back or Key.Delete or Key.Tab or Key.Escape or Key.Enter
            or Key.Left or Key.Right or Key.Home or Key.End
            or Key.OemPeriod or Key.Decimal)
        {
            return;
        }

        // Allow Ctrl+key combos (copy, paste, cut, select all)
        if (e.KeyboardDevice.Modifiers == ModifierKeys.Control)
        {
            return;
        }

        // Allow Shift+Arrow for selection
        if (e.Key is Key.LeftShift or Key.RightShift)
        {
            return;
        }
    }
}
