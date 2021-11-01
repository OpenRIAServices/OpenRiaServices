
param($RepositoryRoot = "$PSScriptRoot", $SqlServer = "(localdb)\MSSQLLocalDB", [switch]$UseSqlCmd = $false)

$DatabaseFolder = join-path $RepositoryRoot "src/Test/Databases"

# Verify SqlServer or SQLPS module is installed
if (-not $UseSqlCmd)
{
	if ((-not (Get-Module SqlServer -ListAvailable)) -and (-not (Get-Module SQLPS)))
	{
		# This will install the nuget provider if not already installed
		Get-PackageProvider -Name NuGet -Force
		# Install-PackageProvider -Name NuGet -Force -Scope CurrentUser
		# Installs SQL Module
		Install-Module -Name SqlServer -AllowClobber -Force -Scope CurrentUser
	}
	
	# Check if module is loaded and if not import it
	if ((Get-Module SqlServer -ListAvailable) -and (-not (Get-Module SqlServer))) {
		Import-Module SqlServer
	}
	elseif ((Get-Module SQLPS -ListAvailable) -and (-not (Get-Module SQLPS))) {
		Import-Module SQLPS
	}
}

function Execute-SQL([string]$sql)
{
	if ($UseSqlCmd)
	{
		& sqlcmd -S $SqlServer -Q $sql
	}
	else
	{
		Invoke-Sqlcmd -ServerInstance $SqlServer -Query $sql
	}
}

# Remove old databases if any
function Remove-Database([string]$databaseName)
{
    $SqlCommand = 
@"
USE master
GO
if exists (select * from sysdatabases where name='$databaseName')
        drop database $databaseName
GO
"@;
    Execute-SQL $SqlCommand
}

function CreateDatabaseFromBackup([string]$databaseName, [string]$LogicalNameMDF, [string]$LogicalNameLDF)
{
    $DatabaseFileWithoutExt = join-path $DatabaseFolder $databaseName

	$BackupFile = $DatabaseFileWithoutExt + ".bak"
    $DataFile = $DatabaseFileWithoutExt + ".mdf"
    $LogFile =  $DatabaseFileWithoutExt + ".ldf"


    echo "Restoring a new backup of '$databaseName' from '$BackupFile' to '$DataFile'"

    Remove-Database $databaseName

    $SqlCommand = 
@"
RESTORE DATABASE $databaseName  
FROM DISK = '$BackupFile'  
WITH MOVE '$($LogicalNameMDF)' TO '$DataFile',  
MOVE '$($LogicalNameLDF)' TO '$LogFile'  
GO

DBCC SHRINKDATABASE('$databaseName');
GO

ALTER DATABASE [$databaseName] SET READ_ONLY;
go
"@;
    Execute-SQL $SqlCommand
}

function Take-Offline([string]$databaseName)
{
$SqlCommand =
@"
Alter database [$databaseName] set single_user with ROLLBACK IMMEDIATE;
go
alter database [$databaseName] set offline;
"@;
    Execute-SQL $SqlCommand
}

function Take-Online([string]$databaseName)
{
$SqlCommand =
@"
alter database [$databaseName] set online;
go
Alter database [$databaseName] set multi_user;
"@;
    Execute-SQL $SqlCommand
}

CreateDatabaseFromBackup "Northwind" "Northwind" "Northwind_log"
CreateDatabaseFromBackup "AdventureWorks" "AdventureWorks_Data" "AdventureWorks_Log"

# Take northwind offline and copy to templates folder
echo "Copying northwind database to websites"

Take-Offline "Northwind"
$mdf = (join-path $DatabaseFolder Northwind.mdf)
$ldf = (join-path $DatabaseFolder Northwind.ldf)
foreach($website in "src\Test\WebsiteFullTrust")
{
    $templateDir = Join-Path $RepositoryRoot "$website\App_Data\Templates\"
    if (-not (Test-Path $templateDir))
    {
        mkdir $templateDir -Force > $null
        echo "Created $templateDir"
    }
    copy $mdf $templateDir -Force
    copy $ldf $templateDir -Force
}
Take-Online "Northwind"
