using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace SalesSystem.DesktopPWF.Helpers;

/// <summary>
/// Attached behavior that auto-focuses the first invalid input control
/// when validation errors are present. Attach to any Panel or Window.
/// Usage: helpers:ValidationFocusBehavior.FocusOnFirstError="True"
/// </summary>
public static class ValidationFocusBehavior
{
    public static readonly DependencyProperty FocusOnFirstErrorProperty =
        DependencyProperty.RegisterAttached(
            "FocusOnFirstError",
            typeof(bool),
            typeof(ValidationFocusBehavior),
            new PropertyMetadata(false, OnFocusOnFirstErrorChanged));

    public static bool GetFocusOnFirstError(DependencyObject obj)
        => (bool)obj.GetValue(FocusOnFirstErrorProperty);

    public static void SetFocusOnFirstError(DependencyObject obj, bool value)
        => obj.SetValue(FocusOnFirstErrorProperty, value);

    private static void OnFocusOnFirstErrorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement element)
            return;

        if ((bool)e.NewValue)
        {
            element.Loaded += OnElementLoaded;
        }
        else
        {
            element.Loaded -= OnElementLoaded;
        }
    }

    private static void OnElementLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement element)
            return;

        // Find the first invalid element and focus it
        var firstInvalid = FindFirstInvalid(element);
        firstInvalid?.Focus();
    }

    /// <summary>
    /// Finds the first control in the visual tree that has validation errors.
    /// Searches depth-first for TextBox, ComboBox, PasswordBox, and other input controls.
    /// </summary>
    public static FrameworkElement? FindFirstInvalid(DependencyObject parent)
    {
        if (parent == null)
            return null;

        // Check this element first
        if (parent is FrameworkElement fe && HasValidationError(fe))
            return fe;

        // Search children
        int childCount = VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < childCount; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            var result = FindFirstInvalid(child);
            if (result != null)
                return result;
        }

        return null;
    }

    /// <summary>
    /// Finds the first empty required field based on a predicate.
    /// Used when INotifyDataErrorInfo errors are not set but fields are empty.
    /// </summary>
    public static FrameworkElement? FindFirstEmptyRequired(DependencyObject parent, Func<FrameworkElement, bool>? isRequired = null)
    {
        if (parent == null)
            return null;

        // Check this element
        if (parent is TextBox tb && string.IsNullOrEmpty(tb.Text))
        {
            if (isRequired == null || isRequired(tb))
                return tb;
        }

        if (parent is ComboBox cb && (cb.SelectedItem == null || cb.SelectedIndex < 0))
        {
            if (isRequired == null || isRequired(cb))
                return cb;
        }

        if (parent is PasswordBox pb && string.IsNullOrEmpty(pb.Password))
        {
            if (isRequired == null || isRequired(pb))
                return pb;
        }

        // Search children
        int childCount = VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < childCount; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            var result = FindFirstEmptyRequired(child, isRequired);
            if (result != null)
                return result;
        }

        return null;
    }

    private static bool HasValidationError(FrameworkElement element)
    {
        // Check INotifyDataErrorInfo via Validation.GetErrors
        if (System.Windows.Controls.Validation.GetHasError(element))
            return true;

        // Also check if it's a TextBox with HasError bound property
        if (element is TextBox textBox)
        {
            var expr = textBox.GetBindingExpression(TextBox.TextProperty);
            if (expr != null && expr.HasError)
                return true;
        }

        if (element is ComboBox comboBox)
        {
            var expr = comboBox.GetBindingExpression(ComboBox.SelectedItemProperty);
            if (expr != null && expr.HasError)
                return true;
        }

        return false;
    }
}
