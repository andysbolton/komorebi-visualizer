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

type Event =
    JsonProvider<"data/sample-events.json", SampleIsList=true, InferenceMode=InferenceMode.ValuesAndInlineSchemasOverrides>

let reset = "\x1b[0m"
let italic = "\x1b[3m"
let deepskyblue = "\x1b[48;5;23m"
let underline = "\x1b[4m"
let magenta = "\x1b[35m"
let deepink = "\x1b[38;5;131m"

let join col = List.fold (+) "" col

let indent = List.replicate 4 " " |> join

let style escapeSequence s = $"{escapeSequence}{s}{reset}"

let (|Indent|_|) (s: string) =
    if s.StartsWith(indent) then
        Some(s.Substring(indent.Length))
    else
        None

let splitIndent s =
    let seed = []

    let rec dosplit s l =
        match s with
        | Indent rest -> dosplit rest (l @ [ indent ])
        | "" -> l
        | _ -> l @ [ s ]

    dosplit s seed

let fmtFocus focused (s: string) =
    if focused then
        let sections = splitIndent s
        let indents = sections[.. sections.Length - 2] |> join
        $"{indents}{deepskyblue}{(List.last sections)}{reset}"
    else
        s

type WorkspaceElement =
    | ContainerElement of Event.Element3
    | Content of Event.Content

let formatContent (w: Event.Content) =
    $"{indent}{indent}  - {italic}{w.Exe} | {w.Title}{reset}"

let formatContainer thisMonitorFocused thisWorkspaceFocused containerFocused title i workspaceElement =
    let thisContainerFocused = i = containerFocused

    let containerName = $"{title}: {i + 1}"

    fmtFocus
        (thisMonitorFocused && thisWorkspaceFocused && thisContainerFocused)
        $"{indent}{indent}{style underline containerName}"
    |> style deepink
    |> printfn "%s"

    match workspaceElement with
    | ContainerElement c ->
        let windowFocused = c.Windows.Focused

        c.Windows.Elements
        |> Seq.iteri (fun i w ->
            (fmtFocus
                (thisMonitorFocused
                 && thisWorkspaceFocused
                 && thisContainerFocused
                 && windowFocused = i)
                (formatContent w))
            |> printfn "%s")
    | Content w -> formatContent w |> printfn "%s"

let pipeServer = new NamedPipeServerStream("komorebi-pipe")

printfn "Waiting for connection."
pipeServer.WaitForConnection()
printfn "Client connected."

let sr = new StreamReader(pipeServer)

let rec loop () =
    async {

        let! line = sr.ReadLineAsync() |> Async.AwaitTask

        // Erase screen
        Console.WriteLine("\x1b[2J")

        let event = line |> Event.Parse

        let monitorFocused = event.State.Monitors.Focused

        event.State.Monitors.Elements
        |> Seq.iteri (fun i m ->
            let thisMonitorFocused = i = monitorFocused

            fmtFocus thisMonitorFocused $"Monitor: {m.Name}"
            |> style magenta
            |> style underline
            |> printfn " %s"

            let workspaceFocused = m.Workspaces.Focused

            m.Workspaces.Elements
            |> Seq.iteri (fun i w ->
                let thisWorkspaceFocused = i = workspaceFocused

                fmtFocus (thisMonitorFocused && thisWorkspaceFocused) (sprintf "%s%s" indent $"Workspace: {w.Name}")
                |> printfn "%s"

                let containerFocused = w.Containers.Focused

                let containerFormatter =
                    formatContainer thisWorkspaceFocused thisMonitorFocused containerFocused

                w.Containers.Elements
                |> Seq.map ContainerElement
                |> Seq.iteri (containerFormatter "Container")

                match w.MonocleContainer with
                | Some monocle -> containerFormatter "Container (monocle)" 0 (ContainerElement monocle)
                | None -> ()

                w.FloatingWindows
                |> Seq.map Content
                |> Seq.iteri (containerFormatter "Container (floating)")))

        do! loop ()
    }

loop () |> Async.RunSynchronously

sr.Close()
pipeServer.Close()
