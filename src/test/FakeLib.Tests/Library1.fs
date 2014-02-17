module FakeLib.Tests.NUnit

open Fake.NUnitSequential

open NUnit.Framework
open FsUnit

[<Test>]
let ``nunit console`` () =
    nunitConsole execProcess (parameters: NUnitParams) assemblies =

    Assert.Fail("todo")
