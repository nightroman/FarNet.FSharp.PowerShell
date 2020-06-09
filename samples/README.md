Scripts in this directory are ready to run by `FarNet.FSharpFar` or its `fsx.exe`.
The configuration file [.fs.ini](.fs.ini) provides the required references.

For running by `fsi.exe` scripts need manually added `#r` directives like

```fsharp
#r "System.Management.Automation"
#r @"C:\Bin\Far\x64\FarNet\Lib\FarNet.FSharp.PowerShell\FarNet.FSharp.PowerShell.dll"
```
