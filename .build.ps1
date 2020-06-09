<#
.Synopsis
	Build script (https://github.com/nightroman/Invoke-Build)
#>

param(
	$Configuration = (property Configuration Release)
)

Set-StrictMode -Version 2
$ModuleName = 'FarNet.FSharp.PowerShell'
$env:FarDevHome = $FarDevHome = if (Test-Path 'C:\Bin\Far\x64') {'C:\Bin\Far\x64'} else {''}

# Synopsis: Remove temp files.
task Clean {
	remove *\bin, *\obj, README.htm, *.nupkg, z
}

# Synopsis: Build and Post (post build target).
task Build {
	exec {dotnet build -c $Configuration}
}

# Synopsis: Post build target. Copy stuff.
task Post -If:$FarDevHome {
	$to = "$FarDevHome\FarNet\Lib\$ModuleName"
	Copy-Item "src\$ModuleName.ini" $to
}

# Get version from release notes.
function Get-Version {
	switch -Regex -File Release-Notes.md {'##\s+v(\d+\.\d+\.\d+)' {return $Matches[1]} }
}

# Synopsis: Set $script:Version.
task Version {
	($script:Version = Get-Version)
}

# Synopsis: Convert markdown to HTML.
task Markdown {
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
task Meta -Inputs .build.ps1, Release-Notes.md -Outputs src/Directory.Build.props -Jobs Version, {
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
task Package -If:$FarDevHome Markdown, {
	$toLib = "z\lib\net45"
	$toModule = "z\tools\FarHome\FarNet\Lib\$ModuleName"
	$fromModule = "$FarDevHome\FarNet\Lib\$ModuleName"

	remove z
	$null = mkdir $toModule
	$null = mkdir $toLib

	Copy-Item -Destination $toLib @(
		"$fromModule\FarNet.FSharp.PowerShell.dll"
		"$fromModule\FarNet.FSharp.PowerShell.xml"
	)

	Copy-Item -Destination $toModule @(
		'README.htm'
		'LICENSE.txt'
		"$fromModule\FarNet.FSharp.PowerShell.dll"
		"$fromModule\FarNet.FSharp.PowerShell.ini"
		"$fromModule\FarNet.FSharp.PowerShell.xml"
	)
}

# Synopsis: Make NuGet package.
task NuGet -If:$FarDevHome Package, Version, {
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

https://raw.githubusercontent.com/nightroman/FarNet/master/Install-FarNet.en.txt

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
			<dependency id="Microsoft.PowerShell.3.ReferenceAssemblies" version="1.0.0" />
		</dependencies>
	</metadata>
</package>
"@
	# pack
	exec { NuGet pack z\Package.nuspec -NoPackageAnalysis }
}

# Synopsis: xUnit.
task Test1 {
	Set-Location tests
	exec { dotnet test --blame --no-restore --no-build -c $Configuration -r $env:TEMP }
}

# Synopsis: fsx.
task Test2 -If:$FarDevHome {
	Invoke-Build ** tests
}

# Synopsis: All tests.
task Test Test1, Test2

task . Build, Test, Clean
