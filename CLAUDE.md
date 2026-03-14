# CLAUDE.md - Health Tracker

This file describes the conventions, architecture, and tooling for this project.
Read it fully before writing or modifying any code.

This application is called Health Tracker and it's a Windows Desktop application to track various health metrics for a user. The requirements for this application can be found here: @requirements.md

---

## Project Overview

- **Framework:** .NET 10
- **Language:** C# 14
- **UI:** Avalonia UI (Windows desktop, self-contained `.exe`) (Avalonia UI documentation can be found here: https://docs.avaloniaui.net/api/avalonia)
- **Pattern:** MVVM + Clean Architecture
- **MVVM Library:** CommunityToolkit.Mvvm
- **Graphing library:** - ScottPlot (Documentation can be found here: https://scottplot.net/api/5/)

---

## Solution Structure

The solution file (`.slnx`) lives in the **repo root** and is named after the application.
All projects live under the `src/` folder.

```
MyApp.slnx
src/
  MyApp.UI/           # Avalonia views, view models, app entry point
  MyApp.Shared/       # Interfaces, exceptions, DTOs, enums
  MyApp.Services/     # Reusable business logic / application services
  MyApp.Infrastructure/ # External concerns: file I/O, HTTP, DB access
  MyApp.Tests/        # xUnit test project
```

Adjust project names to match the actual application name. Follow this naming pattern:
`<AppName>.<Purpose>` — e.g. `MyApp.Services`, `MyApp.Shared`.

---

## Commands

```bash
dotnet build    # Build the solution
dotnet run      # Run the application
dotnet test     # Run all tests
```

Publish as a self-contained Windows executable:

```bash
dotnet publish -c Release -r win-x64 --self-contained true
```

---

## Architecture

### Clean Architecture layers (inner → outer)

1. **Shared** — interfaces, DTOs, exceptions, enums. No dependencies on other layers.
2. **Services** — application/domain logic. Depends only on Shared.
3. **Infrastructure** — implements interfaces from Shared (DB, file I/O, HTTP, etc.). Depends on Shared.
4. **UI** — Avalonia views and view models. Depends on Shared and Services.

Dependencies always point **inward**. Infrastructure and UI are never referenced by Services or Shared.

### MVVM

- Use **CommunityToolkit.Mvvm** throughout.
- View models inherit from `ObservableObject` or `ObservableRecipient`.
- Use `[ObservableProperty]` for bindable properties and `[RelayCommand]` for commands.
- Views are code-behind-light: no business logic in `.axaml.cs` files.
- Never put logic that belongs in a service directly inside a view model.

---

## Dependency Injection

Use **Microsoft.Extensions.DependencyInjection**.

- Register all services, view models, and infrastructure implementations in a central composition root (typically in `MyApp.UI` — e.g. `AppBootstrapper.cs` or inside `App.axaml.cs`).
- View models are registered and resolved via the DI container — do not instantiate them with `new` in production code.
- Use constructor injection exclusively. Avoid service-locator patterns.

---

## Coding Conventions

These are **strict** — follow them in all new and modified code.

### General

- **File-scoped namespaces** — always. Never block-scoped namespaces.
- **`var`** — prefer it whenever the type is clear from context. Use explicit types only when the inferred type would be genuinely ambiguous.
- **Pattern matching** — use `is`, `switch` expressions, and positional/property patterns wherever they improve clarity.
- **Expression-bodied members** — use where they improve readability (single-expression methods, properties).
- **Records** - user records for DTOs, or any other object that has only data, and no logic.
- **Naming** - PascalCase for public members and _camelCase for private fields.
- **Code comments** - add comments that explain *why* something is done the way it is. Never just explain *what* is being done.

### Async / Await

- Do **not** append `Async` to method names. Name methods by what they do, not how they do it.

  ```csharp
  // ✅
  public async Task<User> GetUser(int id) { ... }

  // ❌
  public async Task<User> GetUserAsync(int id) { ... }
  ```

- Always `await` async calls — never `.Result` or `.Wait()`.
- Use `CancellationToken` parameters on any async method that may run long or be cancelled. Pass tokens through the call chain rather than ignoring them.
- For fire-and-forget scenarios in view models (e.g. loading on navigation), use `[RelayCommand]` which handles `Task`-returning commands safely, or wrap carefully with proper exception handling.

### Nullable Reference Types

- NRT is **enabled**. All code must be NRT-clean — no `#nullable disable` suppressions without a comment explaining why.
- Never use the null-forgiving operator (`!`) unless you can justify it with a comment.
- Prefer `is not null` / `is null` over `!= null` / `== null`.
- Use `required` properties and primary constructors to avoid nullable fields where possible.

### Thread Safety / UI Dispatcher

- Avalonia UI controls must only be updated on the UI thread.
- When updating observable properties from a background thread, use:

  ```csharp
  Dispatcher.UIThread.Post(() => { ... });
  // or
  await Dispatcher.UIThread.InvokeAsync(() => { ... });
  ```

- Never block the UI thread with synchronous I/O or long-running work.
- Prefer `Task`-based async pipelines that marshal back to the UI thread only at the final step.

---

## Logging

Use **Serilog** with the **file sink**.

- Configure Serilog at application startup (in `App.axaml.cs` or the bootstrapper).
- Use the static `Log` accessor or inject `ILogger` via DI — be consistent within the project.
- Use structured logging — pass values as properties, not string-interpolated:

  ```csharp
  // ✅
  Log.Information("Loaded {Count} records in {Elapsed}ms", count, elapsed);

  // ❌
  Log.Information($"Loaded {count} records in {elapsed}ms");
  ```

- Log exceptions with `Log.Error(ex, "message")` — always include the exception object.

---

## Database

- **Never use Entity Framework** under any circumstances.
- If database access is required, use **Dapper**.
- All database access lives in the `Infrastructure` project behind an interface defined in `Shared`.

---

## Testing

- **Test framework:** xUnit
- **Mocking:** Moq
- **Assertions:** AwesomeAssertions

### Conventions

- One test project (`MyApp.Tests`), mirroring the source project/namespace structure.
- Name tests: `MethodName_Scenario_ExpectedResult`.
- **DO NOT** emit Arrange / Act / Assert comments.
- Mock only external dependencies and infrastructure — test business logic directly.
- Do not test view models that only delegate to services without logic; test the service.

```csharp
// Example structure
public class UserServiceTests
{
    [Fact]
    public async Task GetUser_ValidId_ReturnsUser()
    {
        var repo = new Mock<IUserRepository>();
        repo.Setup(r => r.GetById(1)).ReturnsAsync(new User { Id = 1 });
        var sut = new UserService(repo.Object);

        var result = await sut.GetUser(1);

        result.Id.Should().Be(1);
    }
}
```

## Git Workflow
- Branch naming: `feature/`, `bugfix/`, `hotfix/`
- Commit format: `type: description` (feat, fix, refactor, test, docs)
- Always create a branch before changes
- Run tests before committing.

---

## Editor Config

If an `.editorconfig` file exists in the repository, **honour its settings** — they take precedence over any general style preferences. Do not suggest changes that conflict with `.editorconfig` rules.

---

## Deployment

- Target: **Windows x64**
- Mode: **self-contained** (no .NET runtime required on the target machine)
- Single-file publish is preferred where practical.
- Do not add platform-specific code outside of the `UI` or `Infrastructure` projects.

## General Workflow
- **Learn from corrections** — After any correction, capture the pattern in memory so the same mistake never recurs.
- **Always get the latest Nuget package version** - When adding a Nuget package to a project, ALWAYS use the last available version compatible with the project platform.

## MCP Tools

> **Setup:** Install once globally with `dotnet tool install -g CWM.RoslynNavigator` and register with `claude mcp add --scope user cwm-roslyn-navigator -- cwm-roslyn-navigator --solution ${workspaceFolder}`. After that, these tools are available in every .NET project.

Prefer the registered MCP server `cwm-roslyn-navigator` for all source navigation (finding symbols, 
reading files, searching). Avoid `cat`, `grep`, `find` for code exploration.  

This MCP server has the following tools:
| Tool | Description |
|------|-------------|
| `find_symbol` | Find where a type, method, or property is defined |
| `find_references` | All usages of a symbol across the solution |
| `find_implementations` | Types that implement an interface or derive from a base class |
| `find_callers` | All methods that call a specific method |
| `find_overrides` | Overrides of a virtual or abstract method |
| `find_dead_code` | Unused types, methods, and properties |
| `get_type_hierarchy` | Inheritance chain, interfaces, and derived types |
| `get_public_api` | Public members of a type without reading the full file |
| `get_symbol_detail` | Full signature, parameters, return type, and XML docs |
| `get_project_graph` | Solution project dependency tree |
| `get_dependency_graph` | Call dependency graph for a method |
| `get_diagnostics` | Compiler and analyzer warnings/errors |
| `get_test_coverage_map` | Heuristic test coverage by naming convention |
| `detect_antipatterns` | .NET anti-patterns (async void, sync-over-async, etc.) |
| `detect_circular_dependencies` | Circular dependency detection at project or type level |