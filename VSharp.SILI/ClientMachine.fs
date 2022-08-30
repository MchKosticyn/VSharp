namespace VSharp.Concolic

open System
open System.Collections.Generic
open System.Diagnostics
open System.IO
open System.Reflection.Emit
open System.Runtime.InteropServices
open VSharp
open VSharp.Core
open VSharp.Interpreter.IL

[<AllowNullLiteral>]
type ClientMachine(entryPoint : Method, cmdArgs : string[] option, requestMakeStep : cilState -> unit, cilState : cilState) =
    let extension =
        if RuntimeInformation.IsOSPlatform(OSPlatform.Windows) then ".dll"
        elif RuntimeInformation.IsOSPlatform(OSPlatform.Linux) then ".so"
        elif RuntimeInformation.IsOSPlatform(OSPlatform.OSX) then ".dylib"
        else __notImplemented__()
    let pathToClient = "libvsharpConcolic" + extension
    let pathToTmp = sprintf "%s%c" (Directory.GetCurrentDirectory()) Path.DirectorySeparatorChar
    let tempTest (id : int) = sprintf "%sstart%d.vst" pathToTmp id
    [<DefaultValue>] val mutable probes : probes
    [<DefaultValue>] val mutable instrumenter : Instrumenter

    let cilState : cilState = cilState

    let initSymbolicFrame (method : Method) = // TODO: unify with InitFunctionFrame
        let parameters = method.Parameters |> Seq.map (fun param ->
            (ParameterKey param, None, param.ParameterType)) |> List.ofSeq
        let locals =
            match method.LocalVariables with
            | null -> []
            | lv ->
                lv
                |> Seq.map (fun local -> (LocalVariableKey(local, method), None, local.LocalType))
                |> List.ofSeq
        let parametersAndThis =
            if method.HasThis then
                (ThisKey method, None, method.DeclaringType) :: parameters // TODO: incorrect type when ``this'' is Ref to stack
            else parameters
        Memory.NewStackFrame cilState.state (Some method) (parametersAndThis @ locals)
        // NOTE: initializing all ipStack frames with -1 offset, because real offset of previous frames is unknown,
        //       but length of ipStack must be equal to length of stack frames
        let ip = Offset.from -1 |> ipOperations.instruction method
        CilStateOperations.pushToIp ip cilState

