module VSharp.Fuzzer.Fuzzer

open System.Reflection

open VSharp
open VSharp.Core
open VSharp.Generator.GeneratorInfo
open VSharp.Fuzzer.FuzzerInfo

type FuzzingMethodInfo = {
    Method: MethodBase
    ArgsInfo: (Generator.Config.GeneratorConfig * System.Type) list
}

type FuzzingResult = {
    Args: obj list
    Returned: obj option
    Thrown: exn option
}

type Fuzzer() =

    member val private Config = defaultFuzzerConfig with get, set

    member private this.SolveGenerics (method: MethodBase): MethodBase option =
        let vsMethod = Application.getMethod method
        let getConcreteType = function
        | ConcreteType t -> Some t
        | _ -> None // TODO: add support for mock
        try
            match SolveGenericMethodParameters vsMethod with
            | Some(classParams, methodParams) ->
                let classParams = classParams |> Array.choose getConcreteType
                let methodParams = methodParams |> Array.choose getConcreteType
                if classParams.Length = method.DeclaringType.GetGenericArguments().Length &&
                    (method.IsConstructor || methodParams.Length = method.GetGenericArguments().Length) then
                    let declaringType = Reflection.concretizeTypeParameters method.DeclaringType classParams
                    let method = Reflection.concretizeMethodParameters declaringType method methodParams
                    Some method
                else
                    None
            | _ -> None
        with :? InsufficientInformationException -> None

    member private this.FuzzOnce (methodInfo: FuzzingMethodInfo) (rnd: System.Random) =
        let method = methodInfo.Method
        let argsTypes = method.GetParameters() |> Array.map (fun p -> p.ParameterType) // Делать один раз
        let args = argsTypes |> Array.map (this.Generator.Generate rnd)
        let mutable obj = null
        if method.IsStatic |> not then
            obj <- this.Generator.Generate rnd method.DeclaringType

        let result = {
            Args = args
            Returned = None
            Thrown = None
        }

        try
            let returned = method.Invoke(obj, args)
            { result with Returned = Some returned }
        with
        | e -> { result with Thrown = Some e }

    member private this.GetInfo (state: state) =
        state.evaluationStack
        let argsTypes =
            method.GetParameters()
            |> Array.map (fun p -> p.ParameterType)
            |> Array.map (fun t -> (crea, t))
        { Method = method; ArgsInfo = argsInfo |> Array.toList }

    interface IFuzzer with

        member this.Fuzz method seed =
            match this.SolveGenerics method with
            | Some m ->
                let rndGenerator = System.Random(seed)
                [0..this.Config.MaxTest]
                |> List.map (fun _ -> System.Random(rndGenerator.NextInt64() |> int))
                |> List.map (this.FuzzOnce m)
                |> Seq.ofList
            | None -> failwith "Can't solve generics"

        member this.Configure config =
            this.Config <- config
