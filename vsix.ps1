function Vsix-ChangeBuildNumber {
	$manifestFilePath = Resolve-Path ".\VS-QuickNavigation\source.extension.vsixmanifest"
	$assemblyFilePath = Resolve-Path ".\VS-QuickNavigation\Properties\AssemblyInfo.cs"
	$buildNumber = [int]$env:APPVEYOR_BUILD_VERSION

	# Updating manifest build version
	"Updating manifest build version..." | Write-Host

	$manifestContent = Get-Content $manifestFilePath
	$manifestXml = [xml]$manifestContent

	[Version]$version = $manifestXml.PackageManifest.Metadata.Identity.Version
	$version = New-Object Version ([int]$version.Major),([int]$version.Minor),$buildNumber
	$manifestXml.PackageManifest.Metadata.Identity.Version = [string]$version
	"New version is " + $manifestXml.PackageManifest.Metadata.Identity.Version | Write-Host

	#$manifestXml.Save([System.Console]::Out)
	$manifestXML.Save($manifestFilePath)

	# Updating assembly build version
	"Updating assembly build version..." | Write-Host

	$assemblyContent = Get-Content $assemblyFilePath
	$assemblyContent = $assemblyContent -replace "public const string Version = ""(\d.\d)""", "public const string Version = ""$version""";
	Set-Content $assemblyFilePath $assemblyContent

	if (Get-Command Update-AppveyorBuild -errorAction SilentlyContinue)
	{
		"Update AppVeyor version..." | Write-Host
		Update-AppveyorBuild -Version $version | Out-Null
	}

	"Version changed" | Write-Host
}