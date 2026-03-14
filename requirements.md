# Health Tracker — Product Requirements Document

**Version:** 1.1  
**Date:** 2025-03-13  
**Platform:** Windows Desktop  
**Stack:** .NET 10 · C# 14 · Avalonia UI  

---

## 1. Overview

Health Tracker is a single-user Windows desktop application for personal health monitoring. It allows the user to record, review, and analyse three key health metrics: body weight, blood pressure, and blood sugar. All data is stored locally on the user's machine. The application provides both a tabular and a graphical view of historical data, and supports configurable health threshold warnings and CSV export.

---

## 2. Goals & Non-Goals

### 2.1 Goals
- Provide a clean, intuitive GUI for entering and reviewing health measurements.
- Store all data locally as JSON files; no network connectivity required.
- Support charting for trend analysis across all three metrics.
- Allow the user to edit and delete historical records with confirmation safeguards.
- Warn the user when a recorded measurement crosses a configurable threshold.
- Export data to CSV on a per-metric, per-date-range basis.

### 2.2 Non-Goals
- Cloud sync or remote backup.
- Multi-user or family profiles.
- Integration with external health devices or APIs.
- Mobile or web versions.
- Onboarding wizard or first-run setup flow.
- Free-text notes on measurements.

---

## 3. Technology Stack

| Concern | Choice |
|---|---|
| Runtime | .NET 10 |
| Language | C# 14 |
| UI Framework | Avalonia UI (latest stable compatible with .NET 10) |
| Data Format | JSON |
| Target OS | Windows (10 and later) |

---

## 4. Data Storage

### 4.1 Location
All data files are stored under the user's Documents folder in an application-specific subdirectory:

```
%USERPROFILE%\Documents\HealthTracker\
```

### 4.2 File Structure
Data is split by metric and by year. Each file contains all records for that metric within that calendar year.

```
Documents/
└── HealthTracker/
    ├── weight_2024.json
    ├── weight_2025.json
    ├── bloodpressure_2024.json
    ├── bloodpressure_2025.json
    ├── bloodsugar_2024.json
    └── bloodsugar_2025.json
```

When a new year begins, a new file is created automatically on the first data entry of that year. Files from prior years are never modified during normal operation (only during explicit edits or deletes of historical data).

### 4.3 JSON Schemas

**Weight entry**
```json
{
  "id": "uuid-v4",
  "date": "2025-03-13",
  "weight_kg": 82.4
}
```

**Blood pressure entry**
```json
{
  "id": "uuid-v4",
  "date": "2025-03-13",
  "time_of_day": "morning",
  "readings": [
    { "systolic_mmhg": 122, "diastolic_mmhg": 78 },
    { "systolic_mmhg": 119, "diastolic_mmhg": 76 }
  ]
}
```

`time_of_day` is an enum: `"morning"` | `"afternoon"`.

**Blood sugar entry**
```json
{
  "id": "uuid-v4",
  "date": "2025-03-13",
  "readings": [ 5.2, 5.4, 5.1 ],
  "context": "fasting"
}
```

`context` is always `"fasting"` in the current version. The field is included in the schema to allow future extension. Between 1 and 3 readings may be recorded per entry.

---

## 5. Metrics

### 5.1 Weight
- **Unit:** kilograms (kg), recorded to one decimal place.
- **Entry fields:** date, weight value.
- **One reading per entry.**
- **Target weight:** the user may optionally set a target body weight (kg) in Settings. This target is rendered as a horizontal reference line on the weight chart.

### 5.2 Blood Pressure
- **Unit:** mmHg (millimetres of mercury) for systolic and diastolic.
- **Entry fields:** date, time-of-day (morning / afternoon), and one or more individual readings. Each reading captures systolic and diastolic values.
- **Multiple readings per session:** the user may record between 1 and 5 readings within a single entry (same date + time-of-day). This supports the clinical practice of taking several consecutive measurements.
- **Averaging:** when multiple readings exist for an entry, the averaged values (systolic, diastolic) are computed automatically and used in charts and summary displays.