//    let bindNewCilState newState =
//        if not <| LanguagePrimitives.PhysicalEquality cilState newState then
//            cilState.suspended <- false
//            newState.suspended <- true
//            cilState <- newState

    let metadataSizeOfAddress state address =
        let t = TypeOfAddress state address
        CSharpUtils.LayoutUtils.MetadataSize t

    static let mutable clientId = 0

    let mutable callIsSkipped = false
    let mutable mainReached = false
    let mutable operands : list<_> = List.Empty
    let environment (method : Method) pipePath =
        let result = ProcessStartInfo()
        let profiler = sprintf "%s%c%s" (Directory.GetCurrentDirectory()) Path.DirectorySeparatorChar pathToClient
        result.EnvironmentVariables.["CORECLR_PROFILER"] <- "{2800fea6-9667-4b42-a2b6-45dc98e77e9e}"
        result.EnvironmentVariables.["CORECLR_ENABLE_PROFILING"] <- "1"
        result.EnvironmentVariables.["CORECLR_PROFILER_PATH"] <- profiler
        result.EnvironmentVariables.["CONCOLIC_PIPE"] <- pipePath
        result.WorkingDirectory <- Directory.GetCurrentDirectory()
        result.FileName <- "dotnet"
        result.UseShellExecute <- false
        result.RedirectStandardOutput <- true
        result.RedirectStandardError <- true
        if method.IsEntryPoint then
            result.Arguments <- method.Module.Assembly.Location
        else
            let runnerPath = "VSharp.TestRunner.dll"
            result.Arguments <- sprintf "%s %s %O" runnerPath (tempTest clientId) false
        result

    [<DefaultValue>] val mutable private communicator : Communicator

    let mutable concolicProcess = new Process()
    let mutable isRunning = false
    let concolicStackKeys = Dictionary<stackKey, UIntPtr>()
    let registerStackKeyAddress stackKey (address : UIntPtr) =
        if concolicStackKeys.ContainsKey stackKey then
            // Updating address, because same function with the same stack key may be called on different objects
            concolicStackKeys[stackKey] <- address
        else concolicStackKeys.Add(stackKey, address)
    let getStackKeyAddress stackKey =
        concolicStackKeys[stackKey]

    member private x.CreateTest() =
        assert(entryPoint.HasBody)
        try
            match TestGenerator.state2test false entryPoint cmdArgs cilState false with
            | Some test -> test.Serialize(tempTest clientId)
            | None -> raise <| Exception "Could not generate start-up test"
        with :? InsufficientInformationException as e ->
            cilState.iie <- Some e
            reraise()

    member x.Spawn() =
        x.CreateTest()
        let pipe, pipePath =
            if RuntimeInformation.IsOSPlatform(OSPlatform.Windows) then
                let pipe = sprintf "concolic_fifo_%d.pipe" clientId
                let pipePath = sprintf "\\\\.\\pipe\\%s" pipe
                pipe, pipePath
            else
                let pipeFile = sprintf "%sconcolic_fifo_%d.pipe" pathToTmp clientId
                pipeFile, pipeFile
        let env = environment entryPoint pipePath
        x.communicator <- new Communicator(pipe)
        concolicProcess <- Process.Start env
        isRunning <- true
        clientId <- clientId + 1
        concolicProcess.OutputDataReceived.Add <| fun args -> Logger.trace "CONCOLIC OUTPUT: %s" args.Data
        concolicProcess.ErrorDataReceived.Add <| fun args -> Logger.trace "CONCOLIC ERROR: %s" args.Data
        concolicProcess.BeginOutputReadLine()
        concolicProcess.BeginErrorReadLine()
        Logger.info "Successfully spawned pid %d, working dir \"%s\"" concolicProcess.Id env.WorkingDirectory
        cilState.state.concreteMemory <- ConcolicMemory(x.communicator)
        if x.communicator.Connect() then
            x.probes <- x.communicator.ReadProbes()
            x.communicator.SendEntryPoint entryPoint.Module.FullyQualifiedName entryPoint.MetadataToken
            x.communicator.SendCoverageInformation cilState.path
            CilStateOperations.clearCoveragePath cilState
            x.instrumenter <- Instrumenter(x.communicator, (entryPoint :> IMethod).MethodBase, x.probes)
            cilState.concolicStatus <- concolicStatus.Running
            true
        else
            cilState.concolicStatus <- concolicStatus.Done
            false

    member x.Terminate() =
        if isRunning then
            Logger.trace "Terminating ClientMachine with Concolic process"
            concolicProcess.Kill()
            concolicProcess.WaitForExit()
            concolicProcess.Close()
            isRunning <- false

    member x.IsRunning = isRunning

    member private x.MarshallRefFromConcolic baseAddress offset key =
        match key with
        | ReferenceType ->
            let cm = cilState.state.concreteMemory
            let address = cm.GetVirtualAddress baseAddress |> ConcreteHeapAddress
            let typ = TypeOfAddress cilState.state address
            match offset with
            | _ when offset = UIntPtr.Zero && not (Types.IsValueType typ) -> HeapRef address typ
            | _ when offset = UIntPtr.Zero -> HeapRef address typeof<obj>
            | _ ->
                let offset = int offset - metadataSizeOfAddress cilState.state address
                let offset = Concrete offset Types.TLength
                Ptr (HeapLocation(address, typ)) typeof<Void> offset
        | LocalVariable(frame, idx) ->
            let stackKey = Memory.FindLocalVariableByIndex cilState.state (int frame) (int idx)
            registerStackKeyAddress stackKey baseAddress
            let offset = int offset |> MakeNumber
            Ptr (StackLocation stackKey) typeof<Void> offset
        | Parameter(frame, idx) ->
            let stackKey = Memory.FindParameterByIndex cilState.state (int frame) (int idx)
            registerStackKeyAddress stackKey baseAddress
            let offset = int offset |> MakeNumber
            Ptr (StackLocation stackKey) typeof<Void> offset
        | Statics(staticFieldID) ->
            let fieldInfo = x.instrumenter.StaticFieldByID (int staticFieldID)
            let fieldOffset = CSharpUtils.LayoutUtils.GetFieldOffset fieldInfo
            let offset = MakeNumber (fieldOffset + int offset)
            Ptr (StaticLocation fieldInfo.DeclaringType) typeof<Void> offset
        | TemporaryAllocatedStruct(frameNumber, frameOffset) ->
            let frameOffset = int frameOffset
            let frame = List.item (List.length cilState.ipStack - int frameNumber) cilState.ipStack
            let method = CilStateOperations.methodOf frame
            let frameOffset = Offset.from frameOffset
            let opCode, callee = method.ParseCallSite frameOffset
            assert(opCode = OpCodes.Newobj)
            let stackKey = TemporaryLocalVariableKey callee.DeclaringType
            let offset = int offset |> MakeNumber
            Ptr (StackLocation stackKey) typeof<Void> offset

    member private x.CalleeArgTypesIfPossible() =
        let m = Memory.GetCurrentExploringFunction cilState.state :?> Method
        let offset = CilStateOperations.currentOffset cilState
        match offset with
        | Some offset ->
            let opCode, callee = m.ParseCallSite offset
            assert(opCode = OpCodes.Call || opCode = OpCodes.Callvirt || opCode = OpCodes.Newobj)
            let isNewObj = opCode = OpCodes.Newobj
            let argTypes = callee.GetParameters() |> Array.map (fun p -> p.ParameterType)
            if Reflection.hasThis callee && not isNewObj then
                Array.append [|callee.DeclaringType|] argTypes
            else argTypes
        | None -> internalfail "CalleeParamsIfPossible: could not get offset"

    member private x.ResolveMethod cilState this resolvedToken unresolvedToken =
        let topMethod = Memory.GetCurrentExploringFunction cilState.state :?> Method
        let resolved = topMethod.ResolveMethod unresolvedToken
        if resolved.IsVirtual && IsNullReference this <> True then
            let declaringType = x.TypeOfConcolicThisRef this
            let methods = Reflection.getAllMethods declaringType
            let method = methods |> Array.tryFind (fun x -> x.MetadataToken = resolvedToken)
            match method with
            | Some m when m.IsGenericMethodDefinition ->
                m.MakeGenericMethod(resolved.GetGenericArguments()) :> Reflection.MethodBase
            | Some m -> m :> Reflection.MethodBase
            | None -> internalfailf "ResolveMethod: unable to find method for 'this' type %O" declaringType
        else resolved

    member private x.TypeOfConcolicThisRef thisRef =
        match thisRef.term with
        | Ptr(StackLocation key, _ , offset) when offset = MakeNumber 0 -> key.TypeOfLocation
        | Ptr(HeapLocation (_, typ), _, offset) when offset = MakeNumber 0 -> typ
        | HeapRef(_, typ) -> typ
        | Ptr _ -> internalfailf "TypeOfConcolicThisRef: non-zero offset pointer case is not implemented %O" thisRef
        | _ -> internalfailf "TypeOfConcolicThisRef: unexpected 'this' %O" thisRef

    member x.SynchronizeStates (c : execCommand) =
        Logger.trace "Synchronizing states with Concolic"
        let concreteMemory = cilState.state.concreteMemory
        Array.iter concreteMemory.DeleteAddress c.deletedAddresses

        let delegateTypes = Dictionary<UIntPtr, Type>()

        let allocateAddress address typ =
            if TypeUtils.isSubtypeOrEqual typ typedefof<Delegate> then
                delegateTypes.Add(address, typ)
            else
                let concreteAddress = lazy(Memory.AllocateEmptyType cilState.state typ)
                concreteMemory.Allocate address concreteAddress
        Array.iter2 allocateAddress c.newAddresses c.newAddressesTypes

        let allocateDelegate (actionPtr, functionId, closurePtr) =
            let closureAddress = concreteMemory.GetVirtualAddress closurePtr |> ConcreteHeapAddress
            let closureRef = HeapRef closureAddress (TypeOfAddress cilState.state closureAddress)
            let methodInfo = x.instrumenter.FunctionByID functionId
            let lambdaType = delegateTypes[actionPtr]
            let lambda = Lambdas.make (methodInfo, closureRef) lambdaType
            let concreteAddress = lazy(Memory.AllocateDelegate cilState.state lambda)
            concreteMemory.Allocate actionPtr concreteAddress
        Array.iter allocateDelegate c.delegates

        let exceptionRegister =
            // NOTE: is exception is symbolic, it was already raised in SILI, so ignoring exception ref
            match c.exceptionRegister, cilState.state.exceptionsRegister.ExceptionTerm with
            | UnhandledConcolic(exceptionRegister, exceptionIsConcrete), _ when exceptionIsConcrete ->
                Unhandled(x.MarshallRefFromConcolic exceptionRegister UIntPtr.Zero ReferenceType)
            | UnhandledConcolic _, Some exc ->
                Unhandled exc
            | CaughtConcolic(exceptionRegister, exceptionIsConcrete), _ when exceptionIsConcrete ->
                Caught(x.MarshallRefFromConcolic exceptionRegister UIntPtr.Zero ReferenceType)
            | CaughtConcolic _, Some exc ->
                Caught exc
            | NoExceptionConcolic, _ -> NoException
            | exceptionRegister, _ -> internalfailf "SynchronizeStates: unexpected exception register %O" exceptionRegister
        cilState.state.exceptionsRegister <- exceptionRegister

        let toPop = int c.callStackFramesPops
        if toPop > 0 then CilStateOperations.popFramesOf toPop cilState

        assert(Memory.CallStackSize cilState.state > 0)
        let initFrame (resolved, unresolved) (addr, offset, k) =
            let this = x.MarshallRefFromConcolic addr offset k
            let resolved = x.ResolveMethod cilState this resolved unresolved
            initSymbolicFrame (Application.getMethod resolved)
        Array.iter2 initFrame c.newCallStackFrames c.thisAddresses

        assert(List.length cilState.ipStack = List.length c.ipStack)
        let changeIP ip offset = CilStateOperations.changeIpOffset ip (Offset.from offset)
        cilState.ipStack <- List.map2 changeIP cilState.ipStack (List.rev c.ipStack)
        if c.isTerminatedByException then
            CilStateOperations.setCurrentIp (SearchingForHandler([], [])) cilState

        let evalStack = EvaluationStack.PopMany (int c.evaluationStackPops) cilState.state.evaluationStack |> snd
        let argTypes = lazy x.CalleeArgTypesIfPossible()
        let mutable maxIndex = 0
        let createTerm i operand =
            match operand with
            | NumericOp(evalStackArgType, content) ->
                match evalStackArgType with
                | evalStackArgType.OpSymbolic ->
                    let idx = int content
                    maxIndex <- max maxIndex (idx + 1)
                    EvaluationStack.GetItem idx cilState.state.evaluationStack
                | evalStackArgType.OpI4 ->
                    Concrete (int content) TypeUtils.int32Type
                | evalStackArgType.OpI8 ->
                    Concrete content TypeUtils.int64Type
                | evalStackArgType.OpR4 ->
                    Concrete (BitConverter.Int64BitsToDouble content |> float32) TypeUtils.float32Type
                | evalStackArgType.OpR8 ->
                    Concrete (BitConverter.Int64BitsToDouble content) TypeUtils.float64Type
                | _ -> __unreachable__()
            | PointerOp(baseAddress, offset, key) ->
                x.MarshallRefFromConcolic baseAddress offset key
            | EmptyOp ->
                let argTypes = argTypes.Value
                Memory.DefaultOf argTypes[i]
        let newEntries = c.evaluationStackPushes |> Array.mapi createTerm
        let _, evalStack = EvaluationStack.PopMany maxIndex evalStack
        operands <- Array.toList newEntries
        let evalStack = Array.fold (fun stack x -> EvaluationStack.Push x stack) evalStack newEntries
        cilState.state.evaluationStack <- evalStack
        cilState.lastPushInfo <- None
        cilState.path <- c.newCoveragePath @ cilState.path

    member x.State with get() = cilState

    member x.ExecCommand() =
        Logger.trace "Reading next command..."
        try
            match x.communicator.ReadCommand() with
            | Instrument methodBody ->
                if int methodBody.properties.token = entryPoint.MetadataToken && methodBody.moduleName = entryPoint.Module.FullyQualifiedName then
                    mainReached <- true
                let methodBody =
                    if mainReached then
                        Logger.trace "Got instrument command! bytes count = %d, max stack size = %d, eh count = %d" methodBody.il.Length methodBody.properties.maxStackSize methodBody.ehs.Length
                        x.instrumenter.Instrument methodBody
                    else x.instrumenter.Skip methodBody
                x.communicator.SendMethodBody methodBody
                true
            | ExecuteInstruction c ->
                Logger.trace "Got execute instruction command! Offset = %x" c.ipStack.Head
                x.SynchronizeStates c
                cilState.concolicStatus <- concolicStatus.Waiting
                requestMakeStep cilState
                true
            | Terminate ->
                Logger.trace "Got terminate command!"
                isRunning <- false
                false
        with
        | :? IOException ->
            Logger.trace "exception caught in concolic machine, waiting process to terminate..."
            if concolicProcess.WaitForExit(1000) |> not then x.Terminate()
            Logger.trace "process terminated, exit code = %d" concolicProcess.ExitCode
            reraise()

    member private x.ConcreteToObj term =
        let evalRefType baseAddress offset typ =
            // NOTE: deserialization of object location is not needed, because Concolic needs only address and offset
            match baseAddress, offset.term with
            | HeapLocation({term = ConcreteHeapAddress address} as a, _), Concrete(offset, _) ->
                let address = cilState.state.concreteMemory.GetPhysicalAddress address
                let offset = offset :?> int + metadataSizeOfAddress cilState.state a
                assert(offset > 0)
                let obj = (address, offset) :> obj
                Some (obj, typ)
            | StackLocation stackKey, Concrete(offset, _) ->
                let address = getStackKeyAddress stackKey
                let obj = (address, offset :?> int) :> obj
                Some (obj, typ)
            | StaticLocation _, Concrete _ ->
                internalfail "Unmarshalling for ptr on static location is not implemented!"
            | _ -> None
        match term with
        | {term = Concrete(obj, typ)} -> Some (obj, typ)
        | NullRef t -> Some (null, t)
        | {term = HeapRef({term = ConcreteHeapAddress address}, typ)} ->
            let address = cilState.state.concreteMemory.GetPhysicalAddress address
            let content = (address, 0) :> obj
            Some (content, typ)
        | {term = Ref address} ->
            let baseAddress, offset = AddressToBaseAndOffset address
            evalRefType baseAddress offset (TypeOf term)
        | {term = Ptr(baseAddress, sightType, offset)} ->
            evalRefType baseAddress offset (sightType.MakePointerType())
        | _ -> None

    member private x.EvalOperands cilState =
        // NOTE: if there are no branching, TryGetModel forces solver to create model
        // NOTE: this made to check communication between Concolic and SILI
        // TODO: after all checks, change this to 'cilState.state.model'
