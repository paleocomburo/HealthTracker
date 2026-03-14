# Health Tracker — Implementation Plan

## Context

Building a greenfield Windows desktop application (Health Tracker) from scratch. The project currently contains only `requirements.md` and `CLAUDE.md`. This plan produces a fully functional .NET 10 / Avalonia UI / MVVM app that allows a single user to record, view, and analyse weight, blood pressure, and blood sugar measurements, stored as local JSON files.

**Charting library chosen: OxyPlot.Avalonia** — mature, stable Avalonia backend; first-class `LineAnnotation` support for threshold/target lines; clean MVVM binding via `PlotModel`; precise tooltip control needed for averaged readings.

---

## Solution Structure

```
D:\projects\HealthTracker\
├── HealthTracker.slnx
├── PLAN.md                          ← copy of this plan for project reference
└── src/
    ├── HealthTracker.UI/            # Avalonia views, view models, DI root, entry point
    ├── HealthTracker.Shared/        # DTOs (records), interfaces, enums, exceptions
    ├── HealthTracker.Services/      # Business logic, validation, CSV export
    ├── HealthTracker.Infrastructure/# JSON file I/O, settings persistence
    └── HealthTracker.Tests/         # xUnit + Moq + AwesomeAssertions
```

**Project references (inner → outer only):**
- Services → Shared
- Infrastructure → Shared
- UI → Shared, Services
- Tests → all four projects

---

## Phase 1 — Solution & Project Scaffolding

### Step 1.1 — Create solution and five projects
```bash
dotnet new sln -n HealthTracker --format slnx
dotnet new classlib   -n HealthTracker.Shared         -o src/HealthTracker.Shared
dotnet new classlib   -n HealthTracker.Services       -o src/HealthTracker.Services
dotnet new classlib   -n HealthTracker.Infrastructure -o src/HealthTracker.Infrastructure
dotnet new avalonia.mvvm -n HealthTracker.UI          -o src/HealthTracker.UI
dotnet new xunit      -n HealthTracker.Tests          -o src/HealthTracker.Tests
# Add all projects to solution, add project references
```

**NuGet packages:**
- `HealthTracker.UI`: `Avalonia`, `Avalonia.Desktop`, `Avalonia.Themes.Fluent`, `CommunityToolkit.Mvvm`, `Microsoft.Extensions.DependencyInjection`, `Serilog`, `Serilog.Sinks.File`, `OxyPlot.Avalonia`
- `HealthTracker.Tests`: `xunit`, `xunit.runner.visualstudio`, `Moq`, `AwesomeAssertions`, `Microsoft.NET.Test.Sdk`, `coverlet.collector`

**Verification:** `dotnet build` succeeds; `dotnet run --project src/HealthTracker.UI` opens a blank Avalonia window.

### Step 1.2 — Configure Serilog at startup
In `App.axaml.cs` `OnFrameworkInitializationCompleted`, configure `Log.Logger` with `WriteTo.File` pointing to `%USERPROFILE%\Documents\HealthTracker\logs\healthtracker-.log` with `RollingInterval.Day`.

**Verification:** A `logs/` subfolder and log file appear on launch.

### Step 1.3 — Add DI composition root
Create `src/HealthTracker.UI/AppBootstrapper.cs` — registers nothing yet, but the container is built and stored. Wire into `App.axaml.cs`.

**Verification:** `dotnet build` succeeds; blank window still opens.

---

## Phase 2 — Shared Layer

All files in `src/HealthTracker.Shared/`.

### Step 2.1 — Enums
- `Enums/TimeOfDay.cs`: `Morning`, `Afternoon`
- `Enums/DateRangeMode.cs`: `Week`, `Month`, `Year`, `Custom`
- `Enums/ThresholdLevel.cs`: `None`, `Warning`, `Danger`, `BelowLower`

### Step 2.2 — DTO records
```
Dtos/WeightEntry.cs           record(Guid Id, DateOnly Date, decimal WeightKg)
Dtos/BloodPressureReading.cs  record(int SystolicMmhg, int DiastolicMmhg)
Dtos/BloodPressureEntry.cs    record(Guid Id, DateOnly Date, TimeOfDay TimeOfDay, IReadOnlyList<BloodPressureReading> Readings)
Dtos/BloodSugarEntry.cs       record(Guid Id, DateOnly Date, IReadOnlyList<decimal> Readings, string Context)
Dtos/ThresholdSettings.cs     record(int? BpSystolicUpperMmhg, int? BpDiastolicUpperMmhg, decimal? BloodSugarWarningMmolL, decimal? BloodSugarDangerMmolL, decimal? BloodSugarLowerMmolL)
Dtos/AppSettings.cs           record(decimal? TargetWeightKg, ThresholdSettings Thresholds)
```