### 5.3 Blood Sugar
- **Unit:** mmol/L, recorded to one decimal place.
- **Context:** fasting measurements only. All readings are assumed to be taken before food consumption.
- **Entry fields:** date and between 1 and 3 individual blood sugar readings (mmol/L).
- **Multiple readings per entry:** the user may record 1 to 3 readings per entry to account for the imprecision of home testing kits. When multiple readings are recorded, their average is used in charts and summary displays. Individual readings are stored in the JSON file.

---

## 6. User Interface

### 6.1 Application Shell
- The main window is divided into a navigation sidebar and a content area.
- The sidebar contains the following items, in order: **Dashboard**, **Weight**, **Blood Pressure**, **Blood Sugar**, and **Settings**.
- Selecting a sidebar item loads the corresponding view in the content area.
- A persistent **Settings** entry at the bottom of the sidebar provides access to threshold and target configuration.
- The application opens to the **Dashboard** view on launch.

### 6.2 Dashboard View
- The dashboard is the home screen of the application and is the first view shown on launch.
- It displays three chart panels side by side (or stacked on narrow windows), one for each metric: Weight, Blood Pressure, and Blood Sugar.
- Each chart on the dashboard behaves the same as the chart in the individual metric view (threshold lines, target weight line, averaging, tooltips).
- The default date range on the dashboard is the **last calendar month**.
- A single shared date range filter at the top of the dashboard applies to all three charts simultaneously. The same filter options are available as in individual metric views (Week, Month, Year, Custom).
- Each chart panel includes a shortcut link/button to navigate to the full metric view for that metric.

### 6.3 Metric View Layout
Each metric view contains two sub-views, toggled by a segmented button or tab control:

1. **Chart View** *(default)* — displays measurements plotted over time.
2. **Table View** — displays individual records in a tabular format.

Both views share a common date range filter control (see Section 6.6).

### 6.4 Data Entry
- Each metric view has an **"Add Entry"** button, which opens a modal dialog for data entry.
- All entry dialogs include:
  - Date picker (defaulting to the current date).
  - Metric-specific input fields (see Section 5).
  - **Save** and **Cancel** buttons.
- For blood pressure, the dialog allows the user to add multiple readings within the same entry using an **"Add Reading"** button. Each reading row shows systolic and diastolic fields. Rows can be removed individually (at least one row must remain).
- For blood sugar, the dialog allows the user to add up to 3 readings using an **"Add Reading"** button. Each reading row shows a single mmol/L value field. Rows can be removed individually (at least one row must remain).
- Input validation is enforced before saving. Invalid or out-of-range values show an inline error message. The Save button is disabled until all fields are valid.

### 6.5 Table View

#### General behaviour
- Records are displayed in reverse chronological order by default (most recent first).
- The user can switch between **week view** (showing records grouped or filtered to a 7-day window) and **month view** (showing records for a calendar month).
- The default date range on opening a metric view shows the period that covers the **last 10 measurements** (regardless of how spread out those measurements are in calendar time).

#### Blood pressure table specifics
- Each blood pressure entry is shown as a single row, representing the date and time-of-day.
- If the entry has multiple readings, the **averaged values** (systolic / diastolic) are displayed prominently in the main row.
- Below or within the row, the individual component readings are shown in a visually de-emphasised style (e.g. smaller font, greyed-out text) so the user can see the raw data without it dominating the view.

#### Blood sugar table specifics
- Each blood sugar entry is shown as a single row representing the date.
- If the entry has multiple readings, the **averaged value** (mmol/L) is displayed prominently in the main row.
- Individual readings are shown in a visually de-emphasised style below or within the row, consistent with the blood pressure approach.

