module VSharp.Fuzzer.Generator

open System
open VSharp
open FsCheck


let private baseTypes = [
    typeof<int8>; typeof<int16>; typeof<int32>; typeof<int64>
    typeof<uint8>; typeof<uint16>; typeof<uint32>; typeof<uint64>
    typeof<float>; typeof<double>
    typeof<char>; typeof<string>
    typeof<byte>
    typeof<bool>
    typeof<decimal>
]

let (|BaseType|_|) (t: Type) = if List.contains t baseTypes then Some () else None
let (|ArrayType|_|) (t: Type) = if t.IsArray then Some () else None
let (|ClassType|_|) (t: Type) = if t.IsClass then Some () else None
let (|InterfaceType|_|) (t: Type) = if t.IsInterface then Some () else None
let (|RecordType|_|) (t: Type) = if t.IsValueType && not t.IsEnum && not (List.contains t baseTypes) then Some () else None


// Here we used FsCheck's Gen module to generate random values of the given type.
let mutable private stdGen = FsCheck.Random.StdGen(1, 1)
let mutable private size = 0
let private generateBase (t: Type): obj =

    assert (List.contains t baseTypes)

    let nextSize, nextStdGen = Random.stdNext stdGen
    stdGen <- nextStdGen
    size <- nextSize

    match t with
    | _ when t = typeof<int8> -> Arb.generate<int8>.Eval(size, stdGen)
    | _ when t = typeof<int16> -> Arb.generate<int16>.Eval(size, stdGen)
    | _ when t = typeof<int32> -> Arb.generate<int32>.Eval(size, stdGen)
    | _ when t = typeof<int64> -> Arb.generate<int64>.Eval(size, stdGen)
    | _ when t = typeof<uint8> -> Arb.generate<uint8>.Eval(size, stdGen)
    | _ when t = typeof<uint16> -> Arb.generate<uint16>.Eval(size, stdGen)
    | _ when t = typeof<uint32> -> Arb.generate<uint32>.Eval(size, stdGen)
    | _ when t = typeof<uint64> -> Arb.generate<uint64>.Eval(size, stdGen)
    | _ when t = typeof<float> -> Arb.generate<float>.Eval(size, stdGen)
    | _ when t = typeof<double> -> Arb.generate<double>.Eval(size, stdGen)
    | _ when t = typeof<char> -> Arb.generate<char>.Eval(size, stdGen)
    | _ when t = typeof<string> -> Arb.generate<string>.Eval(10, stdGen) // On big sizes it can be very slow
    | _ when t = typeof<byte> -> Arb.generate<byte>.Eval(size, stdGen)
    | _ when t = typeof<bool> -> Arb.generate<bool>.Eval(size, stdGen)
    | _ when t = typeof<decimal> -> Arb.generate<decimal>.Eval(size, stdGen)
    | _ -> __unreachable__()

let private setAllFields (t: Type) (setter: Type -> obj) =
    let fields = t.GetFields()
    let instance = Activator.CreateInstance(t)
    for field in fields do
        field.SetValue(instance, setter field.FieldType)
    instance

let rec private generateArray (t: Type) (rnd: Random): obj =
    // It doesn't work :(
    let arrayMaxSize = 100
    let arraySize = rnd.NextInt64(0L, int64 arrayMaxSize) |> int
    let array = Array.zeroCreate arraySize
    let elementType = t.GetElementType()
    for i in 0 .. arraySize - 1 do
        array[i] <- generate rnd elementType
    array

and private generateRecord (t: Type) (rnd: Random): obj =
    setAllFields t (generate rnd)

and private generateClass (t: Type) (rnd: Random): obj =
    let constructors = t.GetConstructors()
    let constructor = constructors.[rnd.NextInt64(0,  int64 constructors.Length) |> int32]
    let constructorArgsTypes = constructor.GetParameters() |> Array.map (fun p -> p.ParameterType)
    let constructorArgs = constructorArgsTypes |> Array.map (generate rnd)
    constructor.Invoke(constructorArgs)

and private generateInterface (t: Type) (rnd: Random): obj =
    let instances =
        t.Assembly.GetTypes()
        |> Array.filter (fun x -> x.IsClass && t.IsAssignableFrom(x))

    if instances.Length = 0 then internalfail "Mocking not supported"
    let instance = instances[rnd.NextInt64(0, instances.Length) |> int]
    generateClass instance rnd

and private generate (rnd: Random) (t: Type): obj =
    // TODO: add support for generic types
    // TODO: add support for enums
    // TODO: add support for tuples (How to recognize them?)
    // TODO: add support for abstract classes

    printfn "Generating %A" t
    match t with
    | BaseType -> generateBase t
    // | ArrayType -> generateArray t rnd
    | RecordType -> generateRecord t rnd
    | ClassType -> generateClass t rnd
    | InterfaceType -> generateInterface t rnd
    | _ -> failwithf $"Type {t} is not supported"


let generateObj (t: Type) = generate (Random(100)) t
