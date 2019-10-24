param(
    [string]$Path = ".",
	[string[]]$Include =  @("OpenRiaServices.*.nuspec"),
	[string]$Version = $null,
	[string]$NuGetPath
)

$scriptPath = (Split-Path -Parent $PSCommandPath)
Push-Location $scriptPath

$outputDir = "bin"
if (-not (Test-Path $outputDir)) {
    mkdir $outputDir
}

# If NuGet path is not specified then chekc one folder above git repo, or hope for it to be in the path
if ([string]::IsNullOrEmpty($NuGetPath))
{
	if (Test-Path ..\..\NuGet.exe) { $NuGetPath = "..\..\NuGet.exe"}
	else { $NuGetPath = "nuget.exe"}
}
[string[]]$NuGetParameters = @("-OutputDirectory", "$outputDir")
if (-not [string]::IsNullOrEmpty($Version)) {$NuGetParameters = $NuGetParameters + @("-Version", $Version)}

foreach($nuspec in (dir $Path -Recurse -Include $Include))
{
        echo "Building $nuspec"
        & $NuGetPath pack ($nuspec) $NuGetParameters
}

Pop-Location