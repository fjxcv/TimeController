# AGENTS.md

## Overview

This project is a C# WPF-based time management application built on .NET 9.0 using the MVVM architectural pattern. It consists of two main usage modes: "Casual Mode" (咸鱼模式) and "Strong Management Mode" (强管理模式), supporting daily/weekly planning, review, and scheduling.

Multiple AI agents assist in the development and maintenance of various modules in the system, including view rendering, logic binding, review analytics, and UI interaction.

---

## Agents

### 1. CasualModeAgent
- **Role**: Handles tasks related to the "Casual Mode" (咸鱼日常模式), where users freely create, manage, and delete non-strict tasks.
- **Scope**: 
  - `Views/CasualMode/`
  - `ViewModels/CasualModeViewModel.cs`
- **Responsibilities**:
  - Design and maintain XAML UI for flexible task creation and management
  - Bind user actions (e.g., mark as complete, delete, edit) to commands
  - Ensure reactive task list rendering with MVVM bindings

---

### 2. StrongManagementAgent
- **Role**: Manages the "Strong Management Mode" (强目标管理), including structured weekly/monthly task planning and associated review modules.
- **Scope**:
  - `Views/StrongGoalWeek/`, `Views/StrongGoalMonth/`, `Views/Review/`
  - `ViewModels/StrongGoalWeekViewModel.cs`, `StrongGoalMonthViewModel.cs`
- **Responsibilities**:
  - Implement structured planning interfaces for:
    - Weekly task blocks with timeline interaction
    - Monthly calendar with date-tile task overview
  - Manage all-day and time-bound tasks using fields like `IsAllDay`, `StartTime`, `EndTime`
  - Coordinate input handling for task creation/editing across different views
  - Interface with review system for data collection

---

### 3. ReviewAgent
- **Role**: Analyzes task history and generates structured review feedback based on user behavior and system records.
- **Scope**:
  - Views: `Views/Review/ReviewView_everyday.xaml`, `Views/Review/ReviewView_everyweek.xaml`
  - ViewModels: `ViewModels/ReviewViewModel_everyday.cs`, `ViewModels/ReviewViewModel_everyweek.cs`
- **Responsibilities**:
  - Calculate and display:
    - Daily and weekly task completion rates
    - Postponement and abandonment frequencies
    - Category-specific task statistics
  - Generate line charts and bar graphs to support user reflection
  - Collect and bind user-selected review metadata such as:
    - Delay reasons
    - Abandonment reasons
  - Provide system-generated suggestions to help improve time management

---

## Agent Collaboration Workflow

1. **CasualModeAgent** manages non-strict tasks in a lightweight format without date/time constraints.
2. **StrongManagementAgent** provides a high-discipline structure for time-bound scheduling:
   - **Week View** supports vertical time axis planning
   - **Month View** displays all-day tasks per date card
3. **ReviewAgent** reflects on user behavior using visual analytics and review feedback tools, sourced from structured logs and user-selected reasons.
4. All agents interact through shared data models and logic in `Models/TaskModel.cs` and `Services/TaskService.cs`, with persistence handled by Entity Framework (`TaskDbContext`).

---

## File Structure Overview

| Directory                      | Description                            |
| ------------------------------ | -------------------------------------- |
| `Views/`                       | XAML UI layout files                   |
| `ViewModels/`                  | Logic and command binding (MVVM)       |
| `Models/`                      | Shared task model definitions          |
| `Services/`                    | Database and backend logic             |
| `Resources/`                   | Icons, style dictionaries, static data |
| `Views/Review/ReviewView_*`    | Daily/Weekly review XAML views         |
| `ViewModels/ReviewViewModel_*` | Corresponding logic files              |

---

## Notes

- All modules follow MVVM strictly.
- Task time logic (`PlannedDate`, `IsAllDay`, `StartTime`, `EndTime`) is centralized in `TaskModel`.
- Database is managed via EF Core with SQLite as the local backend.
- Review feedback logic may later support monthly summaries or AI-prompted suggestions.