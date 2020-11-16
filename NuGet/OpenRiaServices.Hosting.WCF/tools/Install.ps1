param($installPath, $toolsPath, $package, $project)

try {
    $projectPath = Join-Path $project.Properties.Item("FullPath").Value -ChildPath $project.Properties.Item("FileName").Value
} catch { }

# Remove duplicate config entries to work around NuGet issue 1971 which creates duplicates when PublicKeyTokens only differ by case
#
try {
    $config = $project.ProjectItems | Where-Object { $_.Name -eq "Web.config" }    
    $configPath = $config.Properties | Where-Object { $_.Name -eq "FullPath" }
    $configXml = New-Object System.Xml.XmlDocument
    $configXml.PreserveWhitespace = $true
    $configXml.Load($configPath.Value)

    function RemoveNodesWithOldNamespaces($nodes) {
        foreach($node in $nodes)  {
            if ($node.Attributes["type"].Value.Contains("DomainServices")) {
                if ($node.PreviousSibling.NodeType -eq [System.Xml.XmlNodeType]::Whitespace) {
                    $node.ParentNode.RemoveChild($node.PreviousSibling);
                }
                $node.ParentNode.RemoveChild($node);
            }
        }
    }

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
    RemoveNodesWithOldNamespaces($configSections.SelectNodes("sectionGroup[@name='system.serviceModel']/section[@name='domainServices']"));
    RemoveDuplicateNode($configSections.SelectNodes("sectionGroup[@name='system.serviceModel']/section[@name='domainServices']"));

    # Remove all system.web/httpModules/add/DomainServiceModule nodes
    #
    $httpModules = $configXml.configuration["system.web"].httpModules
    foreach($httpModule in $httpModules.SelectNodes("add[@name='DomainServiceModule']")) {
        $httpModules.RemoveChild($httpModule)
    }

    # Remove duplicate system.webServer/modules/add/DomainServiceModule nodes
    #
    $modules = $configXml.configuration["system.webServer"].modules
    RemoveNodesWithOldNamespaces($modules.SelectNodes("add[@name='DomainServiceModule']"))
    RemoveDuplicateNode($modules.SelectNodes("add[@name='DomainServiceModule']"))

    $configXml.Save($configPath.Value)
} catch { }



/////////////////// TODOD_ REMOVE ALLA WITH DOMAINSERVICES