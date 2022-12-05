module VSharp.Generator.ArrayGenerator

open System

open VSharp.Generator.Config
open VSharp.Generator.GeneratorInfo
open VSharp

let private recognize (t : Type) = t.IsArray

let private generate (rnd: Random) (conf: GeneratorConfig) (t: Type) =
    assert (recognize t)
    if t.IsSZArray then
        let arraySize = rnd.NextInt64(0L, int64 conf.ArrayMaxSize) |> int
        let elementType = t.GetElementType()
        let array = Array.CreateInstance(elementType, arraySize)
        for i in 0 .. arraySize - 1 do
            array.SetValue(commonGenerator rnd conf elementType, i)
        array :> obj |> Some
    else
        // TODO: multidimensional arrays
        // TODO: LowerBound
        __notImplemented__ ()

let arrayGenerator: Generator = mkGenerator recognize generate

