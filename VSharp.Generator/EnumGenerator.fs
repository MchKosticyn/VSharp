module VSharp.Generator.EnumGenerator

open System

open VSharp.Generator.Config
open VSharp.Generator.GeneratorInfo


let private recognize (t : Type) = t.IsEnum
let private generate (rnd: Random) (_: GeneratorConfig) (t: Type) =
    let values = Enum.GetValues(t)
    let index = rnd.NextInt64(0, int64 values.Length) |> int
    values.GetValue(index) |> Some

let enumGenerator: Generator = mkGenerator recognize generate
