// Why not Invoke() and Invoke<'t> like in PowerShell?
// Because such a pair makes F# inference problematic.

// fake PSObject
type PSObject (value: obj) =
    member __.BaseObject = value

// Invoke() and InvokeAs<'t>() ~ FarNet.FSharp.PowerShell.PS
type PS (value: obj) =
    member __.Invoke() = PSObject(value)
    member __.InvokeAs<'t>() = value :?> 't

// Invoke() and Invoke<'t>() ~ System.Management.Automation.PowerShell
type PowerShell (value: obj) =
    member __.Invoke() = PSObject(value)
    member __.Invoke<'t>() = value :?> 't

do // everything is fine with Invoke and InvokeAs<'t>
    let ps = PS("PS")

    // PSObject
    let r1 = ps.Invoke()
    printfn "%s" (r1.BaseObject :?> string)

    // string, explicitly specified
    let r2 = ps.InvokeAs<string>()
    printfn "%s" r2

    // string, F# inference works
    let r3 = ps.InvokeAs()
    printfn "%s" r3

do // F# type inference issue explained, see comments
    let ps = PowerShell("PowerShell")

    // PSObject
    let r1 = ps.Invoke()
    printfn "%s" (r1.BaseObject :?> string)

    // string, explicitly specified
    let r2 = ps.Invoke<string>()
    printfn "%s" r2

    // string, but it does not compile, try to uncomment
    let r3 = ps.Invoke()
    //printfn "%s" r3
    ()
