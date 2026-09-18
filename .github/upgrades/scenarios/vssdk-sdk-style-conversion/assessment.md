# Assessment: VSSDK SDK-Style Conversion

## Target Project
| Property | Value |
|----------|-------|
| Project | OpenRiaServices.VisualStudio.Installer |
| Path | src/VisualStudio/Installer/OpenRiaServices.VisualStudio.Installer.csproj |
| Current TFM | .NET Framework v4.7.2 |
| Solution format | .sln |
| packages.config | No |

## VSIX Components Found
- [x] VSIX manifest (`src/VisualStudio/Installer/source.extension.vsixmanifest`)
- [x] VSCT command table (`src/VisualStudio/Installer/VisualStudio.MenuExtension.vsct`)
- [ ] Tool windows
- [ ] MEF exports
- [x] Project templates included by VSIX project references
- [x] Item templates included by VSIX project references

## Included template projects to modernize
- `src/VisualStudio/Templates/CSharp/BusinessApplication/BusinessApplicationProjectTemplate.csproj`
- `src/VisualStudio/Templates/CSharp/RIAServicesLibrary/OpenRiaServicesLibrary.csproj`
- `src/VisualStudio/ItemTemplates/CSharp/AuthenticationDomainService/AuthenticationDomainService.csproj`
- `src/VisualStudio/ItemTemplates/CSharp/DomainServiceClass/DomainServiceClass.csproj`

## Current Package References (Installer)
- EnvDTE80 8.0.3
- Microsoft.VisualStudio.ManagedInterfaces.9.0 9.0.30730
- Microsoft.VisualStudio.ManagedInterfaces.WCF 9.0.21023
- Microsoft.VisualStudio.Shell.14.0 14.0.23205
- Microsoft.VisualStudio.Shell.Design 14.0.23205
- Microsoft.VisualStudio.Shell.Interop.10.0 10.0.30320
- Microsoft.VisualStudio.Validation 14.0.50702
- Microsoft.VisualStudio.Threading 14.0.50702
- Microsoft.VisualStudio.Utilities 14.0.23205
- Microsoft.VSSDK.BuildTools 16.11.69 (below required minimum 18.5.38461)
- VSSDK.ComponentModelHost 12.0.4

## Baseline
- Installer + included template projects build: **No**
- Build command:
  - `msbuild src/VisualStudio/Installer/OpenRiaServices.VisualStudio.Installer.csproj -restore /p:Configuration=Release /m /v:minimal`
- Observed errors:
  - `MSB4226` on template projects importing `$(VSToolsPath)\VSSDK\Microsoft.VsSDK.targets` under Visual Studio 18 path

## Key Findings
- The installer project is legacy non-SDK style and contains legacy VSIX debug properties (`StartAction`/`StartPrograms`/`StartArguments`) and legacy imports.
- Installer and all four template projects need SDK-style conversion to stop relying on legacy `Microsoft.VsSDK.targets` import path behavior.
- Installer keeps WPF/System.Design references and should preserve them after conversion.
- The solution is classic `.sln`, so deploy markers must be added via `Deploy.0` entries in `GlobalSection(ProjectConfigurationPlatforms)` for the installer project GUID.
