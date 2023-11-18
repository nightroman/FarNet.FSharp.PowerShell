[NuGet]: https://www.nuget.org/packages/FarNet.FSharp.PowerShell
[GitHub]: https://github.com/nightroman/FarNet.FSharp.PowerShell
[FarNet.FSharpFar]: https://github.com/nightroman/FarNet/tree/main/FSharpFar#readme
[FarNet.PowerShellFar]: https://github.com/nightroman/FarNet/tree/main/PowerShellFar#readme
[PowerShell]: https://learn.microsoft.com/en-us/dotnet/api/system.management.automation.powershell?view=powershellsdk-7.4.0

# FarNet.FSharp.PowerShell

F# friendly PowerShell Core helper

## Package (two in one)

(1)
The NuGet package [FarNet.FSharp.PowerShell][NuGet] may be used as usual in F# projects.
In this case PowerShell Core comes with its dependency `Microsoft.PowerShell.SDK`
(batteries included because Windows PowerShell cannot be used).

(2)
The package is also used by [FarNet.FSharpFar] in Far Manager and its satellite `fsx.exe`.
In this case PowerShell Core comes with another package [FarNet.PowerShellFar].
To install FarNet packages, follow [these steps](https://github.com/nightroman/FarNet#readme).
Thus, you need these packages in Far Manager:

1. `FarNet` - Far Manager .NET host and API for modules
2. `FarNet.FSharpFar` - F# compiler service host and tools
3. `FarNet.PowerShellFar` - PowerShell Core, host and tools
4. `FarNet.FSharp.PowerShell` - this package connects 2. and 3. in 1.

It looks like a lot but includes all the batteries and the power plant.
This setup is portable with Far Manager, with caveats about prerequisites.

## Overview

**F# code**

The `PS` type wraps the [PowerShell] class and exposes somewhat similar members.

Use `PS.Create()` instead of `PowerShell.Create()`.

Use `Script()` and `Command()` instead of `AddScript` and `AddCommand()`.
`PS` does not directly support command chains. But it is fine to invoke
several scripts and commands using the same `PS` instance.

Use the type safe generic `InvokeAs()` in addition to `Invoke()`.
Result objects must be compatible with the specified type.

Use F# asynchronous `InvokeAsync()` and `InvokeAsyncAs()`.
For parallel scenarios use different `PS` instances.

Use the operator `?` for getting `PSObject` properties.
Note, it also works for `Hashtable` wrapped by `PSObject`.

**PowerShell code**

The default `$ErrorActionPreference` is `Stop`, safe for non-interactive.

Use `Get-Type` to get a type defined in the calling F# assembly or script.

## Example

"Hello, world!" example:

```fsharp
open FarNet.FSharp.PowerShell

PS.Create().Script("Write-Output 'Hello, world!'").Invoke()
|> printfn "%A"
```

## See also

- [Samples](https://github.com/nightroman/FarNet.FSharp.PowerShell/tree/main/samples)
- [Release Notes](https://github.com/nightroman/FarNet.FSharp.PowerShell/blob/main/Release-Notes.md)
