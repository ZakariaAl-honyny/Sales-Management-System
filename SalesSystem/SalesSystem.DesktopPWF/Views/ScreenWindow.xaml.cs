using System.ComponentModel;
using System.Windows;
using SalesSystem.DesktopPWF.Services.App;

namespace SalesSystem.DesktopPWF.Views;

public partial class ScreenWindow : Window
{
    public ScreenWindow()
    {
        InitializeComponent();
    }

    public ScreenWindowOptions? ScreenOptions
    {
        get => (ScreenWindowOptions?)GetValue(ScreenOptionsProperty);
        set => SetValue(ScreenOptionsProperty, value);
    }

    public static readonly DependencyProperty ScreenOptionsProperty =
        DependencyProperty.Register(nameof(ScreenOptions), typeof(ScreenWindowOptions), typeof(ScreenWindow),
            new PropertyMetadata(null, OnScreenOptionsChanged));

    private static void OnScreenOptionsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ScreenWindow window && e.NewValue is ScreenWindowOptions options)
            window.ApplyOptions(options);
    }

    private void ApplyOptions(ScreenWindowOptions options)
    {
        if (!string.IsNullOrEmpty(options.Title))
            Title = options.Title;

        if (options.Left.HasValue)
            Left = options.Left.Value;
        if (options.Top.HasValue)
            Top = options.Top.Value;

        Width = options.Width;
        Height = options.Height;

        ResizeMode = options.CanResize ? ResizeMode.CanResize : ResizeMode.NoResize;
        WindowStyle = options.Style;
        WindowStartupLocation = options.StartupLocation;
    }

    public void SetContent(FrameworkElement element, object viewModel)
    {
        ScreenContent.Content = element;
        DataContext = viewModel;
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);
        // Lifecycle/cleanup handled by ScreenWindowService
    }
}
