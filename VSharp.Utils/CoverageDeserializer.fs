namespace VSharp

open System
open System.Diagnostics
open System.Runtime.InteropServices
open System.Runtime.Serialization
open System.Text

type CoverageLocation = {
    assemblyName: string
    moduleName: string
    methodToken: int
    offset: int
}

type CoverageReport = {
    threadId: int
    coverageLocations: CoverageLocation[]
}

#nowarn "9"
[<Struct; CLIMutable; DataContract>]
[<StructLayout(LayoutKind.Explicit, Size = 20)>]
type RawCoverageLocation = {
    [<FieldOffset(00); DataMember(Order = 1)>] offset: uint32
    [<FieldOffset(04); DataMember(Order = 2)>] event: int32
    [<FieldOffset(08); DataMember(Order = 3)>] methodId: int32
    [<FieldOffset(12); DataMember(Order = 4)>] threadId: uint64
}

type RawMethodInfo = {
    methodToken: uint32 
    moduleName: string
    assemblyName: string
}

type RawCoverageReport = {
    threadId: int
    rawCoverageLocations: RawCoverageLocation[]
}

type RawCoverageReports = {
    methods: System.Collections.Generic.Dictionary<int, RawMethodInfo>
    reports: RawCoverageReport[]
}

module CoverageDeserializer =

    let mutable private data = [||]
    let mutable private dataOffset = 0
    let mutable private deserializedMethods = System.Collections.Generic.Dictionary()


    let timers = System.Collections.Generic.Dictionary<string, Stopwatch>()

    let withTimeMeasure name f =
        let stopwatch = 
            match timers.TryGetValue(name) with
            | true, stopwatch -> stopwatch
            | false, _ ->
                let stopwatch = Stopwatch()
                timers.Add(name, stopwatch)
                stopwatch

        stopwatch.Start()
        let result = f ()
        stopwatch.Stop ()

        result

    let printMeasures () =
        for k in timers.Keys do
            Logger.error $"{k}: {double timers[k].ElapsedMilliseconds / 1000.0}"

    let trace x =
        #if TRACEDESERIALIZATION
        withTimeMeasure "trace" <| fun () -> 
        Logger.traceWithTag Logger.deserializationTraceTag x
        #endif
        ()

    let traceValue name x =
        #if TRACEDESERIALIZATION
        withTimeMeasure "traceValue" <| fun () -> 
        Logger.traceWithTag Logger.deserializationTraceTag $"{name}: {x}"
        x
        #endif
        x

    let inline private increaseOffset i =
        dataOffset <- dataOffset + i

    let inline private readInt32 () =
        withTimeMeasure "readInt32" <| fun () -> 
        let result = BitConverter.ToInt32(data, dataOffset)
        increaseOffset sizeof<int32>
        result

    let inline private readUInt32 () =
        withTimeMeasure "readUInt32" <| fun () -> 
        let result = BitConverter.ToUInt32(data, dataOffset)
        increaseOffset sizeof<uint32>
        result

    let inline private readUInt64 () =
        withTimeMeasure "readUInt64" <| fun () -> 
        let result = BitConverter.ToUInt64(data, dataOffset)
        increaseOffset sizeof<uint64>
        result

    let inline private readString () =
        withTimeMeasure "readString" <| fun () -> 
        let size = readUInt32 () |> int
        let result = Array.sub data dataOffset (2 * size - 2)
        increaseOffset (2 * size)
        let result = Encoding.Unicode.GetString(result)
        result

    let inline private deserializeMethodData () =
        trace "Deserialize method data:"
        let methodToken = readUInt32 () |> traceValue "methodToken"
        let assemblyName = readString () |> traceValue "assemblyName"
        let moduleName = readString () |> traceValue "moduleName"
        { methodToken = methodToken; assemblyName = assemblyName; moduleName = moduleName }

    let inline private deserializeCoverageInfo () =
        trace "Deserialize coverage info:"
        let offset = readUInt32 () |> traceValue "offset"
        let event = readInt32 () |> traceValue "event"
        let methodId = readInt32 () |> traceValue "methodId"
        let threadId = readUInt64 () |> traceValue "threadId"
        { offset = offset; event = event; methodId = methodId; threadId = threadId }

    let inline private deserializeArray elementDeserializer =
        trace "Deserialize array:"
        let arraySize = readInt32 () |> traceValue "Array size"
        Array.init arraySize (fun _ -> elementDeserializer ())

    let inline private deserializeDictionary keyDeserializer elementDeserializer =
        trace "Deserialize dictionary:"
        let dictionarySize = readInt32 () |> traceValue "Dict size"
        let dictionary = System.Collections.Generic.Dictionary()
        for _ in 0..dictionarySize - 1 do
            let index = keyDeserializer ()
            let element = elementDeserializer ()
            dictionary.Add(index, element)
        dictionary

    let inline private deserializeCoverageInfoFast () =
        withTimeMeasure "deserializeCoverageInfoFast" <| fun () -> 
        trace "Deserialize coverage info fast"
        let count = readInt32 () |> traceValue "Reports count"
        let bytesCount = sizeof<RawCoverageLocation> * count
        let targetBytes = Array.zeroCreate bytesCount
        let targetSpan = Span(targetBytes)
        data.AsSpan().Slice(dataOffset, bytesCount).CopyTo(targetSpan)
        let span = MemoryMarshal.Cast<byte, RawCoverageLocation> targetSpan
//        for x in span do
//            trace "------"
//            trace $"offset: {x.offset}"
//            trace $"event: {x.event}"
//            trace $"method id: {x.methodId}"
//            trace $"thread id: {x.threadId}"
        increaseOffset bytesCount
        span.ToArray()

    let private deserializeRawReport () =
        let threadId = readInt32 () |> traceValue "Thread id"
        let threadAborted = readInt32 () |> traceValue "Aborted"
        if threadAborted = 1 then
            {
                threadId = threadId
                rawCoverageLocations = [||]
            }
        else
            {
                threadId = threadId
                rawCoverageLocations = deserializeCoverageInfoFast ()
            }

    let private deserializeRawReports () =
        trace "Start method deserialization"
        let methods = deserializeDictionary readInt32 deserializeMethodData
        trace "Methods deserialized"
        let reports = deserializeArray deserializeRawReport
        trace "Reports deserialized"
        {
            methods = methods
            reports = reports
        }

    let private startNewDeserialization bytes =
        trace "Start new deserialization"
        data <- bytes
        dataOffset <- 0

    let private getMethods () =
        deserializedMethods <- deserializeDictionary readInt32 deserializeMethodData

    let getRawReports bytes =
        try
            startNewDeserialization bytes
            let result = deserializeRawReports ()
            printMeasures ()
            result
        with
        | e ->
            Logger.error $"{dataOffset}"
            Logger.error $"{e.Message}\n\n{e.StackTrace}"
            failwith "CoverageDeserialization failed!"

    let reportsFromRawReports (rawReports: RawCoverageReports) =

        let toLocation (x: RawCoverageLocation) =
            let method = rawReports.methods[x.methodId]
            {
                assemblyName = method.assemblyName
                moduleName = method.moduleName
                methodToken = method.methodToken |> int
                offset = x.offset |> int
            }

        let toReport (x: RawCoverageReport) =
            {
                threadId = x.threadId
                coverageLocations = x.rawCoverageLocations |> Array.map toLocation 
            }

        rawReports.reports |> Array.map toReport
