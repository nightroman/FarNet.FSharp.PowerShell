module Test1
open FarNet.FSharp.PowerShell
open Xunit
open System
open System.IO
open System.Diagnostics

// Invoke: objects as they are, except $null ~ null.
[<Fact>]
let Invoke () =
    use ps = PS.Create()
    let res = ps.Script("1; 3.14; '42'; ''; $null").Invoke()
    Assert.Equal(5, res.Count)
    Assert.Equal(1, res.[0].BaseObject :?> int)
    Assert.Equal(3.14, res.[1].BaseObject :?> float)
    Assert.Equal("42", res.[2].BaseObject :?> string)
    Assert.Equal("", res.[3].BaseObject :?> string)
    Assert.Equal(null, res.[4])

// InvokeAs: objects are converted to T in the PS way.
// Note, InvokeAs does not always require explicit <T>.
[<Fact>]
let InvokeAs () =
    use ps = PS.Create()
    let res = ps.Script("1; 3.14; '42'; ''; $null").InvokeAs()
    Assert.Equal(5, res.Count)
    Assert.Equal(1, res.[0])
    Assert.Equal(3, res.[1])
    Assert.Equal(42, res.[2])
    Assert.Equal(0, res.[3])
    Assert.Equal(0, res.[4])

// Same as Invoke but async.
[<Fact>]
let InvokeAsync () = async {
    use ps = PS.Create()
    let! res = ps.Script("1; 3.14; '42'; ''; $null").InvokeAsync()
    Assert.Equal(5, res.Count)
    Assert.Equal(1, res.[0].BaseObject :?> int)
    Assert.Equal(3.14, res.[1].BaseObject :?> float)
    Assert.Equal("42", res.[2].BaseObject :?> string)
    Assert.Equal("", res.[3].BaseObject :?> string)
    Assert.Equal(null, res.[4])
}

// Same as InvokeAs but async.
[<Fact>]
let InvokeAsyncAs () = async {
    use ps = PS.Create()
    let! res = ps.Script("1; 3.14; '42'; ''; $null").InvokeAsyncAs()
    Assert.Equal(5, res.Length)
    Assert.Equal(1, res.[0])
    Assert.Equal(3, res.[1])
    Assert.Equal(42, res.[2])
    Assert.Equal(0, res.[3])
    Assert.Equal(0, res.[4])
}

[<Fact>]
let invoke_with_input () = async {
    use ps = PS.Create()

    let res = ps.Script("process{$_ * 2}").Invoke([1..3])
    Assert.Equal(3, res.Count)
    Assert.Equal(2, res.[0].BaseObject :?> int)
    Assert.Equal(4, res.[1].BaseObject :?> int)
    Assert.Equal(6, res.[2].BaseObject :?> int)

    let res = ps.Script("process{$_ * 2}").InvokeAs([1..3])
    Assert.Equal(3, res.Count)
    Assert.Equal(2, res.[0])
    Assert.Equal(4, res.[1])
    Assert.Equal(6, res.[2])
}

[<Fact>]
let error_missing () =
    use ps = PS.Create()
    let exn = Assert.Throws<exn>(fun () -> ps.Script("missing").Invoke() |> ignore)
    Assert.Matches("(?s)^The term 'missing' .* At line:1 char:1", exn.Message)

module Basic =
    let ps = PS.Create()

    [<Fact>]
    let ``Just script`` () =
        let res = Assert.Single(ps.Script("'Hello!'").Invoke())
        Assert.Equal("Hello!", res.BaseObject :?> string)

    [<Fact>]
    let ``Script with arguments`` () =
        let res =
            ps.Script(""" param($Name) "Hello, $Name!" """)
                .AddArgument("Joe")
                .Invoke()
            |> Assert.Single

        Assert.Equal("Hello, Joe!", res.BaseObject :?> string)

    [<Fact>]
    let ``Script with parameters`` () =
        let res =
            ps.Script(""" param($Name, $Age) "Name=$Name, Age=$Age" """)
                .AddParameter("Name", "Joe")
                .AddParameter("Age", 42)
                .Invoke()
            |> Assert.Single

        Assert.Equal("Name=Joe, Age=42", res.BaseObject :?> string)

    // It is fine to run the same command many times.
    [<Fact>]
    let ``Command Invoke InvokeAs`` () =
        let command =
            ps.Command("Get-ChildItem")
                .AddParameter("LiteralPath", __SOURCE_DIRECTORY__)
                .AddParameter("File", true)

        let res = command.Invoke()
        Assert.True(res.Count > 2)
        res
        |> Seq.iter (fun x ->
            Assert.IsType(typeof<string>, x?Name)
            Assert.IsType(typeof<int64>, x?Length)
        )

        let res = command.InvokeAs<FileInfo>()
        Assert.True(res.Count > 2)
        res
        |> Seq.iter (fun x ->
            Assert.IsType(typeof<string>, x.Name)
            Assert.IsType(typeof<int64>, x.Length)
        )

