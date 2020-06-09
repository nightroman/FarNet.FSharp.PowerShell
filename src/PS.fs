module FarNet.FSharp.PowerShell
open System
open System.Collections
open System.Collections.Generic
open System.Management.Automation
open System.Reflection

/// Gets PSObject property value or null.
/// It works for Hashtable values as well.
// Why not Option? It is not secure anyway, we still have to check the type.
// So we provide the ultimate shortcut for scripting style programming.
// This is also consistent with PowerShell popular Hashtable API.
// Use PSObject members for full control.
let (?) (it: PSObject) (name: string) =
    match it.Properties.[name] with
    | null ->
        match it.BaseObject with
        | :? Hashtable as x ->
            x.[name]
        | _ ->
            null
    | x -> x.Value

// Init the session, set PS, Get-Type.
let private scriptInit = """
$ErrorActionPreference = 'Stop'
$PS = $args[0]
function Get-Type([Parameter()]$Name) {
    trap {$PSCmdlet.ThrowTerminatingError($_)}
    $PS.FindType($Name)
}
"""

let private exnWithInfo (exn: RuntimeException) =
    if isNull exn.ErrorRecord.InvocationInfo then
        exn :> exn
    else
        Exception(sprintf "%s %s" exn.Message exn.ErrorRecord.InvocationInfo.PositionMessage, exn)

/// F# friendly wrapper of System.Management.Automation.PowerShell.
/// The usage and members are similar.
[<Sealed>]
type PS private (ps, types) as this =
    interface IDisposable with
        member _.Dispose() =
            ps.Dispose()

    /// Creates a new wrapped PowerShell instance.
    static member Create() =
        let caller = Assembly.GetCallingAssembly()
        let types = lazy (caller.GetTypes())

        let ps = PowerShell.Create()
        let this = new PS(ps, types)

        ps.AddScript(scriptInit).AddArgument(this).Invoke() |> ignore
        this

    /// INTERNAL.
    member _.FindType(name) =
        let types = types.Value
        match  types |> Array.tryFindBack (fun x -> x.Name = name) with
        | Some ty ->
            ty
        | None ->
            failwithf "Cannot find type '%s'." name

    /// Gets the wrapped PowerShell instance.
    member _.PowerShell = ps

    /// Sets the script to be invoked by one of the Invoke*() methods.
    member _.Script(script) =
        ps.Commands.Clear()
        ps.AddScript(script) |> ignore
        this

    /// Sets the command to be invoked by one of the Invoke*() methods.
    member _.Command(command: string) =
        ps.Commands.Clear()
        ps.AddCommand(command) |> ignore
        this

    /// Adds the specified argument to the current command.
    member _.AddArgument(argument) =
        ps.AddArgument(argument) |> ignore
        this

    /// Adds the specified arguments to the current command.
    member _.AddArguments(arguments) =
        for v in arguments do
            ps.AddArgument(v) |> ignore
        this

    /// Adds the parameter name and value to current command.
    member _.AddParameter(name, value) =
        ps.AddParameter(name, value) |> ignore
        this

    /// Invokes the current command and returns a PSObject collection.
    member _.Invoke() =
        ps.Streams.ClearStreams()
        try
            ps.Invoke()
        with
        | :? RuntimeException as exn ->
            exn |> exnWithInfo |> raise

    /// Invokes the current command and returns a typed array
    /// converted by PowerShell LanguagePrimitives.Convert().
    member _.Invoke2<'t>() : 't[] =
        ps.Streams.ClearStreams()
        try
            let source = ps.Invoke()
            PS.Convert<'t>(source)
        with
        | :? RuntimeException as exn ->
            exn |> exnWithInfo |> raise

    /// Creates the current command async expression returning a PSObject collection.
    member _.InvokeAsync() = async {
        ps.Streams.ClearStreams()
        try
            let asyncResult = ps.BeginInvoke()
            let! _ok = Async.AwaitIAsyncResult(asyncResult)
            return ps.EndInvoke asyncResult
        with
        | :? RuntimeException as exn ->
            return exn |> exnWithInfo |> raise
    }

    /// Creates the current command async expression returning a typed array
    /// converted by PowerShell LanguagePrimitives.Convert().
    member _.InvokeAsync2<'t>() = async {
        ps.Streams.ClearStreams()
        try
            let asyncResult = ps.BeginInvoke()
            let! _ok = Async.AwaitIAsyncResult(asyncResult)
            let source = ps.EndInvoke asyncResult
            return PS.Convert<'t>(source)
        with
        | :? RuntimeException as exn ->
            return exn |> exnWithInfo |> raise
    }

    static member private Convert<'t>(source: IList<PSObject>) : 't[] =
        let res = Array.zeroCreate source.Count
        for i in 0 .. res.Length - 1 do
            let x = source.[i]
            res.[i] <-
                if isNull x then
                    LanguagePrimitives.ConvertTo<'t>(null)
                else
                    match x.BaseObject with
                    | :? 't as x ->
                        x
                    | _ ->
                        LanguagePrimitives.ConvertTo<'t>(x)
        res