//        match TryGetModel cilState.state with
//        | Some model ->
//            let concretizedOps = operands |> List.choose (model.Eval >> x.ConcreteToObj)
//            if List.length operands <> List.length concretizedOps then None
//            else
//                bindNewCilState cilState
//                Some concretizedOps
//        | None -> None
        None

    member x.StepDone (steppedStates : cilState list) =
        let methodEnded = CilStateOperations.methodEnded cilState
        if methodEnded then
            let method = CilStateOperations.currentMethod cilState
            if method.IsInternalCall then callIsSkipped <- true
        else
            let concretizedOps =
                if callIsSkipped then Some List.empty
                else steppedStates |> List.tryPick x.EvalOperands
            // TODO: move 'connect/disconnect' to 'ConcolicPool.StepDone'
            let disconnectConcolic cilState =
                // TODO: need to detach concolic memory?
                cilState.concolicStatus <- concolicStatus.Detached
            steppedStates |> List.iter disconnectConcolic
            let connectConcolic cilState =
                cilState.concolicStatus <- concolicStatus.Running
            // TODO: unify stopping execution with searcher
            let isIIEState = CilStateOperations.isIIEState cilState
            let stoppedByException = CilStateOperations.stoppedByException cilState
            // NOTE: if current ip is end of entryPoint, test was already generated, so concolic can be terminated
            if CilStateOperations.currentIp cilState = Exit entryPoint || isIIEState || stoppedByException then
                // TODO: Need second bypass after searching for handler failed
                x.Terminate()
            else
                connectConcolic cilState
                let lastPushInfo cilState =
                    match cilState.lastPushInfo with
                    | Some x when IsConcrete x ->
                        CilStateOperations.pop cilState |> ignore
                        Some true
                    | Some _ -> Some false
                    | None -> None
                let updatePathLastPush cilState =
                    match cilState.path with
                    | head::tail ->
                        let lastPush = lastPushInfo cilState |> x.communicator.SerializeStackPush
                        cilState.path <- {head with stackPush = lastPush}::tail
                    | [] -> __unreachable__()
                List.iter updatePathLastPush steppedStates
                let internalCallResult =
                    match cilState.lastPushInfo with
                    | Some res when callIsSkipped ->
                        x.ConcreteToObj res
                    | _ -> None
                assert(not cilState.path.IsEmpty)
                let currentStackPush =
                    assert(cilState.path.Length > 0)
                    cilState.path.Head.stackPush
                x.communicator.SendExecResponse concretizedOps internalCallResult currentStackPush
                callIsSkipped <- false

