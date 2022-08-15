<#
.Synopsis
	Build script, https://github.com/nightroman/Invoke-Build
#>

param(
	$Configuration = (property Configuration Release),
	$FarHome = (property FarHome C:\Bin\Far\x64)
)

Set-StrictMode -Version 3
$ModuleName = 'FarNet.FSharp.PowerShell'
$ModuleRoot = "$FarHome\FarNet\Lib\$ModuleName"
$Description = 'F# friendly PowerShell Core helper'

task build meta, {
	exec { dotnet build -c $Configuration }
}

task publish {
	Set-Location src
	Copy-Item -Destination $ModuleRoot @(
		"$ModuleName.ini"
		"bin\$Configuration\net6.0\$ModuleName.dll"
		"bin\$Configuration\net6.0\$ModuleName.xml"
	)
}

task clean {
	remove *\bin, *\obj, README.htm, *.nupkg, z
}

task version {
	($script:Version = switch -Regex -File Release-Notes.md {'##\s+v(\d+\.\d+\.\d+)' {$Matches[1]; break}})
}

task meta -Inputs .build.ps1, Release-Notes.md -Outputs src/Directory.Build.props -Jobs version, {
	Set-Content src/Directory.Build.props @"
<Project>
	<PropertyGroup>
		<Company>https://github.com/nightroman/$ModuleName</Company>
		<Copyright>Copyright (c) Roman Kuzmin</Copyright>
		<Description>$Description</Description>
		<Product>$ModuleName</Product>
		<Version>$Version</Version>
		<FileVersion>$Version</FileVersion>
		<AssemblyVersion>$Version</AssemblyVersion>
	</PropertyGroup>
</Project>
"@
}

task markdown {
	assert (Test-Path $env:MarkdownCss)
	exec { pandoc.exe @(
		'README.md'
		'--output=README.htm'
		'--from=gfm'
		'--self-contained', "--css=$env:MarkdownCss"
		'--standalone', "--metadata=pagetitle=$ModuleName"
	)}
}

# Synopsis: Collect package files.
task package markdown, {
	remove z
	$toLib = mkdir "z\lib\net6.0"
	$toModule = mkdir "z\tools\FarHome\FarNet\Lib\$ModuleName"

	Copy-Item -Destination $toLib @(
		"$ModuleRoot\FarNet.FSharp.PowerShell.dll"
		"$ModuleRoot\FarNet.FSharp.PowerShell.xml"
	)

	Copy-Item -Destination $toModule @(
		'README.htm'
		'LICENSE'
		"$ModuleRoot\FarNet.FSharp.PowerShell.dll"
		"$ModuleRoot\FarNet.FSharp.PowerShell.ini"
		"$ModuleRoot\FarNet.FSharp.PowerShell.xml"
	)
}

# Synopsis: Make NuGet package.
task nuget package, version, {
	# test versions
	$dllPath = "$FarHome\FarNet\Lib\$ModuleName\$ModuleName.dll"
	($dllVersion = (Get-Item $dllPath).VersionInfo.FileVersion.ToString())
	assert $dllVersion.StartsWith("$Version.") 'Versions mismatch.'

	$Description = @"
$Description

---

The package is designed for FarNet.FSharpFar and requires FarNet.PowerShellFar.
To install FarNet packages, follow these steps:

https://github.com/nightroman/FarNet#readme

---
"@

	Set-Content z\Package.nuspec @"
<?xml version="1.0"?>
<package xmlns="http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd">
	<metadata>
		<id>$ModuleName</id>
		<version>$Version</version>
		<authors>Roman Kuzmin</authors>
		<owners>Roman Kuzmin</owners>
		<projectUrl>https://github.com/nightroman/$ModuleName</projectUrl>
		<license type="expression">Apache-2.0</license>
		<description>$Description</description>
		<releaseNotes>https://github.com/nightroman/FarNet.FSharp.PowerShell/blob/main/Release-Notes.md</releaseNotes>
		<tags>FSharp PowerShell FarManager FarNet FSharpFar</tags>
		<dependencies>
			<group targetFramework="net6.0">
				<dependency id="Microsoft.PowerShell.SDK" version="7.2.6" />
			</group>
		</dependencies>
	</metadata>
</package>
"@

	exec { NuGet.exe pack z\Package.nuspec }
}

# Synopsis: xUnit. Idle if the main project is clean.
task test1 {
	Set-Location tests
	exec { dotnet test -c $Configuration -l "console;verbosity=normal" --no-build }
}

# Synopsis: fsx tests.
task test2 {
	Invoke-Build ** tests
}

# Synopsis: All tests.
task test test1, test2

# Synopsis: Update and test.
task all build, test, clean

# Synopsis: Update.
task . build, clean
