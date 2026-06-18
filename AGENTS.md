# AGENTS.md

## Project
Diadia HEIC Converter is a Windows desktop app for converting HEIC/HEIF photos to JPG.

## Tech Stack
- C# .NET 8 or .NET 9
- WPF
- MVVM
- Windows 10/11

## Rules
- Do not put business logic in MainWindow.xaml.cs.
- Keep conversion logic inside Services.
- Never delete original image files.
- Never overwrite user files unless the setting is explicitly enabled.
- Use async operations for long-running work.
- UI must not freeze during conversion.
- Keep all user-facing strings centralized for future localization.
- Add clear error handling for file permission, corrupted image, unsupported format, and cancellation.

## Build
Use:
dotnet build

## Quality
Before finishing a task, check:
- Project builds successfully.
- No obvious UI-thread blocking.
- No destructive file operations.
- New services are testable.
- README is updated when behavior changes.