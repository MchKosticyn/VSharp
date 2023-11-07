namespace VSharp.Fuzzer

open System
open System.Diagnostics
open System.IO
open System.Reflection
open System.Threading
open System.Threading.Tasks
open VSharp
open VSharp.Fuzzer.Communication
open Logger
open VSharp.Fuzzer.Communication.Contracts
open VSharp.Fuzzer.Communication.Services
open VSharp.Fuzzer.Startup

type private SiliStatisticConverter() =
    
    let methods = System.Collections.Generic.Dictionary<uint32, Method>()

    member this.GetMethod kek = methods[kek]

    member this.ToSiliStatistic
        (methodInfo: System.Collections.Generic.Dictionary<int, RawMethodInfo>)
        (loc: RawCoverageLocation seq) =

        let getMethod l =
            match methods.TryGetValue(l.methodToken) with
            | true, m -> m
            | false, _ ->
                let methodBase = Reflection.resolveMethodBase l.assemblyName l.moduleName (int l.methodToken)
                let method = Application.getMethod methodBase
                methods.Add (l.methodToken, method)
                method

        let toCodeLocation l =
            {
                offset = LanguagePrimitives.Int32WithMeasure (int l.offset)
                method = methodInfo[l.methodId] |> getMethod
            }

        loc |> Seq.map toCodeLocation

type private TestRestorer () =
    let executionInfo = System.Collections.Generic.Dictionary<int, ExecutionData>()

    member this.TrackExecutionInfo threadId executionData =
        executionInfo[threadId] <- executionData

    // TODO: Add test restoring
    member this.RestoreTest failReportPath =
        ()

type Interactor (
    cancellationToken: CancellationToken,
    outputPath: string,
    saveStatistic,
    onCancelled: unit -> unit
    ) =

    do
        enableTag "Communication" Trace
    // TODO: make options configurable (CLI & Tests) 
    let fuzzerOptions =
        {
            initialSeed = 42
            timeLimitPerMethod = 3000
            arrayMaxSize = 10
            stringMaxSize = 10
        }

    let fuzzerDeveloperOptions =
        {
            logPath = Directory.GetCurrentDirectory()
            redirectStdout = true
            redirectStderr = true
            waitDebuggerAttachedFuzzer = false
            waitDebuggerAttachedCoverageTool = false
            waitDebuggerAttachedOnAssertCoverageTool = false
            sanitizersMode = Disabled
        }

    
    let testRestorer = TestRestorer()
    let mutable fuzzerService = Unchecked.defaultof<IFuzzerService>
    let mutable fuzzerProcess = Unchecked.defaultof<Process>
    let mutable queued = System.Collections.Generic.Queue<MethodBase>()


    let handleExit () =
        let unhandledExceptionPath = $"{Directory.GetCurrentDirectory()}{Path.DirectorySeparatorChar}kek.info"
        if File.Exists(unhandledExceptionPath) then
            testRestorer.RestoreTest unhandledExceptionPath

    let setupFuzzer targetAssemblyPath =
        task {
            try
                do! fuzzerService.SetupOutputDirectory { stringValue = outputPath }
                do! fuzzerService.SetupAssembly { assemblyName = targetAssemblyPath }
            with :? TaskCanceledException -> onCancelled ()
        }

    let fuzzNextMethod () =
        task {
            try
                Logger.traceCommunication "fuzzNextMethod"
                if queued.Count <> 0 then
                    let method = queued.Dequeue()
                    do! fuzzerService.Fuzz {
                        moduleName = method.Module.FullyQualifiedName
                        methodId = method.MetadataToken
                    }
            with :? TaskCanceledException -> onCancelled ()
        }

    let masterProcessService =
        let siliStatisticsConverter = SiliStatisticConverter()

        let onTrackCoverage methods (rawData: RawCoverageReport) =
            let alreadyTracked =
                siliStatisticsConverter.ToSiliStatistic methods rawData.rawCoverageLocations
                |> saveStatistic
            Task.FromResult {
                boolValue = alreadyTracked
            }

        let onTrackExecutionSeed (x: ExecutionData) =
            task {
                testRestorer.TrackExecutionInfo x.threadId x
            } :> Task

        let onFinished () =
            task {
                if queued.Count <> 0 then
                    do! fuzzNextMethod ()
                else
                    return ()
            } :> Task

        MasterProcessService(onTrackCoverage, onTrackExecutionSeed, onFinished)

    let startFuzzer () =
        fuzzerProcess <- startFuzzer fuzzerOptions fuzzerDeveloperOptions 
        fuzzerService <- connectFuzzerService ()
        waitFuzzerForReady fuzzerService

    let startMasterProcess () =
        startMasterProcessService masterProcessService CancellationToken.None
        |> ignore

    let initialize () =
        cancellationToken.Register(fun () ->
        if not fuzzerProcess.HasExited  then
            fuzzerProcess.Kill ()
        ) |> ignore
        startMasterProcess ()

    let rec startFuzzingLoop (targetAssemblyPath: string) =

        let logLoop msg = traceCommunication $"[Loop] {msg}"

        let startFuzzing () =
            task {
                startFuzzer ()
                do! setupFuzzer targetAssemblyPath
                do! fuzzNextMethod ()
            }

        let restartFuzzing () =
            task {
                handleExit ()
                if not fuzzerProcess.HasExited then
                    logLoop "Fuzzer alive, killing"
                    fuzzerProcess.Kill()
                if queued.Count <> 0 then
                    do! startFuzzingLoop targetAssemblyPath
                else
                    logLoop "Has not queued method, no need to restart fuzzer"
            }

        let finish () = fuzzerService.Finish (UnitData())

        let waitForExit () =
            task {
                match fuzzerDeveloperOptions.sanitizersMode with
                | Disabled ->  do! fuzzerProcess.WaitForExitAsync cancellationToken
                | Enabled _ -> do fuzzerProcess.Kill()
            }

        task {
            try
                logLoop "Start fuzzing"
                do! startFuzzing ()
                let mutable cont = true
                while cont do
                    logLoop "Poll fuzzer"
                    do! Task.Delay(100)
                    if queued.Count = 0 then
                        logLoop "All methods was sent, finish loop"
                        cont <- false
                    elif fuzzerProcess.HasExited then
                        logLoop "Has unhandled methods but fuzzer exited, restarting"
                        do! restartFuzzing ()
                Logger.traceCommunication "finish"
                do! finish ()
                do! waitForExit ()
            with
                | :? TaskCanceledException ->
                    logLoop "Cancelled"
                    onCancelled ()
                | :? System.Net.Http.HttpRequestException
                | :? Grpc.Core.RpcException -> 
                    logLoop "GRPC Exception, restarting"
                    do! restartFuzzing ()
        }

    member this.StartFuzzing (targetAssemblyPath: string) (isolated: MethodBase seq) =
        initialize ()
        queued <- System.Collections.Generic.Queue<_>(isolated)
        startFuzzingLoop targetAssemblyPath