#### Edit and Delete
- Each row has an **Edit** and a **Delete** action (via icon buttons or a context menu).
- **Edit:** opens the entry's values in the same modal dialog used for data entry. On save, changes are written back to the source JSON file. A confirmation dialog ("Save changes to this entry?") is shown before writing.
- **Delete:** shows a confirmation dialog ("Are you sure you want to delete this entry? This cannot be undone.") before removing the record from the JSON file.
- Confirmations must be explicitly accepted; pressing Escape or clicking Cancel aborts the operation with no changes made.

### 6.6 Date Range Filter
A filter control is visible above both the Table View and the Chart View for each metric. It provides the following options:

| Mode | Description |
|---|---|
| Week | A 7-day window; arrow controls to step backward/forward by one week. |
| Month | A calendar-month window; arrow controls to step backward/forward by one month. |
| Year | A full calendar year; arrow controls or a year selector to navigate. |
| Custom | A date-range picker allowing the user to select arbitrary start and end dates. |

The default range on first opening a metric view always shows the range that encompasses the **last 10 measurements** for that metric. If fewer than 10 measurements exist, all measurements are shown.

### 6.7 Chart View
- Charts are rendered for all three metrics.
- The x-axis represents time (date); the y-axis represents the metric value.
- **Weight:** a single line/point series for weight in kg. If a target weight has been set in Settings, a horizontal dashed reference line is drawn at the target value, labelled "Target".
- **Blood pressure:** two series plotted simultaneously — systolic (mmHg) and diastolic (mmHg). If an entry has multiple readings, the **averaged** values are used as the plotted data point. Hovering a data point on the chart shows a tooltip with the averaged values and, if applicable, the number of individual readings that were averaged.
- **Blood sugar:** a single line/point series for the averaged mmol/L value. If an entry has multiple readings, the averaged value is plotted. Hovering a data point shows a tooltip with the averaged value and the number of individual readings.
- Charts respect the active date range filter.
- Charts include a legend identifying each series.
- Threshold lines (see Section 7) are drawn as horizontal dashed reference lines on the chart, using a distinct colour and label.

---

## 7. Health Thresholds & Warnings

### 7.1 Configurable Thresholds
The user can define threshold values for each metric via the Settings screen. For blood pressure, thresholds can be set independently for systolic and diastolic values. Blood sugar supports two upper threshold levels for graduated warnings.

| Metric | Configurable thresholds |
|---|---|
| Blood Pressure | Upper systolic (mmHg), Upper diastolic (mmHg) |
| Blood Sugar | Warning threshold (mmol/L) — "getting close"; Danger threshold (mmol/L) — "too high"; Lower limit (mmol/L) |

Thresholds are optional. If not set, no warning is shown for that metric/level.

### 7.2 Warning Behaviour — General
- When a newly entered measurement exceeds a configured threshold, a non-blocking warning notification is displayed within the application immediately after saving. The notification states which value exceeded which threshold and at which level.
- Threshold violations are highlighted visually in the Table View using the colour conventions below.
- In the Chart View, each configured threshold is drawn as a horizontal dashed reference line, labelled with its value and level.
- No warnings are shown retroactively when the user navigates historical data, unless explicitly triggered by opening a record that contains an out-of-range value.

### 7.3 Blood Sugar — Two-Level Warning
Blood sugar has two configurable upper thresholds, each with a distinct visual treatment:

| Level | Condition | Colour |
|---|---|---|
| **Warning** | Averaged reading ≥ Warning threshold (but below Danger threshold) | Yellow / Orange |
| **Too High** | Averaged reading ≥ Danger threshold | Amber / Red |

- Both thresholds are independently configurable. If only one is set, only that level is active.
- The Warning threshold is intended to signal that the value is approaching the Danger threshold. It is the user's responsibility to set meaningful values (e.g. Warning at 6.5 mmol/L, Danger at 7.0 mmol/L).
- In the Chart View, both threshold lines are rendered — the Warning line in yellow/orange and the Danger line in amber/red.
- In the Table View, out-of-range values are coloured according to the highest level they breach.

