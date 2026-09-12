# 01-convert-vsix-installer: Convert VSIX installer project to SDK-style VSSDK format

Convert `src/VisualStudio/Installer/OpenRiaServices.VisualStudio.Installer.csproj` from legacy format to SDK-style while preserving VSIX manifest, VSCT, content/template packaging items, project references, signing settings, and required framework references. Replace legacy VSSDK package set with SDK-style-compatible package references and remove legacy imports/startup debug properties.

**Done when**: Installer project uses SDK-style format, preserves VSIX packaging behavior, has no legacy `Microsoft.VsSDK.targets` import, no legacy `StartAction`/`StartPrograms`/`StartArguments` properties, and retains target framework compatibility and output behavior.

---

## Scope Inventory

- **Projects affected**: `src/VisualStudio/Installer/OpenRiaServices.VisualStudio.Installer.csproj`.
- **Distinct concerns**:
  - Legacy-to-SDK project format conversion.
  - VSSDK package and property overlay requirements.
  - VSIX packaging/deploy metadata preservation.
- **Change signals**:
  - Legacy imports still present (`Microsoft.Common.props`, `Microsoft.CSharp.targets`, `Microsoft.VsSDK.targets`).
  - Legacy debug launch settings in project (`StartAction`, `StartPrograms`, `StartArguments`).
  - `Microsoft.VSSDK.BuildTools` currently `16.11.69`, below required minimum `18.5.38461` for SDK-style VSIX.
- **Skill matches loaded**: `converting-to-sdk-style`, `managing-package-references`, `modifying-project-properties`, `building-projects`.

## Research Findings

### Files to Modify
- `src/VisualStudio/Installer/OpenRiaServices.VisualStudio.Installer.csproj` — main conversion target.

### Files to Preserve/Validate
- `src/VisualStudio/Installer/source.extension.vsixmanifest` — preserve as None item with generator metadata.
- `src/VisualStudio/Installer/VisualStudio.MenuExtension.vsct` — preserve VSCT compile item.
- `src/RiaServices.sln` — deploy markers handled in a later task.

### Package Direction
- Replace legacy VS interop package set with `Microsoft.VisualStudio.SDK` metapackage.
- Raise `Microsoft.VSSDK.BuildTools` to at least `18.5.38461` (floor; do not downgrade higher versions).
- Keep package management local in project (no `packages.config`).

### Framework/Build Notes
- Keep target framework unchanged (`v4.7.2` -> `net472` equivalent in SDK-style).
- Preserve WPF/System.Design references (`PresentationCore`, `PresentationFramework`, `System.Design`).
- Build currently fails pre-conversion with `MSB4226` due to legacy `Microsoft.VsSDK.targets` import path resolution against VS18.
