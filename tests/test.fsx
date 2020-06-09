open FarNet.FSharp.PowerShell

let ps = PS.Create()

match Array.last fsi.CommandLineArgs with

| "missingCommand1" ->
    ps.Script("missing").Invoke() |> ignore

| "missingCommand2" ->
    try
        ps.Script("missing").Invoke() |> ignore
    with exn ->
        printfn "%s" exn.Message

| x ->
    failwithf "Missing test '%s'." x
