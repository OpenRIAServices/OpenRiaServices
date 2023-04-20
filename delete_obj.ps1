If (Test-Path "TestResults")
{
   echo "Removing TestResults"
   remove-item -Path "TestResults" -Force -Recurse
}

$subfolders = "obj", "Generated_Code", "bin", "obj.Temp", "obj.Wpf"
foreach($subfolder in (dir $subfolders -Recurse))
{
	# -Force is required for read only or hidden files
	echo "Removing $subfolder"
	remove-item -Path $subfolder -Force -Recurse
}

remove-item  * -Include "project.lock.json" -Recurse
remove-item  * -Include "*.nuget.targets" -Recurse
remove-item  * -Include "*.nuget.props" -Recurse

# since we sometimes start from root folder and sometimes from Finance
$PluginsFolderPaths = "..\Lib\CRM\Plugins\", ".\Lib\CRM\Plugins\"
foreach($PluginsFolderPath in $PluginsFolderPaths )
{
	If (Test-Path $PluginsFolderPath)
	{
		remove-item $PluginsFolderPath\* -Recurse
	}	
}
