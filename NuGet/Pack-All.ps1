param(
	[string]$Path = ".",
	[string[]]$Include = @("OpenRiaServices.*.nuspec"),
	[string]$Version,
	[string]$Configuration = "Release",
	[string]$OutputDirectory = (Join-Path $PSScriptRoot "bin"),
	[switch]$NoBuild
)

$ErrorActionPreference = "Stop"
$outputDir = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutputDirectory)
Push-Location $PSScriptRoot
try {
	$projectParameters = @("--configuration", $Configuration, "--output", $outputDir)
	if ($NoBuild) { $projectParameters += "--no-build" }
	if (-not [string]::IsNullOrEmpty($Version)) { $projectParameters += "-p:PackageVersion=$Version" }

	& dotnet pack ..\src\OpenRiaServices.slnx @projectParameters
	if ($LASTEXITCODE -ne 0) { throw "Project packing failed with exit code $LASTEXITCODE." }

	if ([string]::IsNullOrEmpty($Version)) {
		$Version = & dotnet msbuild ..\src\OpenRiaServices.Server\Framework\OpenRiaServices.Server.csproj -nologo -getProperty:PackageVersion
		if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($Version)) { throw "Could not determine the package version for nuspec packing." }
	}
	$nuspecParameters = @("--output", $outputDir)
	$nuspecParameters += @("--version", $Version)
	foreach ($nuspec in (Get-ChildItem $Path -Recurse -Include $Include)) {
		Write-Host "Packing $($nuspec.FullName)"
		& dotnet pack $nuspec.FullName @nuspecParameters
		if ($LASTEXITCODE -ne 0) { throw "Packing $($nuspec.Name) failed with exit code $LASTEXITCODE." }
	}
}
finally {
	Pop-Location
}
