# QA Hub

A desktop quality-assurance workspace for manual testers — test cases, test runs, defects, requirements traceability, CAPAR/RMA tracking, shipment records, and a serial port terminal for hardware testing, all in one app.

Built with WPF (.NET 10), MVVM, and plain JSON files for storage — no database server required.

## Features

- **Dashboard** — live stats, a pass/fail execution trend chart, a defect severity breakdown, and quick links into the rest of the app
- **Test Cases** — create, edit, and organize manual/automated test cases by area, priority, and status
- **Test Runs** — execute a batch of test cases against a build, mark each Pass/Fail/Blocked/Skipped, and track pass rate
- **Bugs** — log and track defects with severity, status, and assignee
- **Requirements** — trace requirements to the test cases that cover them, with a coverage status per requirement
- **Projects** — group work by product/project, with an active/archived lifecycle
- **Shipments** — track serial numbers shipped under a project, with QA status and firmware/hardware details
- **CAPAR** — corrective & preventive action requests, with root cause, corrective/preventive actions, and owner tracking
- **RMA** — track returned units through receiving, diagnosis, repair, retest, and reshipment
- **Reports** — pass-rate trends across test runs and requirement coverage, rolled up from real data
- **Knowledge Base** — searchable articles for known issues, environment setup guides, and team notes
- **Serial Terminal** — connect to a serial device, send/receive data, and export a timestamped log for manual hardware test evidence
- **Settings** — reset any section's data individually, or everything at once

## Getting Started

1. Open `QAHub.sln` in Visual Studio 2022 or later.
2. Restore NuGet packages (`System.IO.Ports` is the only external dependency).
3. Set `QAHub` as the startup project and run (F5).

**Requirements:** .NET 10 SDK, Windows (WPF is Windows-only).

## Data Storage

QA Hub stores all data as plain JSON files in a `data/` folder created next to `QAHub.sln` (not inside `bin/`). Each section has its own file:

```
data/
├── testcases.json
├── bugs.json
├── testruns.json
├── requirements.json
├── projects.json
├── shipments.json
├── capar.json
├── rma.json
└── knowledgebase.json
```

There's no database server or connection string to configure — the app reads and writes these files directly. To reset a section, use the **Settings** page in-app, or delete the corresponding file (it will be recreated empty on next save).

## Project Structure

```
QAHub/
├── Models/       Plain C# data classes (TestCase, Bug, TestRun, Requirement, Project, Shipment, Capar, Rma, KnowledgeArticle)
├── ViewModels/   MVVM view models — one per page, plus MainViewModel for navigation
├── Views/        WPF UserControls (XAML) — one per page
├── Services/     IDataService / JsonDataService (persistence) and SerialComService (serial port I/O)
├── App.xaml      View-model-to-view DataTemplate mappings, global styles/brushes
└── MainWindow.xaml   Sidebar navigation shell
```

## Architecture Notes

- **MVVM** throughout: views bind to view models via `ObservableObject`/`RelayCommand`; no code-behind logic beyond dialog/DataGrid glue that XAML can't express declaratively.
- **Navigation** is a single `ContentControl` in `MainWindow` whose `Content` swaps between view model instances, resolved to the right view via `DataTemplate`s in `App.xaml`.
- **List/detail/form pattern**: most pages (Test Runs, Requirements, Projects, Shipments, CAPAR, RMA, Knowledge Base) follow the same three-state flow — list view, read-only detail, and a shared create/edit form — for a consistent feel across the app.
