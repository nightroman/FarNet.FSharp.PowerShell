// Run scripts and commands with arguments and parameters.

open FarNet.FSharp.PowerShell
open System.IO

// One PS session for several commands.
let ps = PS.Create()

// Just script
ps.Script("'Hello!'").Invoke()
|> printfn "%A"

// Script with arguments
ps.Script(""" param($Name) "Hello, $Name!" """)
    .AddArgument("Joe")
    .Invoke()
|> printfn "%A"

// Script with parameters
ps.Script(""" param($Name, $Age) "Name=$Name, Age=$Age" """)
    .AddParameter("Name", "Joe")
    .AddParameter("Age", 42)
    .Invoke()
|> printfn "%A"

// Command with Invoke ~> PSObject, untyped properties as x?Name
ps.Command("Get-ChildItem")
    .AddParameter("LiteralPath", __SOURCE_DIRECTORY__)
    .AddParameter("File", true)
    .Invoke()
|> Seq.iter (fun x -> printfn "Name = %O; Length = %O" x?Name x?Length)

// Command with InvokeAs ~> <Type>, typed properties as x.Name
ps.Command("Get-ChildItem")
    .AddParameter("LiteralPath", __SOURCE_DIRECTORY__)
    .AddParameter("File", true)
    .InvokeAs<FileInfo>()
|> Seq.iter (fun x -> printfn "Name = %s; Length = %i" x.Name x.Length)
