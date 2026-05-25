# Research: Multi-Window & UI Polish (v4.5)

**Feature**: `012-multi-window-ui`
**Date**: 2026-05-25
**Status**: Complete

---

## Decision Log

### D-001: Non-Modal Window Host

**Decision**: Create `ScreenWindow.xaml`, a generic WPF Window. It contains a `ContentControl` bound to a ViewModel. Implement `IScreenWindowService.OpenNonModal(ViewModelBase vm)`.

**Rationale**: Reusing a single `ScreenWindow` class for all editors avoids creating dozens of individual XAML Window files. The `DataTemplate` engine in `App.xaml` will automatically resolve the `ViewModel` to its corresponding `UserControl` View.

---

### D-002: Window Tracking & Memory Leaks

**Decision**: `ScreenWindowService` will maintain a `List<WeakReference<Window>> _openWindows`.

**Rationale**: If a hard reference (`List<Window>`) is used, closed windows might not be garbage collected. `WeakReference` allows the Garbage Collector to reclaim the window once WPF closes it, while still letting us iterate over living windows to calculate cascade offsets.

---

### D-003: Cascading Window Math

**Decision**: When opening a new window, calculate offset based on the number of currently alive windows.
`var count = _openWindows.Count(w => w.TryGetTarget(out _));`
`Left = MainWindow.Left + (30 * (count % 10));`
`Top = MainWindow.Top + (30 * (count % 10));`

**Rationale**: Simple math that ensures windows don't perfectly overlap. The modulo 10 prevents windows from cascading entirely off the screen if many are opened.

---

### D-004: EventBus Memory Leaks

**Decision**: Many ViewModels subscribe to the `EventBus`. If a non-modal window closes, the `EventBus` (which is a Singleton) will hold a reference to the ViewModel's handler, preventing GC. Solution: `ScreenWindow` must handle the `Closed` event, cast the DataContext to `IDisposable`, and call `Dispose()`. All ViewModels must ensure their `EventBus` subscriptions are saved as `IDisposable` and disposed in `Dispose()`.

**Rationale**: This is the only way to prevent severe memory leaks in long-running desktop applications using a global Pub/Sub model.

---

### D-005: Dialog Ownership Fix

**Decision**: In `DialogService`, before showing a dialog (`window.ShowDialog()`), find the active window:
`var owner = Application.Current.Windows.OfType<Window>().SingleOrDefault(x => x.IsActive);`
If `owner` is the dialog itself (which causes the `InvalidOperationException`), fallback to `Application.Current.MainWindow`.

**Rationale**: This definitively fixes the "Cannot set Owner property to a Window that has not been shown previously" and self-ownership crashes that occur when dialogs trigger during window transition states.
