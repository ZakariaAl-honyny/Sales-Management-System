using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SalesSystem.DesktopPWF.Helpers;

/// <summary>
/// Helper to handle focus navigation via keyboard (e.g., Enter moves to next field).
/// </summary>
public static class FocusHelper
{
    public static readonly DependencyProperty AdvanceOnEnterProperty =
        DependencyProperty.RegisterAttached("AdvanceOnEnter", 
            typeof(bool), 
            typeof(FocusHelper), 
            new PropertyMetadata(false, OnAdvanceOnEnterChanged));

    public static bool GetAdvanceOnEnter(DependencyObject obj) => (bool)obj.GetValue(AdvanceOnEnterProperty);
    public static void SetAdvanceOnEnter(DependencyObject obj, bool value) => obj.SetValue(AdvanceOnEnterProperty, value);

    private static void OnAdvanceOnEnterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is UIElement element)
        {
            if ((bool)e.NewValue)
            {
                element.KeyDown += Element_KeyDown;
            }
            else
            {
                element.KeyDown -= Element_KeyDown;
            }
        }
    }

    private static void Element_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            var element = sender as UIElement;
            if (element == null) return;

            // Don't advance if it's a multi-line TextBox and Shift is not pressed
            if (element is TextBox textBox && textBox.AcceptsReturn && !Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                return;
            }

            // Move focus to next element
            element.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
            e.Handled = true;
        }
    }
}


