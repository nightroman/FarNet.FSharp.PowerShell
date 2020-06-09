
task samples {
	(Get-Item ..\samples\*.fsx) | .{process{
		Write-Build Cyan $_
		exec {fsx $_}
	}}
}

task missingCommand1 {
	($r = fsx test.fsx missingCommand1)
	equals $LASTEXITCODE 1
	equals $r $null
}

task missingCommand2 {
	($r = exec {fsx test.fsx missingCommand2})
	assert ($r -like "The term 'missing'*. At line:1 char:1*")
}

task convert {
	($r = exec {fsx test.fsx convert})
	equals $r '[|1; 3; 42; 0; 0|]'
}
