[<AutoOpen>]
/// Contains tasks to run [NUnit](http://www.nunit.org/) unit tests.
module Fake.NUnitSequential

type ShOptions = {
    Timeout: System.TimeSpan

}

let sh path (workingDir: System.IO.DirectoryInfo) timeout arguments =
    let args = arguments |> List.toSeq |> String.concat " "
    ExecProcess (fun (info: System.Diagnostics.ProcessStartInfo) ->  
        info.FileName <- path
        info.WorkingDirectory <- workingDir.FullName
        info.Arguments <- args) timeout

let internal nunitConsole (parameters: NUnitParams) assemblies =
    if List.isEmpty assemblies then
        failwith "NUnit: cannot run tests (the assembly list is empty)."
         
    let tool = parameters.ToolPath @@ parameters.ToolName

    let args = buildNUnitdArgs2 parameters assemblies
    let workingDir = System.IO.DirectoryInfo(parameters.WorkingDir)
    (tool, workingDir parameters.TimeOut args 

let internal nunitHelper runner parameters assemblies =
    let details = assemblies |> List.toSeq |> separated ", "
    traceStartTask "NUnit" details

    let result = runner parameters assemblies

    sendTeamCityNUnitImport (parameters.WorkingDir @@ parameters.OutputFile)

    let errorDescription error = 
        match error with
        | OK -> "OK"
        | TestsFailed -> sprintf "NUnit test failed (%d)." error
        | FatalError x -> sprintf "NUnit test failed. Process finished with exit code %s (%d)." x error
    
    match parameters.ErrorLevel with
    | DontFailBuild ->
        match result with
        | OK | TestsFailed -> traceEndTask "NUnit" details
        | _ -> failwith (errorDescription result)
    | Error ->
        match result with
        | OK -> traceEndTask "NUnit" details
        | _ -> failwith (errorDescription result)


/// Runs NUnit on a group of assemblies.
/// ## Parameters
/// 
///  - `setParams` - Function used to manipulate the default NUnitParams value.
///  - `assemblies` - Sequence of one or more assemblies containing NUnit unit tests.
/// 
/// ## Sample usage
///
///     Target "Test" (fun _ ->
///         !! (testDir + @"\Test.*.dll") 
///           |> NUnit (fun p -> { p with ErrorLevel = DontFailBuild })
///     )
let NUnit setParams assemblies =
    let parameters = 
        NUnitDefaults 
        |> setParams
        |> (fun p -> {p with WorkingDir = getWorkingDir p})
    nunitHelper (nunitConsole sh) parameters (assemblies |> Seq.toList)

