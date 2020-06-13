
# generate tasks for fsx files in /samples
Get-Item ../samples/*.fsx | .{process{
	Add-BuildTask -Name:$_.Name -Data:$_ -Jobs:{
		exec { fsx.exe $Task.Data }
	}
}}

task missingCommand {
	($r = fsx.exe test.fsx missingCommand)
	equals $LASTEXITCODE 1
	equals $r $null
}
