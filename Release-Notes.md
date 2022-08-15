# FarNet.FSharp.PowerShell Release Notes

<!--Mind Microsoft.PowerShell.SDK 7.2.6 in build script.-->

## v0.2.0

Requires .NET 6, uses PowerShell Core 7.2.6.

In Far Manager requires FSharpFar 2.0.5+ and PowerShellFar 6+.

## v0.1.1

Update package info.

## v0.1.0

- Added `Invoke()` and `InvokeAs()` overloads with input.
- Renamed `Invoke2` to `InvokeAs`.
- Renamed `InvokeAsync2` to `InvokeAsyncAs`.

## v0.0.5

- Add framework info to the package.
- Add a direct type use test.

## v0.0.4

- Amend getting F# types from PS scripts.
- Add the test project and tests.
- Make Far development optional.

## v0.0.3

Make getting types lazy, they are only used on `Get-Type`.

`*Invoke2()`: if types do not match, use `LanguagePrimitives.ConvertTo()`.

## v0.0.2

First preview.
