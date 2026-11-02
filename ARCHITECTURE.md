# Project Architecture

## Overview
The `CincoVertice.MyTools` project is a WinForms application designed to manage daily tasks and projects. It is built with
a modular architecture to ensure scalability, maintainability, and reusability.

## Layered Architecture
The project is organized into the following layers:

### 1. **Common Layer**
   - **Purpose**: Provides reusable components and utilities shared across multiple projects.
   - **Location**: `common/src/`
   - **Key Components**:
     - **CincoVertice.Common.Ui**: Contains UI-related utilities and themes.
     - **CincoVertice.Common.Utils**: Includes helper classes for common operations.
     - **CincoVertice.Common.WinApi**: Provides wrappers and helpers for Windows API interactions.
     - **CincoVertice.Common.Application**: The code standard. `CodeCheckerService` reports rule violations per
       file and folder; `CodeFixerService` fixes the automatically fixable ones in place through `FixerPipeline`.

### 2. **Application Layer**
   - **Purpose**: Implements the core application logic and services.
   - **Location**: `src/CincoVertice.MyTools.Ui/`
   - **Key Components**:
     - **Calculator**: `CalculatorPresenter` evaluates the expression from `ICalculatorView` and shows the result or
       the error (selecting the character at fault). Tested in `MyTools.Ui.Tests` with a fake view.
     - **CodeReview**: Handles code review functionalities with presenters, views, and adapters.
     - **Services**: Contains application-specific services.
     - **Setup**: Configures dependency injection and application services.

### 3. **UI Layer**
   - **Purpose**: Manages the user interface and user interactions.
   - **Location**: `src/CincoVertice.MyTools.Ui.Win/`
   - **Key Components**:
     Organized by feature: each folder holds a feature's forms and the services that belong to it, matching the
     feature folders in `Ui` (e.g. `Ui/CodeReview` and `Ui.Win/CodeReview`). Namespaces follow folders. Inside a
     feature, forms sit at the root, services go in `Services/`, and user-started actions go in `Actions/` (one class
     per action, with `ExecuteAsync`).
     - **Shell**: What makes this a tray app. `TrayApplicationContext` is the application root: it lives in the
       systray, owns the tray icon and its menu, and shows `MainForm` on demand. `Services/GlobalHotkeyService` owns
       the global hotkeys (e.g. Ctrl+Alt+A opens the calculator).
     - **Calculator**: `CalculatorForm` implements `ICalculatorView`; `Actions/ShowCalculatorAction` shows it (from the
       global hotkey) through `CalculatorPresenter`.
     - **CodeReview**: `CodeReviewForm`; `Services/CodeReviewWindowService` opens each window in its own DI scope;
       `Actions/CheckFolderAction` (folder picker + check) and `Actions/FixFolderAction` (folder picker, warning,
       fix in place, then check again) are started from the tray menu.
     - **Behaviors**: Reusable window plumbing: `DragMover`, `KeyboardMover`, and `FormExtensions`
       (`CenterOnCursorScreen`, `HideOnUserClose`).
     - **Resources**: Stores application resources such as icons and localized strings.
     - **Setup**: Configures UI-specific services and settings.

## Dependency Injection
The project uses `Microsoft.Extensions.DependencyInjection` for dependency injection. The `Setup` classes in the `Ui`
and `Ui.Win` layers register services and configure the application.

## Execution Flow
1. **Entry Point**: The application starts in `Program.cs` located in `CincoVertice.MyTools.Ui.Win`.
2. **Configuration**: The `AddMyToolsConfigurationProviders` method loads configuration settings.
3. **Service Registration**: The `AddMyToolsUiWinLayer` method registers services and dependencies.
4. **Tray Context**: `TrayApplicationContext` is resolved and passed to `Application.Run`. It starts the global
   hotkeys and shows the tray icon. The app starts with no window open.
5. **Main Form**: `MainForm` (a singleton) is shown from the tray icon (double-click or Open). Minimizing or closing
   it hides it back to the tray. Only Exit on the tray menu quits the application.
