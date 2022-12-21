module VSharp.Generator.PrimitiveGenerator

open System

open VSharp.Generator.Config
open VSharp
open FsCheck

let private primitiveTypes = [
    typeof<int8>; typeof<int16>; typeof<int32>; typeof<int64>
    typeof<uint8>; typeof<uint16>; typeof<uint32>; typeof<uint64>
    typeof<float>; typeof<double>
    typeof<char>; typeof<string>
    typeof<byte>
    typeof<bool>
    typeof<decimal>
]

let mutable private stdGen = FsCheck.Random.StdGen(1, 1)
let mutable private size = 0

let (|Primitive|_|) t =
    if List.contains t primitiveTypes then Some Primitive else None

let generate (rnd: Random) (conf: GeneratorConfig) (t: Type) =
    let nextSize, nextStdGen = Random.stdNext stdGen
    stdGen <- nextStdGen
    size <- nextSize
    let stdGen = stdGen
    let size = size

    let res: obj =
        match t with
        | _ when t = typeof<int8> -> Arb.generate<int8>.Eval(size, stdGen)
        | _ when t = typeof<int16> -> Arb.generate<int16>.Eval(size, stdGen)
        | _ when t = typeof<int32> -> Arb.generate<int32>.Eval(size, stdGen)
        | _ when t = typeof<int64> -> Arb.generate<int64>.Eval(size, stdGen)
        | _ when t = typeof<uint8> -> Arb.generate<uint8>.Eval(size, stdGen)
        | _ when t = typeof<uint16> -> Arb.generate<uint16>.Eval(size, stdGen)
        | _ when t = typeof<uint32> -> Arb.generate<uint32>.Eval(size, stdGen)
        | _ when t = typeof<uint64> -> Arb.generate<uint64>.Eval(size, stdGen)
        | _ when t = typeof<float> -> Arb.generate<float>.Eval(size, stdGen) // TODO: check size
        | _ when t = typeof<double> -> Arb.generate<double>.Eval(size, stdGen)
        | _ when t = typeof<char> ->
            // Arb.generate<char>.Eval(size, stdGen)
            rnd.Next(33, int Char.MaxValue) |> char |> box
        | _ when t = typeof<string> -> Arb.generate<string>.Eval(conf.StringMaxSize, stdGen)
        | _ when t = typeof<byte> -> Arb.generate<byte>.Eval(size, stdGen)
        | _ when t = typeof<bool> -> Arb.generate<bool>.Eval(size, stdGen)
        | _ when t = typeof<decimal> -> Arb.generate<decimal>.Eval(size, stdGen)
        | _ -> __unreachable__()
    res