### 7.4 Warning Behaviour — Blood Pressure
Blood pressure has a single upper threshold for systolic and a single upper threshold for diastolic. These use a single warning colour (amber/red) when exceeded. Blood sugar's lower limit also uses this same single amber/red treatment when the reading falls below it.

---

## 8. CSV Export

### 8.1 Scope
CSV export is available per metric. Each export produces a single CSV file containing all records for the selected metric within a chosen date range.

### 8.2 Export Flow
1. The user opens the export dialog from a dedicated **"Export"** button within each metric view.
2. The dialog presents:
   - The metric name (read-only, context from which it was opened).
   - A date range picker (start date, end date).
   - A **"Export"** button and a **"Cancel"** button.
3. On clicking Export, a standard Windows Save File dialog opens, pre-filled with a suggested filename (e.g. `healthtracker_weight_2025-01-01_2025-03-13.csv`).
4. The file is written to the user-chosen location.

### 8.3 CSV Format

**Weight**
```
Date,Weight_kg
2025-03-13,82.4
```

**Blood pressure**  
Individual readings and their average are both included.
```
Date,TimeOfDay,Reading,Systolic_mmHg,Diastolic_mmHg
2025-03-13,Morning,1,122,78
2025-03-13,Morning,2,119,76
2025-03-13,Morning,Average,120.5,77
```

**Blood sugar**  
Individual readings and their average are both included.
```
Date,Reading,BloodSugar_mmol_L,Context
2025-03-13,1,5.2,Fasting
2025-03-13,2,5.4,Fasting
2025-03-13,3,5.1,Fasting
2025-03-13,Average,5.23,Fasting
```

---

## 9. Settings

The Settings screen (accessible from the sidebar) contains:

- **Target weight** (kg) — optional. Displayed as a reference line on the weight chart.
- **Threshold configuration** for each metric (see Section 7). Each threshold field is optional and can be cleared.
- A **"Save Settings"** button. Changes take effect immediately on save.
- Settings are persisted in a dedicated `settings.json` file in the same `HealthTracker` directory as the data files.

**Settings schema**
```json
{
  "target_weight_kg": 80.0,
  "thresholds": {
    "bp_systolic_upper_mmhg": 140,
    "bp_diastolic_upper_mmhg": 90,
    "blood_sugar_warning_mmol_l": 6.5,
    "blood_sugar_danger_mmol_l": 7.0,
    "blood_sugar_lower_mmol_l": 3.9
  }
}
```

---

## 10. Non-Functional Requirements

| Requirement | Detail |
|---|---|
| Startup time | The application should reach a usable state within 3 seconds on a modern Windows PC. |
| Data integrity | All writes should be atomic where possible (write to a temp file, then rename) to avoid corruption on crash. |
| File encoding | All JSON and CSV files are UTF-8 encoded. |
| Error handling | If a data file is missing or malformed on load, the application should display a clear error message and offer to start with an empty dataset for that metric/year rather than crashing. |
| Accessibility | The application should follow Avalonia UI accessibility conventions (keyboard navigation, sufficient colour contrast). |
| Localisation | The application is English-only in its initial version. Dates are displayed in the user's system locale format. |
| Window behaviour | The application window is resizable with a sensible minimum size. Layout should respond gracefully to window resizing. |

---

## 11. File & Folder Summary

```
%USERPROFILE%\Documents\HealthTracker\
├── settings.json
├── weight_<year>.json          (one per calendar year)
├── bloodpressure_<year>.json   (one per calendar year)
└── bloodsugar_<year>.json      (one per calendar year)
```

---

## 12. Out of Scope (Future Considerations)

The following features are explicitly out of scope for version 1.0 but may be considered for future versions:

- Additional measurement contexts for blood sugar (e.g. post-meal, bedtime).
- PDF report generation.
- Data import from CSV.
- Trend analysis or statistical summaries (moving averages, min/max annotations).
- Reminder / notification system for scheduled measurements.
- Dark mode / theme switching.
- Backup and restore functionality.

---

*End of document*
