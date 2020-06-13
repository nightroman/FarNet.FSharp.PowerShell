[NuGet]: https://www.nuget.org/packages/FarNet.FSharp.PowerShell
[GitHub]: https://github.com/nightroman/FarNet.FSharp.PowerShell
[/samples]: https://github.com/nightroman/FarNet.FSharp.PowerShell/tree/master/samples
[PowerShell]: https://docs.microsoft.com/en-us/dotnet/api/system.management.automation.powershell?view=powershellsdk-1.1.0

# FarNet.FSharp.PowerShell

F# friendly PowerShell extension \
(net45 and Windows PowerShell)

## Package

The NuGet package [FarNet.FSharp.PowerShell][NuGet] may be used as usual in F# projects. \
Note, *System.Management.Automation.dll* is not needed in your final binaries.

The package is also designed for [FarNet.FSharpFar](https://github.com/nightroman/FarNet/tree/master/FSharpFar). \
To install FarNet packages, follow [these steps](https://raw.githubusercontent.com/nightroman/FarNet/master/Install-FarNet.en.txt).

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

For more examples see [/samples].

## Notes

Features and API may change before v1.0.

The project is suitable for cloning and playing with Visual Studio. \
*FarNet.FSharp.PowerShell.sln* contains the main project and tests.

*FSharpFar* development and tools are optional.
They require:

- *Far Manager* in `C:\Bin\Far\x64`
- FarNet module *FarNet.FSharpFar*
- *Invoke-Build* with *.build.ps1*
