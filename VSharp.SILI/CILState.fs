namespace VSharp.Interpreter.IL

open VSharp
open System.Text
open System.Collections.Generic
open VSharp.Core
open VSharp.Interpreter.IL
open IpOperations

module CilState =

    type prefix =
        | Constrained of System.Type

    let mutable currentStateId = 0u
    let getNextStateId() =
        let nextId = currentStateId
        currentStateId <- currentStateId + 1u
        nextId

    type public ErrorReporter internal (cilState : cilState) =
        let mutable cilState = cilState
        let mutable stateConfigured : bool = false

        static let mutable reportError : cilState -> string -> unit =
            fun _ _ -> internalfail "'reportError' is not ready"
        static let mutable reportFatalError : cilState -> string -> unit =
            fun _ _ -> internalfail "'reportFatalError' is not ready"

        static member Configure reportErrorFunc reportFatalErrorFunc =
            reportError <- reportErrorFunc
            reportFatalError <- reportFatalErrorFunc

        static member ReportError (cilState : cilState) message =
            cilState.ReportError()
            reportError cilState message

        static member ReportFatalError (cilState : cilState) message =
            cilState.ReportError()
            reportFatalError cilState message

        interface IErrorReporter with
            override x.ReportError msg failCondition =
                assert stateConfigured
                let report state k =
                    let cilState = cilState.ChangeState state
                    cilState.ReportError()
                    reportError cilState msg |> k
                let mutable isAlive = false
                StatedConditionalExecution cilState.State
                    (fun state k -> k (!!failCondition, state))
                    (fun _ k -> k (isAlive <- true))
                    report
                    (fun _ _ -> [])
                    ignore
                isAlive

            override x.ReportFatalError msg failCondition =
                assert stateConfigured
                let report state k =
                    let cilState = cilState.ChangeState state
                    cilState.ReportError()
                    reportFatalError cilState msg |> k
                let mutable isAlive = false
                StatedConditionalExecution cilState.State
                    (fun state k -> k (!!failCondition, state))
                    (fun _ k -> k (isAlive <- true))
                    report
                    (fun _ _ -> [])
                    ignore
                isAlive

            override x.ConfigureState state =
                cilState <- cilState.ChangeState state
                stateConfigured <- true

    and cilState private (
        ipStack : ipStack,
        prefixContext : prefix list,
        currentLoc : codeLocation,
        stackArrays : pset<concreteHeapAddress>,
        errorReported : bool,
        filterResult : term option,
        iie : InsufficientInformationException option,
        level : level,
        initialEvaluationStackSize : uint32,
        stepsNumber : uint,
        suspended : bool,
        targets : Set<codeLocation>,
        history : Set<codeLocation>,
        entryMethod : Method option,
        startingIp : instructionPointer,
        state : state) as this =

        let mutable ipStack : ipStack = ipStack
        // This field stores information about instruction prefix (for example, '.constrained' prefix)
        let mutable prefixContext : prefix list = prefixContext
        // TODO: get rid of currentLoc!
        // This field stores only approximate information and can't be used for getting the precise location. Instead, use ipStack.Head
        let mutable currentLoc : codeLocation = currentLoc
        let mutable stackArrays : pset<concreteHeapAddress> = stackArrays
        let mutable errorReported : bool = errorReported
        let mutable filterResult : term option = filterResult
        let mutable iie : InsufficientInformationException option = iie
        let mutable level : level = level
        let mutable initialEvaluationStackSize : uint32 = initialEvaluationStackSize
        let mutable stepsNumber : uint = stepsNumber
        let mutable suspended : bool = suspended
        let mutable targets : Set<codeLocation> = targets
        /// <summary>
        /// All basic blocks visited by the state.
        /// </summary>
        let mutable history : Set<codeLocation> = history
        /// <summary>
        /// If the state is not isolated (produced during forward execution), Some of it's entry point method, else None.
        /// </summary>
        let entryMethod : Method option = entryMethod
        /// <summary>
        /// Deterministic state id.
        /// </summary>
        let internalId : uint = getNextStateId()

        let errorReporter = lazy ErrorReporter(this)

        new (m : Method, state : state) =
            let ip = Instruction(0<offsets>, m)
            let ipStack = List.singleton ip
            let listEmpty = List.empty
            let currentLoc = ip.ToCodeLocation() |> Option.get
            let pSetEmpty = PersistentSet.empty
            let pDictEmpty = PersistentDict.empty
            let setEmpty = Set.empty
            cilState(
                ipStack, listEmpty, pSetEmpty, false, None, None, pDictEmpty,
                0u, 0u, false, setEmpty, setEmpty, Some m, ip, state
            )

        member x.IsIsolated with get() = entryMethod.IsNone

        member x.EntryMethod with get() =
            if x.IsIsolated then invalidOp "Isolated state doesn't have an entry method"
            entryMethod.Value

        member x.StartsFromMethodBeginning with get() =
            match startingIp with
            | Instruction (0<offsets>, _) -> true
            | _ -> false

        member x.SetCurrentTime time = state.currentTime <- time

        member val ID = internalId

        member x.History with get() = history

        // -------------------- Exception and errors operations --------------------

        member x.SetException exc =
            state.exceptionsRegister <- state.exceptionsRegister.Tail.Push exc

        member x.IsUnhandledException with get() =
            match state.exceptionsRegister.Peek with
            | Unhandled _ -> true
            | _ -> false

        member x.IsUnhandledExceptionOrError with get() =
            match state.exceptionsRegister.Peek with
            | Unhandled _ -> true
            | _ -> errorReported

        member x.HasReportedError with get() = errorReported
        member x.ReportError() = errorReported <- true

        member x.IsStoppedByException with get() =
            match x.CurrentIp with
            | EmptySearchingForHandler -> true
            | _ -> false

        member x.HasRuntimeExceptionOrError with get() =
            match state.exceptionsRegister.Peek with
            | _ when errorReported -> true
            | Unhandled(_, isRuntime, _) -> isRuntime
            | _ -> false

        member x.IsIIEState with get() = Option.isSome iie

        member x.SetIIE (e : InsufficientInformationException) =
            iie <- Some e

        member x.IIE with get() = iie

        member x.IsExecutable with get() =
            match ipStack with
            | [] -> __unreachable__()
            | [ Exit _ ] -> false
            | _ -> true

        member x.IsStopped with get() =
            x.IsIIEState || x.IsStoppedByException || not x.IsExecutable

        member x.NewExceptionRegister() =
            state.exceptionsRegister <- state.exceptionsRegister.Push NoException

        member x.PopExceptionRegister() =
            state.exceptionsRegister <- state.exceptionsRegister.Tail

        member x.ToUnhandledException() =
            state.exceptionsRegister <- state.exceptionsRegister.TransformToUnhandled()

        member x.ToCaughtException() =
            state.exceptionsRegister <- state.exceptionsRegister.TransformToCaught()

        member x.MoveDownExceptionRegister() =
            let elem, rest = state.exceptionsRegister.Pop()
            state.exceptionsRegister <- rest.Tail.Push elem

        // -------------------- Instruction pointer operations --------------------

        member val StartingIp = startingIp

        member x.CurrentIp with get() =
            match ipStack with
            | [] -> internalfail "currentIp: 'IP' stack is empty"
            | h :: _ -> h

        // Obtaining exploring method
        member x.CurrentMethod with get() = x.CurrentIp.ForceMethod()

        member x.CurrentOffset with get() = x.CurrentIp.Offset

        member x.PushToIp (ip : instructionPointer) =
            let loc = currentLoc
            match ip.ToCodeLocation() with
            | Some loc' when loc'.method.HasBody ->
                currentLoc <- loc'
                Application.addCallEdge loc loc'
            | _ -> ()
            ipStack <- ip :: ipStack

        member x.SetCurrentIp (ip : instructionPointer) =
            x.MoveCodeLoc ip
            assert(List.isEmpty ipStack |> not)
            ipStack <- ip :: List.tail ipStack

        member x.SetCurrentIpSafe (ip : instructionPointer) =
            let ip = x.CurrentIp.ChangeInnerIp ip
            x.SetCurrentIp ip

        member x.ReplaceLastIp (ip : instructionPointer) =
            let newIp = x.CurrentIp.ReplacePenultimateIp ip
            x.SetCurrentIp newIp

        member x.ExitMethod (m : Method) =
            match ipStack with
            | ip :: ips ->
                assert(ip.Method = Some m)
                ipStack <- (Exit m) :: ips
            | [] -> __unreachable__()

        member x.IpStack with get() = ipStack

        member x.TryGetFilterIp with get() = ipStack |> List.tryFind (fun ip -> ip.IsInFilter)

        member x.TryCurrentLoc with get() = x.CurrentIp.ToCodeLocation()

        member x.CurrentLoc with get() = x.TryCurrentLoc |> Option.get

        member x.StartingLoc with get() = startingIp.ToCodeLocation() |> Option.get

        member x.CodeLocations with get() =
            ipStack
            |> List.takeWhile (fun ip -> not ip.IsFilter)
            |> List.map (fun ip -> ip.ForceCodeLocation())

        member private x.MoveCodeLoc (ip : instructionPointer) =
            match ip.ToCodeLocation() with
            | Some loc when loc.method.HasBody -> currentLoc <- loc
            | _ -> ()

        // -------------------- Prefix context operations --------------------

        member x.PrefixContext with get() = prefixContext

        member x.PushPrefixContext (prefix : prefix) =
            prefixContext <- prefix :: prefixContext

        member x.PopPrefixContext() =
            match prefixContext with
            | prefix :: context ->
                prefixContext <- context
                Some prefix
            | _ -> None

        // -------------------- Stack arrays operations --------------------

        member x.AddStackArray (address : concreteHeapAddress) =
            stackArrays <- PersistentSet.add stackArrays address

        member x.IsStackArray ref =
            match ref.term with
            | HeapRef({term = ConcreteHeapAddress address}, _)
            | Ref(ArrayIndex({term = ConcreteHeapAddress address}, _, _))
            | Ptr(HeapLocation({term = ConcreteHeapAddress address}, _), _, _) ->
                PersistentSet.contains address stackArrays
            | _ -> false

        // -------------------- Level operations --------------------

        member x.IncrementLevel codeLocation =
            let oldValue = PersistentDict.tryFind level codeLocation |> Option.defaultValue 0u
            level <- PersistentDict.add codeLocation (oldValue + 1u) level

        member x.DecrementLevel codeLocation =
            let oldValue = PersistentDict.tryFind level codeLocation
            match oldValue with
            | Some value when value = 1u ->
                level <- PersistentDict.remove codeLocation level
            | Some value when value > 0u ->
                level <- PersistentDict.add codeLocation (value - 1u) level
            | _ -> ()

        member x.ViolatesLevel maxBound =
            match x.TryCurrentLoc with
            | Some currLoc when PersistentDict.contains currLoc level ->
                level[currLoc] >= maxBound
            | _ -> false

        member x.LevelOfLocation loc =
            if PersistentDict.contains loc level then level[loc] else 0u

        member x.Level with get() = Level.levelToUnsignedInt level

        // -------------------- History operations --------------------

        member x.AddLocationToHistory (loc : codeLocation) =
            history <- Set.add loc history

        // -------------------- EvaluationStack operations --------------------

        member x.SetEvaluationStack evaluationStack =
            state.evaluationStack <- evaluationStack

        member x.ClearEvaluationStackLastFrame() =
            state.evaluationStack <- EvaluationStack.ClearActiveFrame state.evaluationStack

        member x.Push v =
            match v.term with
            | Nop -> internalfail "pushing 'NOP' value onto evaluation stack"
            | _ ->
                state.evaluationStack <- EvaluationStack.Push v state.evaluationStack

        member x.PushMany vs =
            if List.contains (Nop()) vs then
                internalfail "pushing 'NOP' value onto evaluation stack"
            state.evaluationStack <- EvaluationStack.PushMany vs state.evaluationStack

        member x.Peek() = EvaluationStack.Pop state.evaluationStack |> fst

        member x.Peek2() =
            let stack = state.evaluationStack
            let arg2, stack = EvaluationStack.Pop stack
            let arg1, _ = EvaluationStack.Pop stack
            arg2, arg1

        member x.Pop() =
            let v, evaluationStack = EvaluationStack.Pop state.evaluationStack
            state.evaluationStack <- evaluationStack
            v

        member x.Pop2() =
            let arg2 = x.Pop()
            let arg1 = x.Pop()
            arg2, arg1

        member x.Pop3() =
            let arg3 = x.Pop()
            let arg2 = x.Pop()
            let arg1 = x.Pop()
            arg3, arg2, arg1

        member x.PopMany (count : int) =
            let parameters, evaluationStack = EvaluationStack.PopMany count state.evaluationStack
            state.evaluationStack <- evaluationStack
            parameters

        member x.PushNewObjForValueTypes() =
            let ref = x.Pop()
            let value = Memory.Read state ref
            x.Push value

        // -------------------- Filter result operations --------------------

        member x.FilterResult with get() = filterResult

        member x.SetFilterResult (value : term) =
            filterResult <- Some value

        member x.ClearFilterResult() =
            filterResult <- None

        // -------------------- Targets operations --------------------

        member x.Targets with get() = targets

        member x.AddTarget target =
            let prev = targets
            targets <- Set.add target prev
            prev.Count <> targets.Count

        member x.RemoveTarget target =
            let prev = targets
            targets <- Set.remove target prev
            prev.Count <> targets.Count

        member x.ClearTargets() =
            targets <- Set.empty

        // -------------------- Memory interaction --------------------

        member val State = state

        member x.PopFrame() =
            Memory.PopFrame state
            let ip = List.tail ipStack
            ipStack <- ip
            match ip with
            | ip :: _ -> x.MoveCodeLoc ip
            | [] -> ()

        member x.Read ref =
            Memory.ReadUnsafe errorReporter.Value state ref

        member x.ReadField term field =
            Memory.ReadFieldUnsafe errorReporter.Value state term field

        member x.ReadIndex term index valueType =
            Memory.ReadArrayIndexUnsafe errorReporter.Value state term index valueType

        member x.Write ref value =
            let states = Memory.WriteUnsafe errorReporter.Value state ref value
            List.map x.ChangeState states

        member x.WriteClassField ref field value =
            let states = Memory.WriteClassFieldUnsafe errorReporter.Value state ref field value
            List.map x.ChangeState states

        member x.WriteStructField term field value =
            Memory.WriteStructFieldUnsafe errorReporter.Value state term field value

        member x.WriteIndex term index value valueType =
            let states = Memory.WriteArrayIndexUnsafe errorReporter.Value state term index value valueType
            List.map x.ChangeState states

        // -------------------------- Branching --------------------------

        member x.GuardedApplyCIL term (f : cilState -> term -> ('a list -> 'b) -> 'b) (k : 'a list -> 'b) =
            GuardedStatedApplyk
                (fun state term k -> f (x.ChangeState state) term k)
                x.State term id (List.concat >> k)

        member x.StatedConditionalExecutionCIL conditionInvocation thenBranch elseBranch k =
            StatedConditionalExecution x.State conditionInvocation
                (fun state k -> thenBranch (x.ChangeState state) k)
                (fun state k -> elseBranch (x.ChangeState state) k)
                (fun x y -> [x; y])
                (List.concat >> k)

        member x.BranchOnNullCIL term thenBranch elseBranch k =
            x.StatedConditionalExecutionCIL
                (fun state k -> k (IsNullReference term, state))
                thenBranch
                elseBranch
                k

        // -------------------- Changing inner state --------------------

        member private x.DumpSectionValue section value (sb : StringBuilder) =
            let sb = Utils.PrettyPrinting.dumpSection section sb
            Utils.PrettyPrinting.appendLine sb value

        member private x.DumpIpStack (ipStack : ipStack) =
            List.fold (fun acc entry -> $"{acc}\n{entry}") "" ipStack

        member private x.Dump() : string =
            let sb = StringBuilder()
            let sb = x.DumpSectionValue "Starting ip" $"{startingIp}" sb
            let sb = x.DumpSectionValue "IP" (x.DumpIpStack ipStack) sb
            let sb = x.DumpSectionValue "IIE" $"{iie}" sb
            let sb = x.DumpSectionValue "Initial EvaluationStack Size" $"{initialEvaluationStackSize}" sb
            let sb = Utils.PrettyPrinting.dumpDict "Level" id toString id sb level
            let sb = x.DumpSectionValue "State" (Print.Dump state) sb
            if sb.Length = 0 then "<EmptyCilState>" else sb.ToString()

        // -------------------- Changing inner state --------------------

        member x.Copy(state : state) =
            cilState(
                ipStack, prefixContext, currentLoc, stackArrays, errorReported, filterResult, iie, level,
                initialEvaluationStackSize, stepsNumber, suspended, targets, history, entryMethod, startingIp, state
            )

        // This function copies cilState, instead of mutation
        member x.ChangeState state' : cilState =
            if LanguagePrimitives.PhysicalEquality state' state then x
            else x.Copy(state)

        // -------------------- Steps number --------------------

        member x.StepsNumber with get() = stepsNumber

        member x.IncrementStepsNumber() =
            stepsNumber <- stepsNumber + 1u

        // -------------------- Overriding methods --------------------

        override x.ToString() = System.String.Empty

        override x.Equals(other : obj) = System.Object.ReferenceEquals(x, other)

        override x.GetHashCode() = System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode x

        interface IGraphTrackableState with
            override this.CodeLocation = currentLoc
            override this.CallStack = Memory.StackTrace state.stack |> List.map (fun m -> m :?> Method)

module CilStateOperations =
    open CilState

    type cilStateComparer() =
        interface IComparer<cilState> with
            override _.Compare(x : cilState, y : cilState) =
                x.GetHashCode().CompareTo(y.GetHashCode())

    let mkCilStateHashComparer = cilStateComparer()