### Step 2.3 — Repository interfaces
```
Interfaces/IWeightRepository.cs
Interfaces/IBloodPressureRepository.cs
Interfaces/IBloodSugarRepository.cs
  → GetEntries(DateOnly from, DateOnly to, CancellationToken ct) : Task<IReadOnlyList<T>>
  → Add(T entry, CancellationToken ct) : Task<T>
  → Update(T entry, CancellationToken ct) : Task
  → Delete(Guid id, DateOnly entryDate, CancellationToken ct) : Task

Interfaces/ISettingsRepository.cs  → Load / Save
Interfaces/IAppPaths.cs            → string DataDirectory { get; }
Interfaces/IDialogService.cs       → ShowConfirmation / ShowSaveFileDialog / ShowEntryDialog
```

### Step 2.4 — Exceptions
- `Exceptions/DataFileCorruptException.cs` — wraps the inner exception, carries `FilePath`
- `Exceptions/ValidationException.cs` — carries `string Message`

**Verification:** `dotnet build` succeeds.

---

## Phase 3 — Infrastructure Layer

All files in `src/HealthTracker.Infrastructure/`.

### Step 3.1 — AppPaths
`AppPaths.cs` implementing `IAppPaths`. Resolves `%USERPROFILE%\Documents\HealthTracker\`, calls `Directory.CreateDirectory` to ensure it exists. Single place the path is assembled.

### Step 3.2 — JSON serializer configuration
`Json/JsonConfig.cs` — static `JsonSerializerOptions` with `PropertyNamingPolicy = SnakeCaseLower`, `WriteIndented = true`, and a `DateOnly` converter if required by .NET 10.

### Step 3.3 — YearPartitionedStore (core primitive)
`Json/YearPartitionedStore.cs` — generic helper accepting a filename prefix and `IAppPaths`:
- `ReadYear(int year)` → `List<T>` (empty list if file absent; `DataFileCorruptException` if malformed)
- `WriteYear(int year, List<T> entries)` — **atomic write**: serialize to `{prefix}_{year}.tmp`, then `File.Move` with overwrite to `{prefix}_{year}.json`

This is the **only** class that does file I/O for data files.

**Verification:** Unit test: write list → read back → assert equal; corrupt file → assert `DataFileCorruptException`.

### Step 3.4 — WeightRepository
`Repositories/WeightRepository.cs` implementing `IWeightRepository`. Uses private `WeightEntryJson` record (JSON shape). `GetEntries` spans multiple years by calling `ReadYear` for each year in range. Add/Update/Delete read the target year, mutate the list, write atomically.

### Step 3.5 — BloodPressureRepository and BloodSugarRepository
Same structure. Private JSON model types:
```csharp
record BloodPressureReadingJson(int SystolicMmhg, int DiastolicMmhg);
record BloodPressureEntryJson(string Id, string Date, string TimeOfDay, List<BloodPressureReadingJson> Readings);
record BloodSugarEntryJson(string Id, string Date, List<decimal> Readings, string Context);
```

### Step 3.6 — SettingsRepository
`Repositories/SettingsRepository.cs`. Single `settings.json` file (no year partitioning). Returns a default `AppSettings` when file absent.

### Step 3.7 — DI registration
`AddInfrastructure` extension method in Infrastructure. Registers `IAppPaths`, three `YearPartitionedStore<T>` instances, and all four repository implementations as singletons. Wired into `AppBootstrapper`.

**Verification:** `dotnet run` still opens window; no DI errors.

---

## Phase 4 — Services Layer

All files in `src/HealthTracker.Services/`.

### Step 4.1 — WeightService
Constructor: `IWeightRepository`. Methods:
- `GetEntries(from, to, ct)` — delegates
- `AddEntry(date, weightKg, ct)` — validates (> 0, < 500 kg; not future date), creates `WeightEntry` with new `Guid`, calls `Add`
- `UpdateEntry(entry, ct)` — re-validates, calls `Update`
- `DeleteEntry(id, entryDate, ct)` — delegates
- `GetLast10DateRange(ct)` — fetches all, sorts desc, takes 10, returns `(DateOnly from, DateOnly to)`

### Step 4.2 — BloodPressureService and BloodSugarService
Same shape. Validation:
- BP: systolic 60–250, diastolic 40–150, 1–5 readings per entry
- BS: 0.5–30.0 mmol/L per reading, 1–3 readings, context always `"fasting"`

### Step 4.3 — SettingsService
Constructor: `ISettingsRepository`. Load/Save with optional validation (positive threshold values where present).

### Step 4.4 — ThresholdEvaluator
`ThresholdEvaluator.cs` — **pure static class**, no dependencies:
- `EvaluateBloodPressure(int systolic, int diastolic, ThresholdSettings)` → `ThresholdLevel`
- `EvaluateBloodSugar(decimal avgReading, ThresholdSettings)` → `ThresholdLevel`

Returns the highest breach level. Trivially unit-testable.

### Step 4.5 — CsvExportService
Constructor: all three repositories. Methods per metric accept `(DateOnly from, DateOnly to, string filePath, CancellationToken ct)`. Writes UTF-8 CSV with individual readings and average rows per requirements. Uses atomic write (`.tmp` → rename).

### Step 4.6 — DI registration
`AddServices` extension method. Registers all five services. Wired into `AppBootstrapper`.

**Verification:** `dotnet build` succeeds.

---

## Phase 5 — UI Shell

All files in `src/HealthTracker.UI/`.

### Step 5.1 — MainWindow shell
`MainWindow.axaml` — root `Grid` with two columns: fixed sidebar (220 px) and `*` content area. Sidebar is a vertical panel with navigation buttons (Dashboard, Weight, Blood Pressure, Blood Sugar, Settings). Content area is a `ContentControl` bound to `MainWindowViewModel.CurrentView`.

`MainWindowViewModel.cs` — `[ObservableProperty] private object? _currentView;`. Individual `[RelayCommand]` per navigation item. Resolves all feature view models from DI on construction.

### Step 5.2 — ViewLocator
`ViewLocator.cs` implementing Avalonia's `IDataTemplate`. Naming convention: `FooViewModel` → `FooView` (same namespace tree). Registered in `App.axaml` `DataTemplates`.

### Step 5.3 — Five stub view/view model pairs
Create stub `UserControl` + `ObservableObject` pairs for: Dashboard, Weight, BloodPressure, BloodSugar, Settings. Each stub shows a `TextBlock` with the view name. All view models registered in `AppBootstrapper`. Navigate to Dashboard on startup.

**Verification:** All five sidebar buttons switch content area to correct placeholder. No crashes.

### Step 5.4 — DateRangeFilterViewModel (shared component)
`ViewModels/Shared/DateRangeFilterViewModel.cs`:
- `[ObservableProperty] DateRangeMode SelectedMode`
- `[ObservableProperty] DateOnly RangeStart`, `RangeEnd`
- `[RelayCommand] StepForward()`, `StepBackward()`
- Event `RangeChanged` for parent view models to subscribe

`Views/Shared/DateRangeFilterView.axaml` — segmented mode selector, prev/next arrows, date label.

**Verification:** `dotnet build` succeeds.

### Step 5.5 — DialogService
`Services/DialogService.cs` implementing `IDialogService`. Methods call Avalonia's `StorageProvider.SaveFilePickerAsync` and `Window.ShowDialog`. Registered as singleton.

---

## Phase 6 — Settings View

### Step 6.1 — SettingsViewModel (full)
Replace stub. Inject `SettingsService`. `[RelayCommand] LoadSettings(ct)` called on view activation. Observable `string` properties for each settings field (empty string = null on save). `[RelayCommand] Save(ct)` validates, converts, calls `SettingsService.Save`, sets `StatusMessage`.

### Step 6.2 — SettingsView (full)
Replace stub. Sections for Weight, Blood Pressure, Blood Sugar with labelled `TextBox` fields per threshold. "Save Settings" button. `StatusMessage` `TextBlock`.

**Verification:** Enter thresholds, save, restart app, verify values restored in `settings.json`.

---

## Phase 7 — Weight Feature (Full)

### Step 7.1 — WeightViewModel: loading
Replace stub. Inject `WeightService`, `SettingsService`. `DateRangeFilterViewModel Filter` as property. On construction, call `GetLast10DateRange()` to set initial filter range. Subscribe to `Filter.RangeChanged` → reload. `[ObservableProperty] ObservableCollection<WeightEntry> Entries`.

### Step 7.2 — WeightView: tab layout
Replace stub with `DockPanel`: date filter view docked top, header row with "Add Entry" and "Export" buttons, `TabControl` with "Chart" and "Table" tabs.

**Verification:** Navigate to Weight, see filter and tabs. Empty state is graceful.

### Step 7.3 — Weight Table View
`Views/Weight/WeightTableView.axaml` with `DataGrid`: Date, Weight (kg), Edit/Delete icon buttons. `WeightViewModel` adds `[RelayCommand] DeleteEntry(WeightEntry)` (confirmation dialog first) and `[RelayCommand] EditEntry(WeightEntry)`.

### Step 7.4 — Weight Add/Edit Dialog
`Views/Weight/WeightEntryDialog.axaml` — `DatePicker` + `NumericUpDown` for kg + inline validation + Save (disabled until valid) / Cancel.

`ViewModels/Weight/WeightEntryDialogViewModel.cs` — validates on each input change, emits result via `TaskCompletionSource`.

**Verification:** Add entry → appears in table. Edit → changes persist. Delete → removed after confirmation.

### Step 7.5 — Weight Chart View
`Views/Weight/WeightChartView.axaml` with `PlotView` bound to `WeightViewModel.PlotModel`. View model builds `LineSeries` for weight data and a dashed `LineAnnotation` for target weight (shown only if configured). Rebuilt on each data load.

**Verification:** Chart tab shows line chart; target weight line appears when set.

### Step 7.6 — Weight CSV Export
Export dialog with start/end date pickers. On confirm: `SaveFileDialog` via `IDialogService`, then `CsvExportService.ExportWeight`.

**Verification:** CSV file produced with correct format.

---

## Phase 8 — Blood Pressure Feature

### Step 8.1 — BloodPressureViewModel + display model
Display model: `record BloodPressureEntryDisplay(BloodPressureEntry Entry, double AvgSystolic, double AvgDiastolic)`. Averages computed once in view model.

### Step 8.2 — Blood Pressure Table View
`DataGrid`: Date, Time of Day, Avg Systolic, Avg Diastolic, Actions. Individual readings shown as de-emphasised sub-rows via row template `ItemsControl`. `ThresholdLevelToBrushConverter` (value converter in UI project) colours cells above threshold amber/red.

### Step 8.3 — Blood Pressure Add/Edit Dialog
`ObservableCollection<BpReadingRowViewModel>` (each row: `SystolicInput`, `DiastolicInput`, `ValidationError`). "Add Reading" button (max 5). "Remove" per row (min 1 enforced).

### Step 8.4 — Blood Pressure Chart
Two `LineSeries` (systolic, diastolic). Dashed `LineAnnotation` for each configured threshold. Legend enabled.

### Step 8.5 — Blood Pressure CSV Export
Wire export dialog to `CsvExportService.ExportBloodPressure`.

---

## Phase 9 — Blood Sugar Feature

### Step 9.1 — BloodSugarViewModel + display model
Display model: `record BloodSugarEntryDisplay(BloodSugarEntry Entry, decimal AvgReading, ThresholdLevel Level)`. `Level` computed via `ThresholdEvaluator.EvaluateBloodSugar` at load time using current settings.

### Step 9.2 — Blood Sugar Table View
Same `DataGrid` pattern. Row background/foreground driven by `Level` via `ThresholdLevelToBrushConverter`. Two-level colour: yellow/orange for `Warning`, red/amber for `Danger` and `BelowLower`.

### Step 9.3 — Blood Sugar Add/Edit Dialog
Same as BP dialog but max 3 reading rows, single `decimal` value per row, no time-of-day. Context always `"fasting"` (never shown to user, written automatically).

### Step 9.4 — Blood Sugar Chart
Single `LineSeries` for averaged reading. Up to three `LineAnnotation` lines: Warning (yellow/orange), Danger (red/amber), Lower (blue). Labels per line.

### Step 9.5 — Blood Sugar CSV Export
Wire to `CsvExportService.ExportBloodSugar`.

---

## Phase 10 — Dashboard

### Step 10.1 — DashboardViewModel
Replace stub. Inject all three services + `SettingsService`. `DateRangeFilterViewModel Filter` defaulting to last calendar month. `PlotModel WeightPlotModel`, `BloodPressurePlotModel`, `BloodSugarPlotModel`. `[RelayCommand] LoadAll(ct)` rebuilds all three. Navigation commands publish messages via `WeakReferenceMessenger` for `MainWindowViewModel` to handle.

### Step 10.2 — DashboardView
Replace stub. `DockPanel` with `DateRangeFilterView` docked top. Three-column `UniformGrid` with chart panels below. Each panel: `PlotView` + "Go to [metric]" button at bottom.

**Verification:** Dashboard shows three charts. Date filter refreshes all three. "Go to" buttons navigate correctly.

---

## Phase 11 — Tests

### Step 11.1 — ThresholdEvaluator tests
Pure unit tests, no mocks. Cover all `ThresholdLevel` outcomes for both metrics.

### Step 11.2 — WeightService tests
Mock `IWeightRepository`. Cover: valid add calls repo Add; weight ≤ 0 throws `ValidationException`; future date throws `ValidationException`; update/delete delegate correctly; `GetLast10DateRange` with ≥10 and <10 entries.

### Step 11.3 — BloodPressureService and BloodSugarService tests
Same structure. Additional edge cases: 0 readings → `ValidationException`; 6 BP readings → `ValidationException`; 4 BS readings → `ValidationException`.

### Step 11.4 — SettingsService tests
Mock `ISettingsRepository`. Load delegates. Save with all-null fields succeeds. Negative threshold value throws `ValidationException`.

### Step 11.5 — CsvExportService tests
Mock repositories returning fixed data. Export to temp file. Assert CSV content matches expected format including average rows.

### Step 11.6 — WeightRepository integration tests
Real `YearPartitionedStore` against temp directory (cleaned up in `Dispose`). Full CRUD cycle. Cross-year range query reads from two year files correctly.

### Step 11.7 — YearPartitionedStore tests
Atomic write: `.tmp` file does not linger after write. Corrupt file: `DataFileCorruptException` thrown on read.

**Verification:** `dotnet test` — all tests pass.

---

## Phase 12 — Polish

### Step 12.1 — Corrupt file error handling
In each metric's view model `LoadEntries` command, catch `DataFileCorruptException` and set `[ObservableProperty] string? ErrorMessage`. A `NotificationViewModel` singleton in DI holds `ObservableCollection<NotificationMessage>`. A persistent notification area in `MainWindow.axaml` renders it. Each notification has a dismiss command and a "Start with empty data" option for data corruption cases.

### Step 12.2 — Threshold warning notifications
After successful Add/Update in BP and BS view models, call `ThresholdEvaluator` with new entry's averages and current settings. If level ≠ `None`, post a warning `NotificationMessage`. No threshold warning for weight (requirements specify none).

### Step 12.3 — Input validation hardening
Audit all dialogs: Save button driven by `CanSave` computed property; pasting non-numeric text shows inline error without crash; BP/BS add-reading/remove-reading updates `CanSave`; date picker disallows dates more than 1 year in the future.

### Step 12.4 — UI thread safety audit
Confirm all `ObservableCollection` mutations occur on UI thread. Confirm `PlotModel.InvalidatePlot(true)` called on UI thread after background data loads.

### Step 12.5 — Window sizing and accessibility
`MinWidth="900"`, `MinHeight="600"` in `MainWindow.axaml`. `AutomationProperties.Name` set on all icon-only buttons. Verify Tab-key navigation reaches all interactive controls.

### Step 12.6 — Final smoke test
- Add/edit/delete entry for each metric; confirm persistence across restart
- Breach thresholds; confirm in-app warnings appear
- Export CSV for each metric; confirm format in spreadsheet
- Corrupt a JSON file; confirm graceful error (not crash)
- `dotnet test` — all tests pass
- `dotnet publish -c Release -r win-x64 --self-contained true` — produces working `.exe`

---

## Critical Files

| File | Importance |
|---|---|
| `src/HealthTracker.Shared/Interfaces/IWeightRepository.cs` (and siblings) | Contract shared by Infrastructure and Services; changing it cascades everywhere |
| `src/HealthTracker.Infrastructure/Json/YearPartitionedStore.cs` | Only file I/O primitive; atomic write and error handling live here |
| `src/HealthTracker.UI/AppBootstrapper.cs` | Central DI wiring; all layers converge here |
| `src/HealthTracker.UI/ViewModels/Shared/DateRangeFilterViewModel.cs` | Reused by all 5 views; a bug here affects the entire app |
| `src/HealthTracker.Services/ThresholdEvaluator.cs` | Single source of truth for threshold breach logic used across layers |

---

## Key Architectural Decisions

- **JSON model separation:** Private `*Json` record types in each repository match the file format exactly. Public DTOs in Shared carry no `[JsonPropertyName]` attributes — the two representations evolve independently.
- **YearPartitionedStore as the only I/O primitive:** All three repositories are thin wrappers. Atomic writes, encoding, and error wrapping are implemented exactly once.
- **DateRangeFilterViewModel as reusable sub-component:** Avoids duplicating filter logic across five views.
- **ThresholdEvaluator as pure functions:** No dependencies, no I/O. Shared between service layer (post-save warnings) and view model layer (table row colouring).
- **IDialogService abstraction:** View models never reference concrete Avalonia `Window` types, keeping them testable.
- **WeakReferenceMessenger for cross-view-model events:** Dashboard "Go to metric" navigation and threshold warnings communicate across view model boundaries without coupling.
