module VSharp.Generator.ArrayGenerator

open System

open VSharp.Generator.Config
open VSharp

let (|Array|_|) (t : Type) = if t.IsArray then Some Array else None

let generate commonGenerator (rnd: Random) (conf: GeneratorConfig) (t: Type) =
    if t.IsSZArray then
        let arraySize = rnd.NextInt64(0L, int64 conf.ArrayMaxSize) |> int
        let elementType = t.GetElementType()
        let array = Array.CreateInstance(elementType, arraySize)
        for i in 0 .. arraySize - 1 do
            array.SetValue(commonGenerator rnd conf elementType, i)
        array :> obj
    else
        // TODO: multidimensional arrays
        // TODO: LowerBound
        __notImplemented__ ()



