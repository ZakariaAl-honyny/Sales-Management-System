using System.Windows;
using System.Windows.Input;

namespace SalesSystem.DesktopPWF.Helpers;

public static class InputHelper
{
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
}
