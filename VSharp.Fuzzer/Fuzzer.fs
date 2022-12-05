module VSharp.Fuzzer.Fuzzer

open System.Reflection

open VSharp
open VSharp.Core
open VSharp.Fuzzer.FuzzerInfo

type FuzzingMethodInfo = {
    Method: MethodBase
    ArgsInfo: (Generator.Config.GeneratorConfig * System.Type) array
    ThisInfo: (Generator.Config.GeneratorConfig * System.Type) option
}

type FuzzingResult =
    | Thrown of (obj * System.Type) array * exn
    | Returned of (obj * System.Type) array * obj

type Fuzzer() =

    member val private Config = defaultFuzzerConfig with get, set
    member val private Generator = Generator.GeneratorInfo.GetGenerator() with get

    member private this.SolveGenerics (method: IMethod) (state: state): MethodBase option =

        let getConcreteType = function
        | ConcreteType t -> Some t
        | _ -> None // TODO: add support for mock

        let typeModel =
            match state.model with
            | StateModel (_, typeModel) -> typeModel
            | _ -> __unreachable__()

        let methodBase = method.MethodBase

        try
            match SolveGenericMethodParameters typeModel method with
            | Some(classParams, methodParams) ->
                let classParams = classParams |> Array.choose getConcreteType
                let methodParams = methodParams |> Array.choose getConcreteType
                if classParams.Length = methodBase.DeclaringType.GetGenericArguments().Length &&
                    (methodBase.IsConstructor || methodParams.Length = methodBase.GetGenericArguments().Length) then
                    let declaringType = Reflection.concretizeTypeParameters methodBase.DeclaringType classParams
                    let methodBase = Reflection.concretizeMethodParameters declaringType methodBase methodParams
                    Some methodBase
                else
                    None
            | _ -> None
        with :? InsufficientInformationException -> None

    member private this.GetInfo (state: state) =
        let method = API.Memory.GetEntryPoint state
        let methodBase =
            match this.SolveGenerics method state with
            | Some methodBase -> methodBase
            | None -> failwith "Can't solve generic parameters"
        let argsInfo =
            method.Parameters
            |> Array.map (fun x -> Generator.Config.generateConfigForArg state x, x.ParameterType)
        let thisInfo =
            if method.HasThis then
                Some (Generator.Config.generateConfigForType state method.DeclaringType, method.DeclaringType)
            else
                None
        { Method = methodBase; ArgsInfo = argsInfo; ThisInfo = thisInfo }

    member private this.FuzzOnce (methodInfo: FuzzingMethodInfo) (rnd: System.Random) =
        let method = methodInfo.Method
        let args = methodInfo.ArgsInfo |> Array.map (fun (config, t) -> this.Generator rnd config t, t)
        let mutable obj = null
        if Reflection.hasThis method then
            let config, t = methodInfo.ThisInfo.Value
            obj <- this.Generator rnd config t

        let argsWithThis = Array.append [|obj, method.DeclaringType|] args

        try
            let returned = method.Invoke(obj, Array.map fst args)
            Returned (argsWithThis, returned)
        with
        | e -> Thrown (argsWithThis, e)


    member private this.FuzzingResultToState (state: state) (result: FuzzingResult) =
        while Memory.CallStackSize state > 1 do
            API.Memory.PopFrame state
        assert (Memory.CallStackSize state = 1)
        match result with
        | Returned (args, returned) ->
            let cm = state.concreteMemory
            let concreteAddresses = Array.map (fun (arg, t) -> API.Memory.AllocateConcreteObject state arg t) args
            __notImplemented__()
        __notImplemented__()

    interface IFuzzer with

        member this.Fuzz state seed =
            let info = this.GetInfo state
            let rndGenerator = System.Random(seed)
            [0..this.Config.MaxTest]
            |> List.map (fun _ -> System.Random(rndGenerator.NextInt64() |> int))
            |> List.map (this.FuzzOnce info)
            |> List.map (this.FuzzingResultToState state)
            |> Seq.ofList

        member this.Configure config =
            this.Config <- config