module Types =
    type TypesRecord1 = {Name: string; Age: int}

    [<Fact>]
    let ``1 record constructor`` () =
        use ps = PS.Create()
        let res =
            ps.Script("""
            $type = Get-Type TypesRecord1
            $type::new('Joe', 42)
            """).InvokeAs()
            |> Seq.exactlyOne

        Assert.True({TypesRecord1.Name = "Joe"; Age = 42} = res)

    [<CLIMutable>]
    type TypesRecord2 = {Name: string; Age: int}

    [<Fact>]
    let ``2 default constructor`` () =
        use ps = PS.Create()
        let res =
            ps.Script("""
            $type = Get-Type TypesRecord2
            $x = $type::new()
            $x.Name = 'May'
            $x.Age = 11
            $x
            """).InvokeAs()
            |> Seq.exactlyOne

        Assert.True({TypesRecord2.Name = "May"; Age = 11} = res)

    // Get-Type is needed on calling from F# scripts because type names contain unknown parts.
    // But on calling from F# assembly scripts may use types explicitly with their full names.
    [<Fact>]
    let ``3 explicit type`` () =
        use ps = PS.Create()
        let res =
            ps.Script(""" [Test1+Types+TypesRecord1]::new('May', 11) """).InvokeAs()
            |> Seq.exactlyOne

        Assert.True({TypesRecord1.Name = "May"; Age = 11} = res)

module Streams =
    [<Fact>]
    let ``1 Warning, Verbose`` () =
        use ps = PS.Create()
        let res =
            ps.Script("""
            $VerbosePreference = 'Continue'
            Write-Warning "warning 1"
            Write-Verbose "verbose 1"
            Write-Warning "warning 2"
            Write-Verbose "verbose 2"
            """).Invoke()

        Assert.Equal(0, res.Count)

        let warning = ps.PowerShell.Streams.Warning.ReadAll()
        Assert.Equal(2, warning.Count)
        Assert.Equal("warning 1", warning.[0].Message)
        Assert.Equal("warning 2", warning.[1].Message)

        let verbose = ps.PowerShell.Streams.Verbose.ReadAll()
        Assert.Equal(2, verbose.Count)
        Assert.Equal("verbose 1", verbose.[0].Message)
        Assert.Equal("verbose 2", verbose.[1].Message)

module PSObject =
    [<Fact>]
    let ``1 different types`` () =
        use ps = PS.Create()
        let res =
            ps.Script("""
            # .NET type
            $Host

            # Hashtable
            @{Name = 'Hashtable'; Version = '1.0'}

            # PowerShell class
            class FakeHost {$Name; $Version}
            [FakeHost]@{Name = 'PS class'; Version = '2.0'}

            # PowerShell custom object
            [PSCustomObject]@{Name = 'PSCustomObject'; Version = '3.0'}
            """).Invoke()

        Assert.Equal(4, res.Count)

        let r = res.[0]
        Assert.Equal("Default Host", Assert.IsType<string>(r?Name))
        Assert.IsType(typeof<Version>, r?Version)

        let r = res.[1]
        Assert.Equal("Hashtable", Assert.IsType<string>(r?Name))
        Assert.Equal("1.0", Assert.IsType<string>(r?Version))

        let r = res.[2]
        Assert.Equal("PS class", Assert.IsType<string>(r?Name))
        Assert.Equal("2.0", Assert.IsType<string>(r?Version))

        let r = res.[3]
        Assert.Equal("PSCustomObject", Assert.IsType<string>(r?Name))
        Assert.Equal("3.0", Assert.IsType<string>(r?Version))

module InvokeAsync =
    [<Fact>]
    let ``1 parallel scenario`` () =
        let jobSeconds = 1.0

        // make two ready to run jobs
        use ps1 = PS.Create().Script("Start-Sleep $args[0]; 1").AddArgument(jobSeconds)
        use ps2 = PS.Create().Script("Start-Sleep $args[0]; 2").AddArgument(jobSeconds)

        // run two jobs sequentially, it takes ~ 2 * jobSeconds
        let time = Stopwatch.StartNew()
        // do 1 here
        let r1 = ps1.InvokeAs<int>()
        // do 2 here
        let r2 = ps2.InvokeAs<int>()

        Assert.Equal(1, Assert.Single(r1))
        Assert.Equal(2, Assert.Single(r2))
        Assert.True(time.Elapsed.TotalSeconds > 2.0 * jobSeconds)

        // run one job parallel with another, it takes ~ jobSeconds
        let time = Stopwatch.StartNew()
        let r1, r2 =
            async {
                // do 1 in parallel
                let! complete1 = ps1.InvokeAsyncAs<int>() |> Async.StartChild
                // do 2 here
                let r2 = ps2.InvokeAs<int>()
                // complete job 1
                let! r1 = complete1
                // results
                return r1, r2
            }
            |> Async.RunSynchronously

        Assert.Equal(1, Assert.Single(r1))
        Assert.Equal(2, Assert.Single(r2))
        Assert.True(time.Elapsed.TotalSeconds < 2.0 * jobSeconds)

module Input =
    [<Fact>]
    let ``1 script`` () =
        use ps = PS.Create()

        let script = """
        param($Factor)
        process {
            $_ * $Factor
        }
        """

        let res =
            [1..4]
            |> ps.Script(script).AddArgument(3.14).InvokeAs
            |> Seq.toArray

        Assert.True([| 3.14; 6.28; 9.42; 12.56 |] = res)

    [<Fact>]
    let ``2 command`` () =
        use ps = PS.Create()

        let res =
            [
                __SOURCE_DIRECTORY__
                __SOURCE_DIRECTORY__ + "/" + __SOURCE_FILE__
            ]
            |> ps.Command("Get-Item").InvokeAs<FileSystemInfo>

        Assert.Equal(2, res.Count)
        Assert.Equal("tests", res.[0].Name)
        Assert.Equal("Test1.fs", res.[1].Name)
