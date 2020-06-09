[NuGet]: https://www.nuget.org/packages/FarNet.FSharp.PowerShell
[GitHub]: https://github.com/nightroman/FarNet.FSharp.PowerShell
[/samples]: https://github.com/nightroman/FarNet.FSharp.PowerShell/tree/master/samples
[PowerShell]: https://docs.microsoft.com/en-us/dotnet/api/system.management.automation.powershell?view=powershellsdk-1.1.0

# FarNet.FSharp.PowerShell

F# friendly PowerShell extension \
(net45 and Windows PowerShell)

## Package

The NuGet package [FarNet.FSharp.PowerShell][NuGet] may be used as usual in F# projects.
Note, *System.Management.Automation.dll* is not needed in your final binaries.
It is just for building.

The package is also designed for [FarNet.FSharpFar](https://github.com/nightroman/FarNet/tree/master/FSharpFar).
To install FarNet packages, follow [these steps](https://raw.githubusercontent.com/nightroman/FarNet/master/Install-FarNet.en.txt).

## Overview

**F# code**

`PS` is the F# friendly wrapper of the [PowerShell] class, with similar but fewer members.
Use `PS.Create()` instead of `PowerShell.Create()`.

Use `Script()` and `Command()` instead of `AddScript` and `AddCommand()`.
`PS` does not directly support command chains. But it is fine to invoke
several scripts and commands one after another using same `PS` object.

Use the type safe helper `Invoke2()` in addition to `Invoke()`.
All result objects must be compatible with the specified type.

Use F# asynchronous `InvokeAsync()` and `InvokeAsync2()`.
For parallel scenarios use different `PS` instances.

Use the operator `?` for getting `PSObject` properties.
Note, it also works for `Hashtable` wrapped by `PSObject`.

**PowerShell code**

The default `$ErrorActionPreference` is `Stop`, safe for non-interactive.

Use `Get-Type` to get a type defined in the calling F# assembly.

## Example

"Hello, world!" example:

```fsharp
open FarNet.FSharp.PowerShell

PS.Create().Script("Write-Output 'Hello, world!'").Invoke()
|> printfn "%A"
```

For more examples see [/samples].

## Notes

Features and API may change without notice before v1.0.

Right now the project is not suitable for cloning and hacking.
It is originally developed strictly for Far Manager with FSharpFar.
So the project file and build tools assume the special environment.

But it turns out that the package is a generic F# library.
It makes sense to turn the project into generic as well.
FarNet stuff will be on top of it and optional.
