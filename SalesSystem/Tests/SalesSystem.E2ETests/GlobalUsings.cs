// Global using directives for E2E tests

// Testing frameworks
global using Xunit;

// FluentAssertions - for more readable assertions
global using FluentAssertions;
global using FluentAssertions.Execution;

// FlaUI Core
global using FlaUI.Core;
global using FlaUI.Core.AutomationElements;
global using FlaUI.Core.AutomationElements.Infrastructure;
global using FlaUI.Core.Conditions;
global using FlaUI.Core.Definitions;
global using FlaUI.Core.Input;
global using FlaUI.Core.WindowsAPI;

// FlaUI UIA2 specific (better WPF visual tree traversal)
global using FlaUI.UIA2;
global using FlaUI.UIA2.Patterns;
global using UIA = System.Windows.Automation;

// Test categories
global using TestCategory = SalesSystem.E2ETests.TestCategories;
