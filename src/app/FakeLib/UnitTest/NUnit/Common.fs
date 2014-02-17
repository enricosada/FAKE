[<AutoOpen>]
/// Contains types and utility functions relaited to running [NUnit](http://www.nunit.org/) unit tests.
module Fake.NUnitCommon

open System
open System.IO
open System.Text

/// Option which allows to specify if a NUnit error should break the build.
type NUnitErrorLevel = TestRunnerErrorLevel // a type alias to keep backwards compatibility

/// Parameter type for NUnit.
type NUnitParams = 
    { IncludeCategory: string
      ExcludeCategory: string
      ToolPath: string
      ToolName: string
      TestInNewThread: bool
      OutputFile: string
      Out: string
      ErrorOutputFile: string
      Framework: string
      ShowLabels: bool
      WorkingDir: string
      XsltTransformFile: string
      TimeOut: TimeSpan
      DisableShadowCopy: bool
      Domain: string
      Process: string option
      AdditionalArguments: string list
      ErrorLevel: NUnitErrorLevel }

/// NUnit default parameters. FAKE tries to locate nunit-console.exe in any subfolder.
let NUnitDefaults = 
    let toolname = "nunit-console.exe"
    { IncludeCategory = ""
      ExcludeCategory = ""
      ToolPath = findToolFolderInSubPath toolname (currentDirectory @@ "tools" @@ "Nunit")
      ToolName = toolname
      TestInNewThread = false
      OutputFile = currentDirectory @@ "TestResult.xml"
      Out = ""
      ErrorOutputFile = ""
      WorkingDir = ""
      Framework = ""
      ShowLabels = true
      XsltTransformFile = ""
      TimeOut = TimeSpan.FromMinutes 5.0
      DisableShadowCopy = false
      Domain = ""
      Process = None
      AdditionalArguments = []
      ErrorLevel = Error }

/// Builds the command line arguments from the given parameter record and the given assemblies.
/// [omit]
let buildNUnitdArgs parameters assemblies =
    new StringBuilder()
    |> append "-nologo"
    |> appendIfTrue parameters.DisableShadowCopy "-noshadow" 
    |> appendIfTrue parameters.ShowLabels "-labels" 
    |> appendIfTrue parameters.TestInNewThread "-thread" 
    |> appendFileNamesIfNotNull assemblies
    |> appendIfNotNullOrEmpty parameters.IncludeCategory "-include:"
    |> appendIfNotNullOrEmpty parameters.ExcludeCategory "-exclude:"
    |> appendIfNotNullOrEmpty parameters.XsltTransformFile "-transform:"
    |> appendIfNotNullOrEmpty parameters.OutputFile  "-xml:"
    |> appendIfNotNullOrEmpty parameters.Out "-out:"
    |> appendIfNotNullOrEmpty parameters.Framework  "-framework:"
    |> appendIfNotNullOrEmpty parameters.ErrorOutputFile "-err:"
    |> appendIfNotNullOrEmpty parameters.Domain "-domain:"
    |> appendIfSome parameters.Process (sprintf "-process:%s")
    |> append (parameters.AdditionalArguments |> String.concat " ")
    |> toText

let buildNUnitdArgs2 p assemblies =
    let isSet s = not <| String.IsNullOrEmpty(s)
    let ifSet f x = if isSet x then Some (f x) else None
    [ 
        [
            Some "-nologo";
            (if p.DisableShadowCopy then Some "-noshadow" else None);
            (if p.ShowLabels then Some "-labels" else None);
            (if p.TestInNewThread then Some "-thread" else None);
        ];
        (assemblies |> List.map Some);
        [
            p.IncludeCategory |> ifSet (sprintf "-include:%s");
            p.ExcludeCategory |> ifSet (sprintf "-exclude:%s");
            p.XsltTransformFile |> ifSet (sprintf "-transform:'%s'");
            p.OutputFile |> ifSet (sprintf "-xml:'%s'");
            p.Out |> ifSet (sprintf "-out:'%s'");
            p.Framework |> ifSet (sprintf "-framework:%s")
            p.ErrorOutputFile |> ifSet (sprintf "-err:%s")
            p.Domain |> ifSet (sprintf "-domain:%s")
            p.Process |> Option.bind (sprintf "-process:%s" >> Some)
        ];
        (p.AdditionalArguments |> List.map Some);
    ]
    |> List.concat
    |> List.choose id


/// Tries to detect the working directory as specified in the parameters or via TeamCity settings
/// [omit]
let getWorkingDir parameters =
    Seq.find isNotNullOrEmpty [parameters.WorkingDir; environVar("teamcity.build.workingDir"); "."]
    |> Path.GetFullPath

/// NUnit console returns negative error codes for errors and sum of failed, ignored and exceptional tests otherwise. 
/// Zero means that all tests passed.
let (|OK|TestsFailed|FatalError|) errorCode =
    match errorCode with
    | 0 -> OK
    | -1 -> FatalError "InvalidArg"
    | -2 -> FatalError "FileNotFound"
    | -3 -> FatalError "FixtureNotFound"
    | -100 -> FatalError "UnexpectedError"
    | x when x < 0 -> FatalError "FatalError"
    | _ -> TestsFailed


