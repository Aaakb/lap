param([string]$Source = (Join-Path $PSScriptRoot '..\publish'),[string]$Output = (Join-Path $PSScriptRoot '..\installer-output\AlAsmaiLab-Setup-0.1.1.msi'))
$ErrorActionPreference='Stop'
$Source=[IO.Path]::GetFullPath($Source); $Output=[IO.Path]::GetFullPath($Output)
if(!(Test-Path -LiteralPath (Join-Path $Source 'MedicalLab.exe'))) { throw 'Publish the application first.' }
& (Join-Path $PSScriptRoot 'Verify-Publish.ps1') -Source $Source
$project=[xml](Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot '..\MedicalLab\MedicalLab.csproj'))
$productVersion=[string]$project.Project.PropertyGroup.Version
New-Item -ItemType Directory -Force -Path (Split-Path $Output) | Out-Null
if(Test-Path -LiteralPath $Output) { throw 'Output exists; choose a new output path.' }
$work=Join-Path $env:TEMP ('AlAsmaiLab-build-'+[Guid]::NewGuid().ToString('N')); New-Item -ItemType Directory -Path $work | Out-Null
$installer=New-Object -ComObject WindowsInstaller.Installer
$db=$installer.OpenDatabase($Output,3)
function Sql([string]$query) { $view=$db.OpenView($query); $view.Execute(); $view.Close() }
function Set-ComProperty($object,[string]$name,[object[]]$arguments) { $object.GetType().InvokeMember($name,[Reflection.BindingFlags]::SetProperty,$null,$object,$arguments) | Out-Null }
function Row([string]$table,[string[]]$columns,[object[]]$values) {
 $fields=($columns | ForEach-Object { '`'+$_+'`' }) -join ','; $marks=($values | ForEach-Object {'?'}) -join ','
 $view=$db.OpenView('INSERT INTO `'+$table+'` ('+$fields+') VALUES ('+$marks+')'); $record=$installer.CreateRecord($values.Count)
 for($i=0;$i -lt $values.Count;$i++) { if($null -ne $values[$i]) { if($values[$i] -is [int]) { Set-ComProperty $record 'IntegerData' @(($i+1),$values[$i]) } else { Set-ComProperty $record 'StringData' @(($i+1),[string]$values[$i]) } } }
 $view.Execute($record); $view.Close()
}
Sql 'CREATE TABLE `Property` (`Property` CHAR(72) NOT NULL, `Value` CHAR(0) LOCALIZABLE PRIMARY KEY `Property`)'
Sql 'CREATE TABLE `Upgrade` (`UpgradeCode` CHAR(38) NOT NULL, `VersionMin` CHAR(20), `VersionMax` CHAR(20), `Language` CHAR(255), `Attributes` LONG NOT NULL, `Remove` CHAR(255), `ActionProperty` CHAR(72) NOT NULL PRIMARY KEY `UpgradeCode`, `VersionMin`, `VersionMax`, `Language`, `Attributes`)'
Sql 'CREATE TABLE `Directory` (`Directory` CHAR(72) NOT NULL, `Directory_Parent` CHAR(72), `DefaultDir` CHAR(255) NOT NULL LOCALIZABLE PRIMARY KEY `Directory`)'
Sql 'CREATE TABLE `Component` (`Component` CHAR(72) NOT NULL, `ComponentId` CHAR(38), `Directory_` CHAR(72) NOT NULL, `Attributes` SHORT NOT NULL, `Condition` CHAR(255), `KeyPath` CHAR(72) PRIMARY KEY `Component`)'
Sql 'CREATE TABLE `Feature` (`Feature` CHAR(38) NOT NULL, `Feature_Parent` CHAR(38), `Title` CHAR(64) LOCALIZABLE, `Description` CHAR(255) LOCALIZABLE, `Display` SHORT, `Level` SHORT NOT NULL, `Directory_` CHAR(72), `Attributes` SHORT NOT NULL PRIMARY KEY `Feature`)'
Sql 'CREATE TABLE `FeatureComponents` (`Feature_` CHAR(38) NOT NULL, `Component_` CHAR(72) NOT NULL PRIMARY KEY `Feature_`, `Component_`)'
Sql 'CREATE TABLE `File` (`File` CHAR(72) NOT NULL, `Component_` CHAR(72) NOT NULL, `FileName` CHAR(255) NOT NULL LOCALIZABLE, `FileSize` LONG NOT NULL, `Version` CHAR(72), `Language` CHAR(20), `Attributes` SHORT, `Sequence` LONG NOT NULL PRIMARY KEY `File`)'
Sql 'CREATE TABLE `Media` (`DiskId` SHORT NOT NULL, `LastSequence` LONG NOT NULL, `DiskPrompt` CHAR(64) LOCALIZABLE, `Cabinet` CHAR(255), `VolumeLabel` CHAR(32), `Source` CHAR(72) PRIMARY KEY `DiskId`)'
Sql 'CREATE TABLE `Shortcut` (`Shortcut` CHAR(72) NOT NULL, `Directory_` CHAR(72) NOT NULL, `Name` CHAR(128) NOT NULL LOCALIZABLE, `Component_` CHAR(72) NOT NULL, `Target` CHAR(255) NOT NULL, `Arguments` CHAR(255), `Description` CHAR(255) LOCALIZABLE, `Hotkey` SHORT, `Icon_` CHAR(72), `IconIndex` SHORT, `ShowCmd` SHORT, `WkDir` CHAR(72) PRIMARY KEY `Shortcut`)'
foreach($table in @('InstallExecuteSequence','InstallUISequence','AdminExecuteSequence')) { Sql ('CREATE TABLE `'+$table+'` (`Action` CHAR(72) NOT NULL, `Condition` CHAR(255), `Sequence` SHORT PRIMARY KEY `Action`)') }
$properties=@{ProductCode='{C1C78592-6880-41A8-8F10-451301D252B0}';UpgradeCode='{A99F48CF-385D-4C76-91F5-56B931DA8E9E}';ProductName='Al Asmai Lab';ProductVersion=$productVersion;ProductLanguage='1033';Manufacturer='Al Asmai Lab';INSTALLLEVEL='1';ARPCOMMENTS='Single-user application. Database is created on first run and retained after uninstall.';ARPNOMODIFY='1';SecureCustomProperties='PREVIOUSPRODUCTS'}
foreach($key in $properties.Keys) { Row Property @('Property','Value') @($key,$properties[$key]) }
Row Upgrade @('UpgradeCode','VersionMax','Attributes','Remove','ActionProperty') @($properties.UpgradeCode,$productVersion,1,'ALL','PREVIOUSPRODUCTS')
Row Directory @('Directory','Directory_Parent','DefaultDir') @('TARGETDIR',$null,'SourceDir')
Row Directory @('Directory','Directory_Parent','DefaultDir') @('LocalAppDataFolder','TARGETDIR','.')
Row Directory @('Directory','Directory_Parent','DefaultDir') @('UserPrograms','LocalAppDataFolder','Programs')
Row Directory @('Directory','Directory_Parent','DefaultDir') @('INSTALLDIR','UserPrograms','AlAsmaiLab')
Row Directory @('Directory','Directory_Parent','DefaultDir') @('ProgramMenuFolder','TARGETDIR','.')
Row Directory @('Directory','Directory_Parent','DefaultDir') @('DesktopFolder','TARGETDIR','.')
Row Feature @('Feature','Title','Level','Directory_','Attributes') @('Main','Laboratory desktop',1,'INSTALLDIR',0)
$directories=@{''='INSTALLDIR'}; $directoryIndex=0
foreach($dir in (Get-ChildItem -LiteralPath $Source -Directory -Recurse | Sort-Object {$_.FullName.Length})) {
 $relative=[IO.Path]::GetRelativePath($Source,$dir.FullName); $parent=[IO.Path]::GetDirectoryName($relative); $directoryIndex++; $id='D'+$directoryIndex
 Row Directory @('Directory','Directory_Parent','DefaultDir') @($id,$directories[$parent],$dir.Name); $directories[$relative]=$id
}
$files=@(Get-ChildItem -LiteralPath $Source -File -Recurse | Where-Object { $_.Name -notin @('setup.inf','setup.rpt') } | Sort-Object FullName); $sequence=0; $exeId=''; $exeComponent=''
$ddf=[Collections.Generic.List[string]]::new(); foreach($line in @('.OPTION EXPLICIT','.Set Cabinet=on','.Set Compress=on','.Set CompressionType=MSZIP','.Set MaxDiskSize=0','.Set CabinetNameTemplate=lab.cab',('.Set DiskDirectoryTemplate="'+$work+'"'))) { $ddf.Add($line) }
$ddf.Add('.Set InfFileName="'+(Join-Path $work 'setup.inf')+'"'); $ddf.Add('.Set RptFileName="'+(Join-Path $work 'setup.rpt')+'"')
foreach($file in $files) {
 $sequence++; $id='F'+$sequence.ToString('D5'); $component='C'+$sequence.ToString('D5'); $relative=[IO.Path]::GetRelativePath($Source,$file.FullName); $parent=[IO.Path]::GetDirectoryName($relative)
 $hash=[Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes('AlAsmaiLab/'+$relative.ToLowerInvariant())); $guid=[Guid]::new([byte[]]$hash[0..15]).ToString('B').ToUpperInvariant()
 Row Component @('Component','ComponentId','Directory_','Attributes','KeyPath') @($component,$guid,$directories[$parent],256,$id)
 Row FeatureComponents @('Feature_','Component_') @('Main',$component)
 Row File @('File','Component_','FileName','FileSize','Attributes','Sequence') @($id,$component,$file.Name,[int]$file.Length,16384,$sequence)
 $ddf.Add('"'+$relative+'" '+$id)
 if($relative -eq 'MedicalLab.exe') { $exeId=$id; $exeComponent=$component }
}
$ddfPath=Join-Path $work 'files.ddf'; [IO.File]::WriteAllLines($ddfPath,$ddf,[Text.Encoding]::Default)
Push-Location $Source; try { & makecab.exe /F $ddfPath | Out-Null; if($LASTEXITCODE -ne 0) { throw 'makecab failed' } } finally { Pop-Location }
Row Media @('DiskId','LastSequence','Cabinet') @(1,$sequence,'#lab.cab')
$view=$db.OpenView('INSERT INTO `_Streams` (`Name`,`Data`) VALUES (?,?)'); $record=$installer.CreateRecord(2); Set-ComProperty $record 'StringData' @(1,'lab.cab'); $record.SetStream(2,(Join-Path $work 'lab.cab')); $view.Execute($record); $view.Close()
foreach($shortcut in @(@('StartShortcut','ProgramMenuFolder'),@('DesktopShortcut','DesktopFolder'))) { Row Shortcut @('Shortcut','Directory_','Name','Component_','Target','Description','ShowCmd','WkDir') @($shortcut[0],$shortcut[1],'Al Asmai Lab',$exeComponent,('[#'+$exeId+']'),'Open laboratory application',1,'INSTALLDIR') }
$actions=@{FindRelatedProducts=25;ValidateProductID=700;CostInitialize=800;FileCost=900;CostFinalize=1000;MigrateFeatureStates=1200;InstallValidate=1400;InstallInitialize=1500;RemoveExistingProducts=1510;ProcessComponents=1600;UnpublishFeatures=1800;RemoveShortcuts=3200;RemoveFiles=3500;InstallFiles=4000;CreateShortcuts=4500;RegisterUser=6000;RegisterProduct=6100;PublishFeatures=6300;PublishProduct=6400;InstallFinalize=6600}
foreach($action in $actions.Keys) { Row InstallExecuteSequence @('Action','Sequence') @($action,[int]$actions[$action]) }
foreach($action in @('CostInitialize','FileCost','CostFinalize')) { Row InstallUISequence @('Action','Sequence') @($action,[int]$actions[$action]) }
Row InstallUISequence @('Action','Sequence') @('FindRelatedProducts',25)
Row InstallUISequence @('Action','Sequence') @('ExecuteAction',1300)
$admin=@{CostInitialize=800;FileCost=900;CostFinalize=1000;InstallValidate=1400;InstallInitialize=1500;InstallAdminPackage=3900;InstallFiles=4000;InstallFinalize=6600}
foreach($action in $admin.Keys) { Row AdminExecuteSequence @('Action','Sequence') @($action,[int]$admin[$action]) }
$summary=$db.SummaryInformation(20)
foreach($pair in @(@(1,1252),@(2,'Installation Database'),@(3,'Al Asmai Lab'),@(4,'Al Asmai Lab'),@(7,'x64;1033'),@(9,[Guid]::NewGuid().ToString('B').ToUpperInvariant()),@(14,200),@(15,2),@(18,'Al Asmai Lab build script'),@(19,2))) { Set-ComProperty $summary 'Property' $pair }
$summary.Persist(); $db.Commit()
[Runtime.InteropServices.Marshal]::FinalReleaseComObject($summary) | Out-Null
[Runtime.InteropServices.Marshal]::FinalReleaseComObject($db) | Out-Null
[Runtime.InteropServices.Marshal]::FinalReleaseComObject($installer) | Out-Null
Write-Output "Created $Output ($sequence files)"
