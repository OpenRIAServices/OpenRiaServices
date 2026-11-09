---
name: release-preparation
description: Prepare an OpenRiaServices release by updating NuGet packages, updating release notes, and validating the solution.
---

# Release preparation and dependency updates

Use this skill when preparing an OpenRiaServices release. Its package version policy and dependency-update guidance also apply whenever updating NuGet packages or other dependencies outside release preparation; use those parts without performing the release-note or release-version steps unless the update is part of a release.

## Package version policy

- Preserve wildcard package references for patch updates. Do not pin a wildcard reference to the latest resolved patch.
- Before changing a dependency to a newer minor or major version, identify the latest stable version compatible with each target framework and ask the maintainer to approve the version change.
- Keep Microsoft.EntityFrameworkCore references aligned with their target frameworks: EF Core 8 for `net8.0`, EF Core 10 for `net10.0`. Preserve any separate legacy target-framework dependency unless the maintainer requests a migration.
- MSBuild dependencies in framework and code-generation projects must support every target framework and the minimum supported Visual Studio version. Prefer the newest compatible MSBuild minor version; for .NET 8 / Visual Studio 2022, use the newest minor that supports those targets.
- Keep Visual Studio extension dependencies compatible with the latest supported Visual Studio 2022 baseline. Treat a release due to reach end of support within 14 days as already out of support.
- Update test dependencies, including MSTest, where compatible. If those updates introduce analyzer warnings, fix the warnings in a separate commit from the dependency update.
- When updating NuGet dependencies, update the corresponding package `.nuspec` file(s) under `NuGet\` so their dependency versions match the project references. Review all affected `.nuspec` files and verify their dependency versions against the framework project files.

## Release notes (only when preparing a release)

- After dependency updates, revise the root `Changelog.md` Unreleased heading to match the next planned minor release version(s).
- Keep the ASP.NET Core package version track distinct from the main package version track; use the corresponding next minor version for each.
- Add a concise dependency-update entry under the appropriate Unreleased section.

## Validation

- Restore and Release-build `src\RiaServices.sln`.
- Set up SQL LocalDB databases with `sqllocaldb start MSSQLLocalDB` and `.\Setup-TestDatabases.ps1 -UseSqlCmd` when the SQL Server PowerShell module is unavailable.
- Run all tests from `src` with `dotnet test --solution RiaServices.Tests.slnf --configuration Release`.
- Report commands, failures, and workarounds accurately. Do not claim full validation if a required tool, target framework, or database is unavailable.
