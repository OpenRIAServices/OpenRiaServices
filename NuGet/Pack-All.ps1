param(
	[string[]]$Path =  @("OpenRiaServices.*", "."),
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

foreach($folder in (dir $Path | where {[System.IO.Directory]::Exists($_)}))
{
    $targets = (dir "$folder\*.nuspec")
    foreach($nuspec in $targets)
    {
        echo "Building $nuspec"
        & $NuGetPath pack ($nuspec) $NuGetParameters
    }
}

Pop-Location