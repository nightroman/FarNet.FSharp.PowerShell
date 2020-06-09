// Access the wrapped PowerShell for its Streams.

open FarNet.FSharp.PowerShell

// PS session.
let ps = PS.Create()

// Script with warnings and verbose messages.
ps.Script("""
$VerbosePreference = 'Continue'
Write-Warning "warning 1"
Write-Verbose "verbose 1"
Write-Warning "warning 2"
Write-Verbose "verbose 2"
""").Invoke() |> ignore

// Get warnings.
ps.PowerShell.Streams.Warning.ReadAll()
|> printfn "%A"

// Get verbose messages.
ps.PowerShell.Streams.Verbose.ReadAll()
|> printfn "%A"
