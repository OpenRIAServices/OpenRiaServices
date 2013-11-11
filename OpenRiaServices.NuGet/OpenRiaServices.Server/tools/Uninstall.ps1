param($installPath, $toolsPath, $package, $project) 

try {
    $projectPath = Join-Path $project.Properties.Item("FullPath").Value -ChildPath $project.Properties.Item("FileName").Value
} catch { }

# Remove the project import element for the validation targets
#
if ($projectPath -ne $null) {
    $rootElement = [Microsoft.Build.Construction.ProjectRootElement]::Open($projectPath)

    $targetsImport = $rootElement.Imports | Where-Object {$_.Project -like "*Microsoft.Ria.Validation.targets"}
    while ($targetsImport.Parent.Count -eq 1) {$targetsImport = $targetsImport.Parent}
    $targetsImport.Parent.RemoveChild($targetsImport)
}
