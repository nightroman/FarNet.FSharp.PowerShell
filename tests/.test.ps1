
task samples {
	(Get-Item ..\samples\*.fsx) | .{process{
		Write-Build Cyan $_
		exec {fsx $_}
	}}
}

task missingCommand {
	($r = fsx test.fsx missingCommand)
	equals $LASTEXITCODE 1
	equals $r $null
}
