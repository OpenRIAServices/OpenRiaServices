# VSSDK SDK-Style Conversion Plan

## Overview

**Target**: Modernize the Visual Studio VSIX installer project and its VSIX-packaged template projects to SDK-style extension project format.
**Scope**: Visual Studio extension area with one VSIX project and four template-packaging projects under `src/VisualStudio/Templates` and `src/VisualStudio/ItemTemplates`.

## Tasks

### 01-convert-vsix-installer: Convert VSIX installer project to SDK-style VSSDK format

Convert `src/VisualStudio/Installer/OpenRiaServices.VisualStudio.Installer.csproj` from legacy format to SDK-style while preserving VSIX manifest, VSCT, content/template packaging items, project references, signing settings, and required framework references. Replace legacy VSSDK package set with SDK-style-compatible package references and remove legacy imports/startup debug properties.

**Done when**: Installer project uses SDK-style format, preserves VSIX packaging behavior, has no legacy `Microsoft.VsSDK.targets` import, no legacy `StartAction`/`StartPrograms`/`StartArguments` properties, and retains target framework compatibility and output behavior.

---

### 02-convert-vsix-template-projects: Convert VSIX template packaging projects to SDK-style

Convert template projects included by the installer (`BusinessApplicationProjectTemplate`, `OpenRiaServicesLibrary`, `AuthenticationDomainService`, `DomainServiceClass`) to SDK-style template-project format so they no longer depend on legacy VSSDK targets path resolution. Preserve their template assets (`.vstemplate`, icons, template content files) and keep non-deploying template container settings.

**Done when**: All four template projects are SDK-style, keep existing template output assets, remove legacy VSSDK import usage, and remain compatible with VSIX template packaging.

---

### 03-update-solution-deploy-markers: Add deploy markers for VSIX debugging in solution

Update `src/RiaServices.sln` to ensure the installer project has deploy markers in `ProjectConfigurationPlatforms`, replacing reliance on old `StartProgram` debug launch settings for experimental instance deployment.

**Done when**: `RiaServices.sln` contains `Deploy.0` entries for the installer project GUID in Debug and Release Any CPU configurations.

---

### 04-validate-vsix-modernization: Validate build output and conversion completeness

Clean and build the installer project and included template projects, verify VSIX generation expectations, and confirm all converted projects are free of legacy import patterns and duplicate compile item issues. Validate that conversion goals are met without changing intended scope.

**Done when**: Validation build passes for converted projects, no conversion regressions are found, and all task success criteria above are satisfied.
