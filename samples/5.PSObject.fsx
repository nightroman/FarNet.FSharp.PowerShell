// Access result PSObject "properties".

open FarNet.FSharp.PowerShell

// Script gets 4 different type objects with "properties" Name and Version:
let script = """
# .NET type
$Host

# Hashtable
@{Name = 'Hashtable'; Version = '1.0'}

# PowerShell class
class FakeHost {$Name; $Version}
[FakeHost]@{Name = 'PS class'; Version = '2.0'}

# PowerShell custom object
[PSCustomObject]@{Name = 'PSCustomObject'; Version = '3.0'}
"""

// In F# these different objects are consumed using the same API.
// In fact, one Version is System.Version and others are strings.
// In this example, this does not matter.
PS.Create().Script(script).Invoke()
|> Seq.iter (fun x ->
    printfn "Name = '%O'; Version = %O; Type = %s" x?Name x?Version (x.BaseObject.GetType().Name)
)
