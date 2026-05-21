namespace SalesSystem.Desktop.Tests.Commands;

using System.Windows.Input;
using FluentAssertions;
using SalesSystem.DesktopPWF.ViewModels;

/// <summary>
/// Tests for RelayCommand and AsyncRelayCommand
/// </summary>
public class RelayCommandTests
{
    #region RelayCommand Tests

    [Fact]
    public void RelayCommand_Execute_CallsAction()
    {
        // Arrange
        var executed = false;
        var command = new RelayCommand(() => executed = true);

        // Act
        command.Execute(null);

        // Assert
        executed.Should().BeTrue();
    }

    [Fact]
    public void RelayCommand_Execute_WithParameter_CallsActionWithParameter()
    {
        // Arrange
        object? capturedParam = null;
        var command = new RelayCommand(p => capturedParam = p);

        // Act
        command.Execute("test-param");

        // Assert
        capturedParam.Should().Be("test-param");
    }

    [Fact]
    public void RelayCommand_CanExecute_ReturnsTrue_WhenNoPredicate()
    {
        // Arrange
        var command = new RelayCommand(() => { });

        // Act & Assert
        command.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void RelayCommand_CanExecute_ReturnsFalse_WhenPredicateReturnsFalse()
    {
        // Arrange
        var command = new RelayCommand(() => { }, () => false);

        // Act & Assert
        command.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void RelayCommand_CanExecute_ReturnsTrue_WhenPredicateReturnsTrue()
    {
        // Arrange
        var command = new RelayCommand(() => { }, () => true);

        // Act & Assert
        command.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void RelayCommand_CanExecute_WithParameter_UsesPredicate()
    {
        // Arrange
        var command = new RelayCommand(_ => { }, p => p != null);

        // Act & Assert
        command.CanExecute(null).Should().BeFalse();
        command.CanExecute("test").Should().BeTrue();
    }

    [Fact(Skip = "Requires WPF message loop - CommandManager.InvalidateRequerySuggested() doesn't fire events in unit tests")]
    public void RelayCommand_RaiseCanExecuteChanged_TriggersCanExecuteChanged()
    {
        // Arrange
        var canExecuteChangedCalled = false;
        var command = new RelayCommand(() => { });
        command.CanExecuteChanged += (s, e) => canExecuteChangedCalled = true;

        // Act
        command.RaiseCanExecuteChanged();

        // Assert
        canExecuteChangedCalled.Should().BeTrue();
    }

    [Fact]
    public void RelayCommand_Constructor_Throws_WhenExecuteIsNull()
    {
        // Act & Assert - pass null to the Action<object?> overload directly
        Assert.Throws<ArgumentNullException>(() => new RelayCommand((Action<object?>)null!));
    }

    #endregion

    #region AsyncRelayCommand Tests

    [Fact]
    public async Task AsyncRelayCommand_Execute_CallsAsyncFunc()
    {
        // Arrange
        var executed = false;
        var tcs = new TaskCompletionSource();
        var command = new AsyncRelayCommand(async () => { 
            await tcs.Task; 
            executed = true; 
        });

        // Act
        command.Execute(null);
        
        // Wait a bit for async execution to start
        await Task.Delay(20);
        
        // Complete the task
        tcs.SetResult();
        
        // Wait for execution to complete
        await Task.Delay(20);

        // Assert
        executed.Should().BeTrue();
    }

    [Fact]
    public async Task AsyncRelayCommand_Execute_WithParameter_CallsAsyncFuncWithParameter()
    {
        // Arrange
        object? capturedParam = null;
        var command = new AsyncRelayCommand(async p => { await Task.Delay(10); capturedParam = p; });

        // Act
        command.Execute("test-param");
        await Task.Delay(50);

        // Assert
        capturedParam.Should().Be("test-param");
    }

    [Fact]
    public void AsyncRelayCommand_CanExecute_ReturnsTrue_WhenNotExecutingAndNoPredicate()
    {
        // Arrange
        var command = new AsyncRelayCommand(async () => { await Task.Delay(1); });

        // Act & Assert
        command.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void AsyncRelayCommand_CanExecute_ReturnsFalse_WhenExecuting()
    {
        // Arrange
        var tcs = new TaskCompletionSource();
        var command = new AsyncRelayCommand(async () => await tcs.Task);

        // Start execution
        command.Execute(null);

        // Act & Assert - while executing, CanExecute should be false
        command.CanExecute(null).Should().BeFalse();

        // Complete the task
        tcs.SetResult();
    }

    [Fact]
    public void AsyncRelayCommand_CanExecute_ReturnsFalse_WhenPredicateReturnsFalse()
    {
        // Arrange
        var command = new AsyncRelayCommand(async () => { await Task.Delay(1); }, () => false);

        // Act & Assert
        command.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void AsyncRelayCommand_CanExecute_ReturnsTrue_WhenPredicateReturnsTrue()
    {
        // Arrange
        var command = new AsyncRelayCommand(async _ => { await Task.Delay(1); }, _ => true);

        // Act & Assert
        command.CanExecute(null).Should().BeTrue();
    }

    [Fact(Skip = "Requires WPF message loop - CommandManager.InvalidateRequerySuggested() doesn't fire in unit tests")]
    public async Task AsyncRelayCommand_RaisesCanExecuteChanged_BeforeAndAfterExecution()
    {
        // Arrange
        var canExecuteChangedCount = 0;
        var tcs = new TaskCompletionSource();
        var command = new AsyncRelayCommand(async () => await tcs.Task);
        command.CanExecuteChanged += (s, e) => canExecuteChangedCount++;

        // Act - start execution
        command.Execute(null);

        // Assert - CanExecuteChanged should have been raised (before and after)
        canExecuteChangedCount.Should().BeGreaterThan(0);

        // Complete execution
        tcs.SetResult();
        await Task.Delay(50);

        // CanExecuteChanged should be raised again after execution completes
        canExecuteChangedCount.Should().BeGreaterThan(1);
    }

    [Fact]
    public void AsyncRelayCommand_Constructor_Throws_WhenExecuteIsNull()
    {
        // Act & Assert - pass null to the Func<object?, Task> overload directly
        Assert.Throws<ArgumentNullException>(() => new AsyncRelayCommand((Func<object?, Task>)null!));
    }

    [Fact]
    public void AsyncRelayCommand_Execute_DoesNotExecute_IfAlreadyExecuting()
    {
        // Arrange
        var executionCount = 0;
        var tcs = new TaskCompletionSource();
        var command = new AsyncRelayCommand(async () =>
        {
            executionCount++;
            await tcs.Task;
        });

        // Act - start first execution
        command.Execute(null);
        // Try to execute again while still running
        command.Execute(null);

        // Assert - only one execution should have started
        executionCount.Should().Be(1);

        // Complete the task
        tcs.SetResult();
    }

    [Fact(Skip = "Requires WPF message loop - CommandManager.InvalidateRequerySuggested() doesn't fire in unit tests")]
    public void AsyncRelayCommand_RaiseCanExecuteChanged_TriggersCanExecuteChanged()
    {
        // Arrange
        var canExecuteChangedCalled = false;
        var command = new AsyncRelayCommand(async () => { await Task.Delay(1); });
        command.CanExecuteChanged += (s, e) => canExecuteChangedCalled = true;

        // Act
        command.RaiseCanExecuteChanged();

        // Assert
        canExecuteChangedCalled.Should().BeTrue();
    }

    #endregion
}