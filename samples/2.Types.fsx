// Define F# types, get and use them in called PowerShell.

open FarNet.FSharp.PowerShell

// One PS session for several commands.
let ps = PS.Create()

// F# record may be created by the "all fields" constructor.

type Record1 = {Name: string; Age: int}

ps.Script("""
$type = Get-Type Record1
$type::new('Joe', 42)
""").Invoke()
|> printfn "%A"

// F# record with default constructor and settable fields.

[<CLIMutable>]
type Record2 = {Name: string; Age: int}

ps.Script("""
$type = Get-Type Record2
$x = $type::new()
$x.Name = 'May'
$x.Age = 11
$x
""").Invoke()
|> printfn "%A"

// Or pass types in PS once and avoid search by Get-Type:

ps.Script("$Record1 = $args[0]; $Record2 = $args[1]")
    .AddArgument(typeof<Record1>)
    .AddArgument(typeof<Record2>)
    .Invoke()

ps.Script("""
$Record1::new('Jonh', 22)
""").Invoke()
|> printfn "%A"

ps.Script("""
$x = $Record2::new()
$x.Name = 'Mary'
$x.Age = 33
$x
""").Invoke()
|> printfn "%A"
