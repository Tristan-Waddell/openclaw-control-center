# Contributing to Control Center

Thanks for your interest in contributing.

## Project scope

Control Center is a **Windows-first WPF desktop application**. UI changes should be developed and validated on Windows.

## Getting started

1. Fork and clone the repository.
2. Create a branch from `main`.
3. Build and test before opening a PR:

```powershell
dotnet restore ControlCenter.sln
dotnet build ControlCenter.sln -c Release
dotnet test ControlCenter.sln -c Release
```

## Contribution expectations

- Keep changes focused and minimal.
- Preserve architecture boundaries enforced by `Directory.Build.targets`.
- Add/update tests for behavior changes.
- Use clear commit messages and PR descriptions.

## Pull requests

- Link related issues.
- Describe what changed and why.
- Include validation steps/run output.
- Be responsive to review feedback.

By participating, you agree to follow our [Code of Conduct](CODE_OF_CONDUCT.md).
