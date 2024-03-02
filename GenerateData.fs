open System
open System.IO
open System.IO.Pipes
open System.Text
open FSharp.Data
open System.Runtime.InteropServices

// Enable ANSI escape codes for the terminal

[<Literal>]
let STD_OUTPUT_HANDLE = -11

[<Literal>]
let ENABLE_VIRTUAL_TERMINAL_PROCESSING = 4u

[<DllImport("kernel32.dll", SetLastError = true)>]
extern IntPtr GetStdHandle(int nStdHandle)

[<DllImport("kernel32.dll")>]
extern bool GetConsoleMode(IntPtr hConsoleHandle, uint& lpMode)

[<DllImport("kernel32.dll")>]
extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode)

let handle = GetStdHandle(STD_OUTPUT_HANDLE)
let mutable mode = 0u
GetConsoleMode(handle, &mode) |> ignore
mode <- mode ||| ENABLE_VIRTUAL_TERMINAL_PROCESSING
SetConsoleMode(handle, mode) |> ignore

// *****************************************

type Event = JsonProvider<"data/sample-event.json", InferenceMode=InferenceMode.ValuesAndInlineSchemasOverrides>

let log color s =
    Console.ForegroundColor <- color
    printfn "%s" s
    Console.ResetColor()


let reset = "\x1b[0m"
let italic = "\x1b[3m"
let green = "\x1b[48;5;22m"
let underline = "\x1b[4m"
let magenta = "\x1b[35m"

let style escapeSequence s = $"{escapeSequence}{s}{reset}"

let fmtFocus focused s =
    if focused then $"{italic}{green}{s}{reset}" else s

let formatContainer thisMonitorFocused thisWorkspaceFocused containerFocused i (c: Event.Element3) =
    let thisContainerFocused = i = containerFocused

    let containerName = $"Container: {i + 1}"

    fmtFocus (thisMonitorFocused && thisWorkspaceFocused && thisContainerFocused) $"\t  {style underline containerName}"
    |> log ConsoleColor.DarkMagenta

    let windowFocused = c.Windows.Focused

    c.Windows.Elements
    |> Seq.iteri (fun i w ->
        (fmtFocus
            (thisMonitorFocused
             && thisWorkspaceFocused
             && thisContainerFocused
             && windowFocused = i)
            $"\t  - {w.Exe} | {w.Title}")
        |> Console.WriteLine)

let mutable json = []


let pipeServer = new NamedPipeServerStream("komorebi-pipe")

pipeServer.WaitForConnection()
printfn "Client connected."
let sr = new StreamReader(pipeServer)

let loop () =
    async {

        let! line = sr.ReadLineAsync() |> Async.AwaitTask
        let parsed = line |> Event.Parse

        printfn "infunc %s" (parsed.JsonValue.ToString())
        json <- json @ [ parsed.JsonValue.ToString() ]
    }
// let monitorFocused = event.State.Monitors.Focused
//
// event.State.Monitors.Elements
// |> Seq.iteri (fun i m ->
//     let thisMonitorFocused = i = monitorFocused
//
//     sprintf "%s: %s" (style underline "Monitor") (fmtFocus thisMonitorFocused m.Name)
//     |> style magenta
//     |> printfn " %s"
//
//     let workspaceFocused = m.Workspaces.Focused
//
//     m.Workspaces.Elements
//     |> Seq.iteri (fun i w ->
//         let thisWorkspaceFocused = i = workspaceFocused
//
//         match w.Name with
//         | Some name ->
//             fmtFocus (thisMonitorFocused && thisWorkspaceFocused) $"\t{style underline name}"
//             |> log ConsoleColor.Cyan
//         | None -> ()
//
//         let containerFocused = w.Containers.Focused
//
//         let containerFormatter =
//             formatContainer thisWorkspaceFocused thisMonitorFocused containerFocused
//
//         w.Containers.Elements |> Seq.iteri containerFormatter
//
//         containerFormatter true w.MonocleContainer))

loop () |> Async.RunSynchronously
loop () |> Async.RunSynchronously
loop () |> Async.RunSynchronously
loop () |> Async.RunSynchronously
loop () |> Async.RunSynchronously
loop () |> Async.RunSynchronously
loop () |> Async.RunSynchronously
loop () |> Async.RunSynchronously
loop () |> Async.RunSynchronously
loop () |> Async.RunSynchronously
loop () |> Async.RunSynchronously
loop () |> Async.RunSynchronously
loop () |> Async.RunSynchronously
loop () |> Async.RunSynchronously
loop () |> Async.RunSynchronously
loop () |> Async.RunSynchronously
loop () |> Async.RunSynchronously
loop () |> Async.RunSynchronously
loop () |> Async.RunSynchronously
loop () |> Async.RunSynchronously
loop () |> Async.RunSynchronously
loop () |> Async.RunSynchronously
loop () |> Async.RunSynchronously
loop () |> Async.RunSynchronously
loop () |> Async.RunSynchronously
loop () |> Async.RunSynchronously
loop () |> Async.RunSynchronously
loop () |> Async.RunSynchronously
loop () |> Async.RunSynchronously
loop () |> Async.RunSynchronously
loop () |> Async.RunSynchronously
loop () |> Async.RunSynchronously

printfn "here"

let sw = new StreamWriter("data/sample-events.json")

sw.WriteLine("[")
json |> Seq.iter (fun e -> sw.WriteLine(e + ",") |> ignore)
sw.WriteLine("]")
sw.Close()
pipeServer.Close()
