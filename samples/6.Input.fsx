// Invoke with input data (in addition to arguments or parameters).

open FarNet.FSharp.PowerShell
open System.IO

// One PS session for several commands.
let ps = PS.Create()

// Invoke some script with input.
// Note, InvokeAs omits the generic type. F# infers it (from %f in printfn).

let script = """
param($Factor)
process {
    $_ * $Factor
}
"""

[1..4]
|> ps.Script(script).AddArgument(3.14).InvokeAs
|> Seq.iter (printfn "%f")

// Invoke some command with input.
// Note, in this case InvokeAs specifies the generic type.

[
    __SOURCE_DIRECTORY__
    __SOURCE_DIRECTORY__ + "/" + __SOURCE_FILE__
]
|> ps.Command("Get-Item").InvokeAs<FileSystemInfo>
|> Seq.iter (fun x -> printfn "%s ~ LastWriteTime=%O" x.Name x.LastWriteTime)