[<AllowNullLiteral>]
type ConcolicPool(entryPoint, cmdArgs, requestMakeStep : cilState -> unit, reportIncomplete : cilState -> unit, width : int (* How many machines we can run synchronously *)) =
    let queue = Queue<cilState>()
    let activeMachines = Dictionary<cilState, ClientMachine>()

    let activate() =
        let delta = width - activeMachines.Count
        if delta > 0 then // If there is space to start new machines
            while queue.Count > 0 && activeMachines.Count < width do
                let cilState = queue.Dequeue()
                let machine = ClientMachine(entryPoint, cmdArgs, requestMakeStep(*, onMachineTerminated*), cilState)
                try
                    if machine.Spawn() then
                        activeMachines.Add(cilState, machine)
                    else
                        internalfail "Unable to spawn concolic machine!"
                with :? InsufficientInformationException ->
                    reportIncomplete cilState
            true
        else false

    let onMachineTerminated (machine : ClientMachine) =
        let removed = activeMachines.Remove machine.State
        assert removed
        machine.State.concolicStatus <- concolicStatus.Done
        machine.Terminate()
        activate()

    member x.Schedule state =
        queue.Enqueue state
        assert(state.concolicStatus = concolicStatus.Detached)
        state.concolicStatus <- concolicStatus.Queued
        Logger.trace "Scheduling new state with path (%s)" (Coverage.dump state.path)
        activate() |> ignore

    member x.StepDone (fromState : cilState, toStates : cilState list) =
        let concolicMachine : ClientMachine ref = ref null
        if activeMachines.TryGetValue(fromState, concolicMachine) then
            let machine = concolicMachine.Value
            machine.StepDone toStates

    member x.ExecCommand() =
        assert(width = 1)
        activeMachines.Count = 1 &&
            let onlyMachine = Seq.head activeMachines.Values
            onlyMachine.State.concolicStatus = concolicStatus.Running &&
            onlyMachine.ExecCommand() ||
            onMachineTerminated onlyMachine

    member x.IsRunning() =
        assert(width = 1)
        activeMachines.Count = 1 &&
            let onlyMachine = Seq.head activeMachines.Values
            onlyMachine.State.concolicStatus = concolicStatus.Running

    member x.Terminate() =
        queue.Clear()
        for machine in activeMachines.Values do
            machine.Terminate ()
