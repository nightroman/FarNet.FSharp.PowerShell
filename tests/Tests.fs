module Tests
open FarNet.FSharp.PowerShell
open Xunit
open System
open System.IO
open System.Diagnostics

[<Fact>]
let Invoke2_convert () =
    use ps = PS.Create()
    let res = ps.Script("1; 3.14; '42'; ''; $null").Invoke2<int>()
    Assert.True([| 1; 3; 42; 0; 0 |] = res)

[<Fact>]
let InvokeAsync2_convert () = async {
    use ps = PS.Create()
    let! res = ps.Script("1; 3.14; '42'; ''; $null").InvokeAsync2<int>()
    Assert.True([| 1; 3; 42; 0; 0 |] = res)
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
    let ``Command Invoke Invoke2`` () =
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

        let res = command.Invoke2<FileInfo>()
        Assert.True(res.Length > 2)
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
            """).Invoke2()
            |> Array.exactlyOne

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
            """).Invoke2()
            |> Array.exactlyOne

        Assert.True({TypesRecord2.Name = "May"; Age = 11} = res)

    // Get-Type is needed on calling from F# scripts because type names contain unknown parts.
    // But on calling from F# assembly scripts may use types explicitly with their full names.
    [<Fact>]
    let ``3 explicit type`` () =
        use ps = PS.Create()
        let res =
            ps.Script(""" [Tests+Types+TypesRecord1]::new('May', 11) """).Invoke2()
            |> Array.exactlyOne

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
        let r1 = ps1.Invoke2<int>()
        // do 2 here
        let r2 = ps2.Invoke2<int>()

        Assert.Equal(1, Assert.Single(r1))
        Assert.Equal(2, Assert.Single(r2))
        Assert.True(time.Elapsed.TotalSeconds > 2.0 * jobSeconds)

        // run one job parallel with another, it takes ~ jobSeconds
        let time = Stopwatch.StartNew()
        let r1, r2 =
            async {
                // do 1 in parallel
                let! complete1 = ps1.InvokeAsync2<int>() |> Async.StartChild
                // do 2 here
                let r2 = ps2.Invoke2<int>()
                // complete job 1
                let! r1 = complete1
                // results
                return r1, r2
            }
            |> Async.RunSynchronously

        Assert.Equal(1, Assert.Single(r1))
        Assert.Equal(2, Assert.Single(r2))
        Assert.True(time.Elapsed.TotalSeconds < 2.0 * jobSeconds)
