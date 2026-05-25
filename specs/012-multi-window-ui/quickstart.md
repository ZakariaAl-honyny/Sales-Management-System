# Quickstart: Multi-Window & UI Polish (v4.5)

## Implementation Order

1. **Window Host**: Create `ScreenWindow.xaml`. Add the `Closed` event handler in code-behind to dispose of the ViewModel.
2. **Window Service**: Implement `ScreenWindowService` and `IScreenWindowService`. Add the `WeakReference` tracking and modulo-10 cascading math. Register in DI.
3. **Dialog Fix**: Update `DialogService.cs` to safely resolve the active window and prevent self-ownership crashes.
4. **MessageBox Purge**: Search the Desktop codebase for `MessageBox.Show`. Replace every instance with `IDialogService.ShowErrorAsync` or `ShowWarningAsync`.
5. **Memory Leak Audit**: Review all ViewModels that inject `IEventBus`. Ensure they hold the subscription `IDisposable` and call `Dispose()` on it in their own `Dispose()` method.
6. **List Sorting**: Update list ViewModels or backend queries to sort descending.
7. **ToolTips**: Do a sweeping pass across all XAML files in the `Views` folder, adding Arabic `ToolTip` strings to interactive controls.

## Key Invariants to Verify

- **Garbage Collection**: Open 5 editor windows, then close them. Force GC. Use Visual Studio Diagnostic Tools to verify the ViewModel instances have dropped to 0.
- **Dialogs**: Open a non-modal editor, trigger an error, ensure the dialog opens centered OVER the non-modal editor, not the main window behind it.
- **Cascading**: Open 12 windows rapidly. They should cascade down and right, then reset back to the top-left on the 11th window.
