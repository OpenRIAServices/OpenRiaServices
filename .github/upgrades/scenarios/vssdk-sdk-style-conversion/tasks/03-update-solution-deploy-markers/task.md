# 03-update-solution-deploy-markers: Add deploy markers for VSIX debugging in solution

Update `src/RiaServices.sln` to ensure the installer project has deploy markers in `ProjectConfigurationPlatforms`, replacing reliance on old `StartProgram` debug launch settings for experimental instance deployment.

**Done when**: `RiaServices.sln` contains `Deploy.0` entries for the installer project GUID in Debug and Release Any CPU configurations.

---
