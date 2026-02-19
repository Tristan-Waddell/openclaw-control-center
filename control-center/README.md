# Control Center (Epic 2 Skeleton)

Control Center is a **Windows-first WPF desktop application**.

Windows-only starter skeleton for Control Center with strict architecture boundaries:

- **UI** (`ControlCenter.UI`) → WPF shell + WebView2 host
- **App** (`ControlCenter.App`) → composition root, DI, environment profiles, logging bootstrap
- **Application** (`ControlCenter.Application`) → use-case services + interfaces
- **Domain** (`ControlCenter.Domain`) → core entities/rules
- **Infrastructure** (`ControlCenter.Infrastructure`) → SQLite-backed adapter implementations
- **Contracts** (`ControlCenter.Contracts`) → DTOs and public contracts
- **Tests** (`ControlCenter.Tests`) → baseline xUnit tests

## Build + Test (Windows)

```powershell
cd control-center
dotnet restore ControlCenter.sln
dotnet build ControlCenter.sln -c Release
dotnet test ControlCenter.sln -c Release
```

## Run UI

```powershell
cd control-center/src/ControlCenter.UI
$env:CONTROLCENTER_ENV = "Development"  # optional: Development | Production
dotnet run
```

## Environment Profiles

`ControlCenter.UI/appsettings*.json` controls profile-specific values:

- `appsettings.json` (Production defaults)
- `appsettings.Development.json`

`CONTROLCENTER_ENV` selects which override file is loaded.

## Dependency Rule Enforcement

Architecture guardrails are enforced in `Directory.Build.targets` during `dotnet build`:

- Domain cannot reference Application/UI/Infrastructure
- Application cannot reference UI/Infrastructure
- Infrastructure cannot reference UI/App

CI (`.github/workflows/control-center-ci.yml`) runs build + test on `windows-latest`, so rule breaks fail PRs.

## Contributing Expectations

- UI work targets Windows and should be verified on Windows.
- Keep architecture boundaries intact.
- Include tests for behavioral changes.
- Follow `CONTRIBUTING.md`, `CODE_OF_CONDUCT.md`, and PR/issue templates.
