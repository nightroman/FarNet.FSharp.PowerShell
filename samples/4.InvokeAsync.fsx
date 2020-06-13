// Invoke PowerShell asynchronously, e.g. for running parallel jobs.

open FarNet.FSharp.PowerShell
open System.Diagnostics

let jobSeconds = 1

// make two ready to run jobs
let ps1 = PS.Create().Script("Start-Sleep $args[0]; 1").AddArgument(jobSeconds)
let ps2 = PS.Create().Script("Start-Sleep $args[0]; 2").AddArgument(jobSeconds)

// run two jobs sequentially, it takes ~ 2 * jobSeconds
do
    printfn "Test 1..."
    let time = Stopwatch.StartNew()
    // do 1 here
    let r1 = ps1.InvokeAs<int>()
    // do 2 here
    let r2 = ps2.InvokeAs<int>()
    printfn "time: %O %A %A" time.Elapsed r1 r2

// run one job parallel with another, it takes ~ jobSeconds
do
    printfn "Test 2..."
    let time = Stopwatch.StartNew()
    let r1, r2 =
        async {
            // do 1 in parallel
            let! complete1 = ps1.InvokeAsyncAs<int>() |> Async.StartChild
            // do 2 here
            let r2 = ps2.InvokeAs<int>()
            // complete job 1
            let! r1 = complete1
            // results
            return r1, r2
        }
        |> Async.RunSynchronously
    printfn "time: %O %A %A" time.Elapsed r1 r2
