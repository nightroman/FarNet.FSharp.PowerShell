[NuGet]: https://www.nuget.org/packages/FarNet.FSharp.PowerShell
[GitHub]: https://github.com/nightroman/FarNet.FSharp.PowerShell
[/samples]: https://github.com/nightroman/FarNet.FSharp.PowerShell/tree/master/samples

# FarNet.FSharp.PowerShell

F# friendly PowerShell extension \
(net45 and Windows PowerShell 2+)

## Package

The NuGet package [FarNet.FSharp.PowerShell][NuGet] may be used as usual in F# projects.

The package is also designed for *FarNet.FSharpFar*.
To install FarNet packages, follow [these steps](https://raw.githubusercontent.com/nightroman/FarNet/master/Install-FarNet.en.txt).

## Overview

**F# code**

The type `PS` is the F# friendly wrapper of `PowerShell` with similar but fewer methods.
Use `PS.Create()` instead of `PowerShell.Create()`.

Use `Script()` and `Command()` instead of `AddScript` and `AddCommand()`.
`PS` does not directly support multiple added commands.

Use the type safe helper `Invoke2()` in addition to `Invoke()`.

Use F# asynchronous `InvokeAsync()` and `InvokeAsync2()`.

Use the operator `?` for getting `PSObject` properties.
Note, it also works for `Hashtable` wrapped by `PSObject`.

**PowerShell code**

The default error action preference is `Stop`, safe for non-interactive use.

Use the function `Get-Type` to get F# types from the calling F# code.

## Example

"Hello, world!" example:

```fsharp
open FarNet.FSharp.PowerShell

PS.Create().Script("Write-Output 'Hello, world!'").Invoke()
|> printfn "%A"
```

For more examples see [/samples].
