#####################################
#
# This file can be used to build OpenRiaServices under coverity analysis tools
#
# Once built you must manually package and upload the results to the coverity service
# for analysis.
#
#####################################

$sln = "RiaServices.sln" 
$buildCMD = "C:\Program Files (x86)\MSBuild\14.0\Bin\msbuild.exe"
$covBuild = [string](dir "C:\cov-analysis-*\bin\cov-build.exe" | select -first 1)

if ([string]::IsNullOrEmpty($covBuild)) {
  Write-Error "coverity analysis tools should be extracted directly under C:"
  return -1
}
else
{
  echo "Using coverity from '$covBuild'"
}

& nuget restore $sln

if (-not $env:CONFIGURATION) {
 $env:CONFIGURATION = "Release"
}

if (-not $env:PLATFORM) {
 $env:PLATFORM = "Any CPU"
}

& msbuild $sln /t:Clean "/p:Configuration=$env:CONFIGURATION" "/p:Platform=$env:PLATFORM"

# Based on https://thehermeticvault.com/software-development/using-coverity-scan-with-appveyor
# Define build command.

$buildArgs = @(
  $sln,
  "/p:UseSharedCompilation=false",
  "/t:Framework\Desktop\OpenRiaServices_DomainServices_Client;Framework\Desktop\OpenRiaServices_DomainServices_Client_Web;Framework\Desktop\OpenRiaServices_DomainServices_Hosting_Endpoint;Framework\Desktop\OpenRiaServices_DomainServices_EntityFramework",
  "/m",
  "/verbosity:minimal",
  "/p:Configuration=$env:CONFIGURATION",
  "/p:Platform=$env:PLATFORM")
  
& $covBuild --dir cov-int $buildCmd $buildArgs

