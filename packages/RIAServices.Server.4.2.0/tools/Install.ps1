param($installPath, $toolsPath, $package, $project)

try {
    $projectPath = Join-Path $project.Properties.Item("FullPath").Value -ChildPath $project.Properties.Item("FileName").Value
} catch { }

# Add a project import element for the validation targets
#
if ($projectPath -ne $null) {
    $relativeToolsPath = [NuGet.PathUtility]::GetRelativePath($projectPath, $toolsPath)
    $relativeTargetsPath = Join-Path $relativeToolsPath -ChildPath "Microsoft.Ria.Validation.targets"

    $rootElement = [Microsoft.Build.Construction.ProjectRootElement]::Open($projectPath)
    $rootElement.AddImport($relativeTargetsPath)
}

# Remove duplicate config entries to work around NuGet issue 1971 which creates duplicates when PublicKeyTokens only differ by case
#
try {
    $config = $project.ProjectItems | Where-Object { $_.Name -eq "Web.config" }    
    $configPath = $config.Properties | Where-Object { $_.Name -eq "FullPath" }
    $configXml = New-Object System.Xml.XmlDocument
    $configXml.PreserveWhitespace = $true
    $configXml.Load($configPath.Value)

    function RemoveDuplicateNode($nodes) {
        if ($nodes.Count -eq 2) {
            $node = $nodes.Item(1)
            if ($node.PreviousSibling.NodeType -eq [System.Xml.XmlNodeType]::Whitespace) {
                $node.ParentNode.RemoveChild($node.PreviousSibling);
            }
            $node.ParentNode.RemoveChild($node)
        }
    }

    # Remove duplicate configSections/sectionGroup/section/domainServices nodes
    #
    $configSections = $configXml.configuration.configSections
    RemoveDuplicateNode($configSections.SelectNodes("sectionGroup[@name='system.serviceModel']/section[@name='domainServices']"));

    # Remove duplicate system.web/httpModules/add/DomainServiceModule nodes
    #
    $httpModules = $configXml.configuration["system.web"].httpModules
    RemoveDuplicateNode($httpModules.SelectNodes("add[@name='DomainServiceModule']"));

    # Remove duplicate system.webServer/modules/add/DomainServiceModule nodes
    #
    $modules = $configXml.configuration["system.webServer"].modules
    RemoveDuplicateNode($modules.SelectNodes("add[@name='DomainServiceModule']"))

    $configXml.Save($configPath.Value)
} catch { }