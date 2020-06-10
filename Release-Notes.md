# FarNet.FSharp.PowerShell Release Notes

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
