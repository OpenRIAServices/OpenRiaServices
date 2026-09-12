# 02-convert-vsix-template-projects: Convert VSIX template packaging projects to SDK-style

Convert template projects included by the installer (`BusinessApplicationProjectTemplate`, `OpenRiaServicesLibrary`, `AuthenticationDomainService`, `DomainServiceClass`) to SDK-style template-project format so they no longer depend on legacy VSSDK targets path resolution. Preserve their template assets (`.vstemplate`, icons, template content files) and keep non-deploying template container settings.

**Done when**: All four template projects are SDK-style, keep existing template output assets, remove legacy VSSDK import usage, and remain compatible with VSIX template packaging.

---

## Research Findings

This scope was completed as part of task `01-convert-vsix-installer` because the installer build path depends on all four template projects exposing `TemplateProjectOutputGroup` under SDK-style conversion.

Converted template projects:
- `src/VisualStudio/Templates/CSharp/BusinessApplication/BusinessApplicationProjectTemplate.csproj`
- `src/VisualStudio/Templates/CSharp/RIAServicesLibrary/OpenRiaServicesLibrary.csproj`
- `src/VisualStudio/ItemTemplates/CSharp/AuthenticationDomainService/AuthenticationDomainService.csproj`
- `src/VisualStudio/ItemTemplates/CSharp/DomainServiceClass/DomainServiceClass.csproj`

Validation evidence:
- Installer build now resolves template output groups and produces VSIX.
- Legacy `Microsoft.VsSDK.targets` imports were removed from all four template projects.
