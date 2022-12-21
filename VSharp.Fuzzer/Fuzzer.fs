namespace Fuzzer

open System
open System.Collections.Generic
open System.Reflection

open VSharp
open VSharp.Core
open VSharp.Fuzzer.FuzzerInfo

type FuzzingMethodInfo = {
    Method: MethodBase
    ArgsInfo: (Generator.Config.GeneratorConfig * Type) array
    ThisInfo: (Generator.Config.GeneratorConfig * Type) option
}

type FuzzingResult =
    | Thrown of (obj * Type) array * exn
    | Returned of (obj * Type) array * obj

// TODO: refactor module: 'Fuzzer' class has method parameter
type Fuzzer(method : Method) =

    let methodBase = (method :> IMethod).MethodBase
    let typeMocks = Dictionary<Type list, ITypeMock>()

    member val private Config = defaultFuzzerConfig with get, set
    member val private Generator = Generator.Generator.generate with get

    // TODO: refactor
    member private this.SolveGenerics (method: IMethod) (model: model option): MethodBase option =

        let getConcreteType = function
        | ConcreteType t -> Some t
        | _ -> None // TODO: add support for mock

        let typeModel =
            match model with
            | Some (StateModel (_, typeModel)) -> typeModel
            | None -> typeModel.CreateEmpty()
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

    member private this.GetInfo (state: state option) =
        let model = Option.map (fun s -> s.model) state
        let methodBase =
            match this.SolveGenerics method model with
            | Some methodBase -> methodBase
            | None -> failwith "Can't solve generic parameters"
        let argsInfo =
            method.Parameters
            |> Array.map (fun x -> Generator.Config.defaultGeneratorConfig, x.ParameterType)
        let thisInfo =
            if method.HasThis then
                Some (Generator.Config.defaultGeneratorConfig, method.DeclaringType)
            else
                None
        { Method = methodBase; ArgsInfo = argsInfo; ThisInfo = thisInfo }

    member private this.FuzzOnce (methodInfo: FuzzingMethodInfo) (rnd: Random) =
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

//    pc -- Empty
//    evaluationStack -- Result (in case of generation), Empty (in case of seed)
//    stack -- First frame = method
//    stackBuffers -- Empty
//    classFields -- Empty
//    arrays -- Empty
//    lengths -- Empty
//    lowerBounds -- Empty
//    staticFields -- Empty
//    boxedLocations -- Empty
//    initializedTypes -- Empty
//    concreteMemory -- All heap objects
//    allocatedTypes -- All heap objects
//    typeVariables -- Empty
//    delegates -- Empty (?)
//    currentTime -- startingTime
//    startingTime -- default
//    exceptionsRegister -- Empty (in case of Returned), HeapRef exn (in case of Thrown)
//    model -- Empty StateModel (in case of generation), Filled StateModel? (in case of seed)
//    complete -- true?
//    typeMocks -- created mocks

//    member private this.FillModel (args: array<obj * Type>) =
//        let model = Memory.EmptyModel method (typeModel.CreateEmpty())
//        match model with
//        | StateModel (state, _) ->
//            state
//        | _ -> __unreachable__()

    member private this.FillState (args : array<obj * Type>) =
        // Creating state
        let state = Memory.EmptyState()
        state.model <- Memory.EmptyModel method (typeModel.CreateEmpty())
        // Creating first frame and filling stack
        let this =
            if method.HasThis then
                Some (Memory.ObjectToTerm state (fst (Array.head args)) method.DeclaringType)
            else None
        let args = Array.tail args
        let createTerm (arg, argType) = Memory.ObjectToTerm state arg argType |> Some
        let parameters = Array.map createTerm args |> List.ofArray
        Logger.info $"[Fuzzer] Creating state with params: {parameters}"
        Memory.InitFunctionFrame state method this (Some parameters)
        // Filling used type mocks
        for mock in typeMocks do state.typeMocks.Add mock

        match state.model with
        | StateModel (model, _) ->
            Memory.InitFunctionFrame model method this (Some parameters)
        | _ -> __unreachable__()

        // Returning filled state
        state

    member private this.FuzzingResultToInitialState (result: FuzzingResult) =
        match result with
        | Returned (args, _)
        | Thrown(args, _) ->
            this.FillState args

    member private this.FuzzingResultToCompletedState (result: FuzzingResult) =
        match result with
        | Returned (args, returned) ->
            let state = this.FillState args
            // Pushing result onto evaluation stack
            let returnType = Reflection.getMethodReturnType methodBase
            let returnedTerm = Memory.ObjectToTerm state returned returnType
            state.evaluationStack <- EvaluationStack.Push returnedTerm state.evaluationStack
            state
        | Thrown(args, exn) ->
            let state = this.FillState args
            // Filling exception register
            let exnType = exn.GetType()
            let exnRef = Memory.AllocateConcreteObject state exn exnType
            // TODO: check if exception was thrown by user or by runtime
            state.exceptionsRegister <- Unhandled(exnRef, false)
            state

    member this.FuzzWithState state seed =
        let info = this.GetInfo (Some state)
        let rndGenerator = Random(seed)
        [0..this.Config.MaxTest]
        |> List.map (fun _ -> Random(rndGenerator.NextInt64() |> int))
        |> List.map (this.FuzzOnce info)
        |> List.map this.FuzzingResultToCompletedState
        |> Seq.ofList

    member this.Fuzz () =
        let seed = Int32.MaxValue // Magic const!!!!
        let info = this.GetInfo None
        let rndGenerator = Random(seed)
        [0..this.Config.MaxTest]
        |> List.map (fun _ -> Random(rndGenerator.Next() |> int))
        |> List.map (this.FuzzOnce info)
        |> List.map this.FuzzingResultToInitialState
        |> Seq.ofList

    member this.Configure config =
        this.Config <- config
