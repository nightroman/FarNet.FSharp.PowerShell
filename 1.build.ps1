<#
.Synopsis
	Build script, https://github.com/nightroman/Invoke-Build
#>

param(
	$Configuration = (property Configuration Release),
	$FarHome = (property FarHome C:\Bin\Far\x64)
)

Set-StrictMode -Version 3
$TargetFramework = 'net10.0'
$_name = 'FarNet.FSharp.PowerShell'
$_root = "$FarHome\FarNet\Lib\$_name"
$_description = 'F# friendly PowerShell Core helper.'

task build meta, {
	exec { dotnet build -c $Configuration }
}

task publish {
	$null = mkdir $_root -Force
	Set-Location src
	Copy-Item -Destination $_root @(
		"$_name.ini"
		"bin\$Configuration\$TargetFramework\$_name.dll"
		"bin\$Configuration\$TargetFramework\$_name.xml"
	)
}

task clean {
	remove *\bin, *\obj, README.htm, *.nupkg, z, TestResults
}

task version {
	($Script:_version = Get-BuildVersion Release-Notes.md '##\s+v(\d+\.\d+\.\d+)')
}

task meta -Inputs $BuildFile, Release-Notes.md -Outputs src/Directory.Build.props -Jobs version, {
	Set-Content src/Directory.Build.props @"
<Project>
	<PropertyGroup>
		<Company>https://github.com/nightroman/$_name</Company>
		<Copyright>Copyright (c) Roman Kuzmin</Copyright>
		<Description>$_description</Description>
		<Product>$_name</Product>
		<Version>$_version</Version>
		<FileVersion>$_version</FileVersion>
		<AssemblyVersion>$_version</AssemblyVersion>
	</PropertyGroup>
</Project>
"@
}

task markdown {
	requires -Path $env:MarkdownCss
	exec { pandoc.exe @(
		'README.md'
		'--output=README.htm'
		'--from=gfm'
		'--embed-resources'
		'--standalone'
		"--css=$env:MarkdownCss"
		"--metadata=pagetitle=$_name"
	)}
}

# Synopsis: Collect package files.
task package markdown, {
	remove z
	$toLib = mkdir "z\lib\$TargetFramework"
	$toModule = mkdir "z\tools\FarHome\FarNet\Lib\$_name"

	Copy-Item -Destination z @(
		'README.md'
	)

	Copy-Item -Destination $toLib @(
		"$_root\FarNet.FSharp.PowerShell.dll"
		"$_root\FarNet.FSharp.PowerShell.xml"
	)

	Copy-Item -Destination $toModule @(
		'README.htm'
		'LICENSE'
		"$_root\FarNet.FSharp.PowerShell.dll"
		"$_root\FarNet.FSharp.PowerShell.ini"
		"$_root\FarNet.FSharp.PowerShell.xml"
	)
}

# Synopsis: Make NuGet package.
task nuget package, version, {
	$dllPath = "$FarHome\FarNet\Lib\$_name\$_name.dll"
	($dllVersion = (Get-Item $dllPath).VersionInfo.FileVersion.ToString())
	assert $dllVersion.StartsWith("$_version.") 'Versions mismatch.'

	Set-Content z\Package.nuspec @"
<?xml version="1.0"?>
<package xmlns="http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd">
	<metadata>
		<id>$_name</id>
		<version>$_version</version>
		<authors>Roman Kuzmin</authors>
		<owners>Roman Kuzmin</owners>
		<license type="expression">Apache-2.0</license>
		<readme>README.md</readme>
		<projectUrl>https://github.com/nightroman/$_name</projectUrl>
		<description>$_description</description>
		<releaseNotes>https://github.com/nightroman/FarNet.FSharp.PowerShell/blob/main/Release-Notes.md</releaseNotes>
		<tags>FSharp PowerShell FarManager FarNet FSharpFar</tags>
		<dependencies>
			<group targetFramework="$TargetFramework">
				<dependency id="Microsoft.PowerShell.SDK" version="7.5.4" />
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
