module VSharp.Generator.EnumGenerator

open System

open VSharp.Generator.Config

let (|Enum|_|) (t : Type) =
    if t.IsEnum then Some Enum else None

let generate (rnd: Random) (_: GeneratorConfig) (t: Type) =
    let values = Enum.GetValues(t)
    let index = rnd.NextInt64(0, int64 values.Length) |> int
    values.GetValue(index) |> Some

