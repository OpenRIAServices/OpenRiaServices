# Copilot instructions for OpenRiaServices

## Repository overview

OpenRiaServices is a .NET Foundation project evolved from WCF RIA Services. It provides client/server libraries, hosting packages, code-generation tooling, Visual Studio integration, and tests for rich n-tier applications.

- `src\OpenRiaServices.Client*`: client libraries/tests
- `src\OpenRiaServices.Server*`: server, EF/EF Core, and authentication
- `src\OpenRiaServices.Hosting.*`: hosting; prefer ASP.NET Core for new work
- `src\OpenRiaServices.Tools*`: code generation, MSBuild, and T4
- `src\VisualStudio`: VS extension/templates/tests
- `src\Test`: shared/integration assets and databases
- `NuGet`: packaging;
- `docs`: documentation (both new and legacy from WCF RIA Services)

OData feature is legacy, use them only when explicitly targeted.
The primary CI definition is `azure-pipelines.yml`.

## Environment and commands

Run from the repository root on Windows with Visual Studio/MSBuild and .NET Framework 4.7.2 targeting support. `src\global.json` requests .NET SDK 10.0.100 with major roll-forward; multi-targeted projects also require .NET 8. Cloud setup is in `.github\workflows\copilot-setup-steps.yml`.

```powershell
# Restore and Release-build (preferred)
msbuild src\RiaServices.sln -restore /p:Configuration=Release /m /v:minimal

# Test the solution after building
dotnet test src\RiaServices.sln --no-restore --configuration Release --settings src\test.runsettings
```

Prefer targeted project builds/tests. CI separates target frameworks and excludes `OpenRiaServices.Common*Test.dll` from its main test patterns; consult `azure-pipelines.yml` when local behavior differs.

Database-backed tests require SQL LocalDB and restored `Northwind`/`AdventureWorks` databases:

```powershell
sqllocaldb start MSSQLLocalDB
.\Setup-TestDatabases.ps1 # use -UseSqlCmd if the SqlServer module is unavailable
```

## Generated files

- NEVER edit `*.tt.cs` under `src\OpenRiaServices.Tools.TextTemplate\Framework`. Edit the corresponding `.tt`/`.ttinclude`, regenerate affected templates with Visual Studio **Transform All T4 Templates** or `devenv /Command TextTransformation.TransformAllTemplates`, review output, and build `src\OpenRiaServices.Tools.TextTemplate\Framework\OpenRiaServices.Tools.TextTemplate.csproj` for all targets.
- NEVER edit baseline `*.g.cs` or `*.g.vb` files directly. Run `dotnet test src\OpenRiaServices.Tools\Test\OpenRiaServices.Tools.Test.csproj --framework net472`, execute the exact `updateAllBaselines.bat` reported by failures, build, and rerun the test until it passes.

Run tests after building:

```powershell
dotnet test src\RiaServices.sln --configuration Release --settings src\test.runsettings
```

For focused changes, prefer running the relevant test project directly, for example:

```powershell
dotnet test src\OpenRiaServices.Hosting.AspNetCore\Test\OpenRiaServices.Hosting.AspNetCore.Test\OpenRiaServices.Hosting.AspNetCore.Test.csproj --configuration Release
```

CI separates test execution by target framework and excludes `OpenRiaServices.Common*Test.dll` from its main patterns. If a local full test run behaves differently from CI, inspect `azure-pipelines.yml` for the exact VSTest assembly patterns.

## Coding conventions

- Follow `.editorconfig` and existing style in nearby files.
- C# uses 4-space indentation and C# 14.0 (`src\Directory.Build.props`).
- Framework projects enable recommended analysis mode and XML documentation generation.
- New public, protected, and internal APIs should have XML documentation comments, this is not required for assembly local ("private") types.
- Release builds should not introduce warnings.
- Assembly signing is enabled centrally in `src\Directory.Build.props`; framework and test projects use keys from `src\snk`.
- Use existing libraries and patterns. Do not modernize unrelated legacy code while fixing a targeted issue.

## Versioning and packaging

- Version defaults and package metadata live in `src\Directory.Build.props`.
- CI uses GitVersion (`GitVersion.yml`) to derive build versions.
- NuGet package outputs go under `NuGet\bin` locally, or the artifact staging directory in CI.
- Packaging scripts and `.nuspec` files are under `NuGet`.

## Changelog and documentation expectations

- Treat changelog and documentation updates as part of completing a feature or notable behavior change, not as optional follow-up work.
- Record notable changes in the repository root changelog, `D:\a\OpenRiaServices\OpenRiaServices\Changelog.md`.
- Follow the existing changelog structure and keep entries compatible with the project's "Keep a Changelog" style release notes.
- The project aims to follow Semantic Versioning. Changes affecting explicitly versioned packages such as ASP.NET Core and EntityFrameworkCore should be documented clearly enough to support `major.minor.patch` release notes.
- When adding or changing a feature, update the most relevant project README in addition to code comments/API docs. For example, project-specific usage belongs in files such as `src\OpenRiaServices.Hosting.AspNetCore\Framework\README.md` and `src\OpenRiaServices.Server.EntityFrameworkCore\Framework\README.md`.
- Feature documentation should describe how to use the feature and, when helpful, why or when the feature makes sense.
- If a change is too small to merit a changelog entry, explicitly consider whether README or other documentation still needs an update.
- AI agents working in this repository should proactively check whether `Changelog.md`, a project README, `README.md`, `CONTRIBUTING.md`, or legacy docs under `docs` need updates as part of the same task.

## Known gotchas and workarounds

- The repository is a mixed modern/legacy .NET solution. A plain Ubuntu environment is not sufficient for normal validation.
- Some projects target `net472`, `net8.0`, `net10.0`, `net8.0-windows`, or combinations of these. Use targeted builds/tests when possible.
- Database-backed tests require SQL LocalDB and `Setup-TestDatabases.ps1`. If setup fails because the `SqlServer` PowerShell module is unavailable, retry with `-UseSqlCmd` when `sqlcmd` is installed.
- `Setup-TestDatabases.ps1` restores `Northwind` and `AdventureWorks`, marks them read-only, and copies Northwind database files into website test templates.
- Some tests and test assets are integration-heavy; `CONTRIBUTING.md` notes that not all tests are trivial to run locally.
- Code generation is build-integrated and can be hard to debug. Relevant areas include `OpenRiaServices.Tools`, `OpenRiaServices.Tools.CodeGenTask`, and `OpenRiaServices.Tools.TextTemplate`.
- For code-generation debugging, build in Debug, keep matching PDBs, inspect build output, and attach to the MSBuild/dotnet process as described in `CONTRIBUTING.md`.
- WCF hosting, ASP.NET Membership authentication, and some archived/Silverlight areas are legacy. Prefer ASP.NET Core paths for new work unless the task explicitly targets legacy behavior.
- The primary CI definition is `azure-pipelines.yml`; GitHub Actions currently cover CodeQL and Copilot setup.

## Validation

Use the smallest validation covering the change:

- Documentation only: inspect the Markdown; no full build normally needed.
- Library: build the solution and run nearby tests, set up LocalDB and run EndToEnd tests before finishing work
- Code generation/shared infrastructure: build the solution and run targeted code-generation tests.
- VS extension/templates: validate with Visual Studio/MSBuild on Windows; Linux is not representative.

Report exact commands, failures, and attempted workarounds in the task summary. Do not hide validation failures.
