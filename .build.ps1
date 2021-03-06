<#
.Synopsis
	Build script, https://github.com/nightroman/Invoke-Build
#>

param(
	$Configuration = (property Configuration Release)
)

Set-StrictMode -Version 2
$ModuleName = 'FarNet.FSharp.PowerShell'
$env:FarDevHome = $FarDevHome = if (Test-Path 'C:\Bin\Far\x64') {'C:\Bin\Far\x64'} else {''}

# Synopsis: Remove temp files.
task clean {
	remove *\bin, *\obj, README.htm, *.nupkg, z
}

# Synopsis: Build and Post (post build target).
task build {
	exec {dotnet build -c $Configuration}
}

# Synopsis: Post build target. Copy stuff.
task post -If:$FarDevHome {
	$to = "$FarDevHome\FarNet\Lib\$ModuleName"
	Copy-Item "src\$ModuleName.ini" $to
}

# Get version from release notes.
function Get-Version {
	switch -Regex -File Release-Notes.md {'##\s+v(\d+\.\d+\.\d+)' {return $Matches[1]} }
}

# Synopsis: Set $script:Version.
task version {
	($script:Version = Get-Version)
}

# Synopsis: Convert markdown to HTML.
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

# Synopsis: Generate meta files.
task meta -Inputs .build.ps1, Release-Notes.md -Outputs src/Directory.Build.props -Jobs version, {
	Set-Content src/Directory.Build.props @"
<Project>
	<PropertyGroup>
		<Company>https://github.com/nightroman/$ModuleName</Company>
		<Copyright>Copyright (c) Roman Kuzmin</Copyright>
		<Description>F# friendly PowerShell extension</Description>
		<Product>$ModuleName</Product>
		<Version>$Version</Version>
		<FileVersion>$Version</FileVersion>
		<AssemblyVersion>$Version</AssemblyVersion>
	</PropertyGroup>
</Project>
"@
}

# Synopsis: Collect package files.
task package -If:$FarDevHome markdown, {
	remove z
	$toLib = mkdir "z\lib\net472"
	$toModule = mkdir "z\tools\FarHome\FarNet\Lib\$ModuleName"
	$fromModule = "$FarDevHome\FarNet\Lib\$ModuleName"

	Copy-Item -Destination $toLib @(
		"$fromModule\FarNet.FSharp.PowerShell.dll"
		"$fromModule\FarNet.FSharp.PowerShell.xml"
	)

	Copy-Item -Destination $toModule @(
		'README.htm'
		'LICENSE'
		"$fromModule\FarNet.FSharp.PowerShell.dll"
		"$fromModule\FarNet.FSharp.PowerShell.ini"
		"$fromModule\FarNet.FSharp.PowerShell.xml"
	)
}

# Synopsis: Make NuGet package.
task nuget -If:$FarDevHome package, version, {
	# test versions
	$dllPath = "$FarDevHome\FarNet\Lib\$ModuleName\$ModuleName.dll"
	($dllVersion = (Get-Item $dllPath).VersionInfo.FileVersion.ToString())
	assert $dllVersion.StartsWith("$Version.") 'Versions mismatch.'

	$text = @'
F# friendly PowerShell extension

---

The package may be used as usual in F# projects.

It is also configured for FarNet.FSharpFar.
To install FarNet packages, follow these steps:

https://github.com/nightroman/FarNet#readme

---
'@
	# nuspec
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
		<requireLicenseAcceptance>false</requireLicenseAcceptance>
		<summary>$text</summary>
		<description>$text</description>
		<releaseNotes>https://github.com/nightroman/FarNet.FSharp.PowerShell/blob/master/Release-Notes.md</releaseNotes>
		<tags>FSharp PowerShell FarManager FarNet FSharpFar</tags>
		<dependencies>
			<group targetFramework=".NETFramework4.7.2">
				<dependency id="Microsoft.PowerShell.3.ReferenceAssemblies" version="1.0.0" />
			</group>
		</dependencies>
	</metadata>
</package>
"@
	# pack
	exec { NuGet.exe pack z\Package.nuspec }
}

# Synopsis: xUnit.
task test1 {
	Set-Location tests
	exec { dotnet test --blame --no-restore --no-build -c $Configuration -r $env:TEMP }
}

# Synopsis: fsx.
task test2 -If:$FarDevHome {
	Invoke-Build ** tests
}

# Synopsis: All tests.
task test test1, test2

task . build, test, clean
