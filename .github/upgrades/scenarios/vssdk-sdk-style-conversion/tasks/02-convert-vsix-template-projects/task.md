# 02-convert-vsix-template-projects: Convert VSIX template packaging projects to SDK-style

Convert template projects included by the installer (`BusinessApplicationProjectTemplate`, `OpenRiaServicesLibrary`, `AuthenticationDomainService`, `DomainServiceClass`) to SDK-style template-project format so they no longer depend on legacy VSSDK targets path resolution. Preserve their template assets (`.vstemplate`, icons, template content files) and keep non-deploying template container settings.

**Done when**: All four template projects are SDK-style, keep existing template output assets, remove legacy VSSDK import usage, and remain compatible with VSIX template packaging.

---
