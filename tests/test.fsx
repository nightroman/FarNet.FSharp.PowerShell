open FarNet.FSharp.PowerShell

let ps = PS.Create()

match Array.last fsi.CommandLineArgs with

| "missingCommand" ->
    ps.Script("missing").Invoke() |> ignore

| x ->
    failwithf "Missing test '%s'." x
