namespace VSharp.Concolic

open System
open System.IO
open System.IO.Pipes
open System.Text
open System.Runtime.InteropServices
open VSharp
open VSharp.Core.API

[<type: StructLayout(LayoutKind.Sequential, Pack=1, CharSet=CharSet.Ansi)>]
type probes = {
    mutable ldarg_0 : uint64
    mutable ldarg_1 : uint64
    mutable ldarg_2 : uint64
    mutable ldarg_3 : uint64
    mutable ldarg_S : uint64
    mutable ldarg : uint64
    mutable ldarga : uint64

    mutable ldloc_0 : uint64
    mutable ldloc_1 : uint64
    mutable ldloc_2 : uint64
    mutable ldloc_3 : uint64
    mutable ldloc_S : uint64
    mutable ldloc : uint64
    mutable ldloca : uint64

    mutable starg_S : uint64
    mutable starg : uint64
    mutable stloc_0 : uint64
    mutable stloc_1 : uint64
    mutable stloc_2 : uint64
    mutable stloc_3 : uint64
    mutable stloc_S : uint64
    mutable stloc : uint64

    mutable ldc : uint64
    mutable dup : uint64
    mutable pop : uint64

    mutable brtrue : uint64
    mutable brfalse : uint64
    mutable switch : uint64

    mutable unOp : uint64
    mutable binOp : uint64
    mutable execBinOp_4 : uint64
    mutable execBinOp_8 : uint64
    mutable execBinOp_f4 : uint64
    mutable execBinOp_f8 : uint64
    mutable execBinOp_p : uint64
    mutable execBinOp_8_4 : uint64
    mutable execBinOp_4_p : uint64
    mutable execBinOp_p_4 : uint64
    mutable execBinOp_4_ovf : uint64
    mutable execBinOp_8_ovf : uint64
    mutable execBinOp_f4_ovf : uint64
    mutable execBinOp_f8_ovf : uint64
    mutable execBinOp_p_ovf : uint64
    mutable execBinOp_8_4_ovf : uint64
    mutable execBinOp_4_p_ovf : uint64
    mutable execBinOp_p_4_ovf : uint64

    mutable ldind : uint64
    mutable stind : uint64
    mutable execStind_I1 : uint64
    mutable execStind_I2 : uint64
    mutable execStind_I4 : uint64
    mutable execStind_I8 : uint64
    mutable execStind_R4 : uint64
    mutable execStind_R8 : uint64
    mutable execStind_ref : uint64

    mutable conv : uint64
    mutable conv_Ovf : uint64

    mutable newarr : uint64
    mutable localloc : uint64
    mutable ldobj : uint64
    mutable ldstr : uint64
    mutable ldtoken : uint64
    mutable stobj : uint64
    mutable initobj : uint64
    mutable ldlen : uint64

    mutable cpobj : uint64
    mutable execCpobj : uint64
    mutable cpblk : uint64
    mutable execCpblk : uint64
    mutable initblk : uint64
    mutable execInitblk : uint64

    mutable castclass : uint64
    mutable isinst : uint64

    mutable box : uint64
    mutable unbox : uint64
    mutable unboxAny : uint64

    mutable ldfld : uint64
    mutable ldflda : uint64
    mutable stfld_4 : uint64
    mutable stfld_8 : uint64
    mutable stfld_f4 : uint64
    mutable stfld_f8 : uint64
    mutable stfld_p : uint64
    mutable stfld_struct : uint64

    mutable ldsfld : uint64
    mutable ldsflda : uint64
    mutable stsfld : uint64

    mutable ldelema : uint64
    mutable ldelem : uint64
    mutable execLdelema : uint64
    mutable execLdelem : uint64

    mutable stelem : uint64
    mutable execStelem_I : uint64
    mutable execStelem_I1 : uint64
    mutable execStelem_I2 : uint64
    mutable execStelem_I4 : uint64
    mutable execStelem_I8 : uint64
    mutable execStelem_R4 : uint64
    mutable execStelem_R8 : uint64
    mutable execStelem_Ref : uint64
    mutable execStelem_Struct : uint64

    mutable ckfinite : uint64
    mutable sizeof : uint64
    mutable ldftn : uint64
    mutable ldvirtftn : uint64
    mutable arglist : uint64
    mutable mkrefany : uint64

    mutable enter : uint64
    mutable enterMain : uint64
    mutable leave : uint64
    mutable leaveMain_0 : uint64
    mutable leaveMain_4 : uint64
    mutable leaveMain_8 : uint64
    mutable leaveMain_f4 : uint64
    mutable leaveMain_f8 : uint64
    mutable leaveMain_p : uint64
    mutable finalizeCall : uint64
    mutable execCall : uint64
    mutable call : uint64
    mutable pushFrame : uint64
    mutable callVirt : uint64
    mutable newobj : uint64
    mutable calli : uint64
    mutable throw : uint64
    mutable rethrow : uint64

    mutable mem_p : uint64
    mutable mem_1_idx : uint64
    mutable mem_2_idx : uint64
    mutable mem_4_idx : uint64
    mutable mem_8_idx : uint64
    mutable mem_f4_idx : uint64
    mutable mem_f8_idx : uint64
    mutable mem_p_idx : uint64
    mutable mem2_4 : uint64
    mutable mem2_8 : uint64
    mutable mem2_f4 : uint64
    mutable mem2_f8 : uint64
//    mutable mem2_p : uint64
    mutable mem2_8_4 : uint64
//    mutable mem2_4_p : uint64
//    mutable mem2_p_1 : uint64
//    mutable mem2_p_2 : uint64
//    mutable mem2_p_4 : uint64
//    mutable mem2_p_8 : uint64
//    mutable mem2_p_f4 : uint64
//    mutable mem2_p_f8 : uint64
//    mutable mem3_p_p_p : uint64
//    mutable mem3_p_p_i1 : uint64
//    mutable mem3_p_p_i2 : uint64
//    mutable mem3_p_p_i4 : uint64
//    mutable mem3_p_p_i8 : uint64
//    mutable mem3_p_p_f4 : uint64
//    mutable mem3_p_p_f8 : uint64
//    mutable mem3_p_i1_p : uint64
    mutable unmem_1 : uint64
    mutable unmem_2 : uint64
    mutable unmem_4 : uint64
    mutable unmem_8 : uint64
    mutable unmem_f4 : uint64
    mutable unmem_f8 : uint64
    mutable unmem_p : uint64

    mutable dumpInstruction : uint64
}
with
    member private x.Probe2str =
        let map = System.Collections.Generic.Dictionary<uint64, string>()
        typeof<probes>.GetFields() |> Seq.iter (fun fld -> map.Add(fld.GetValue x |> unbox, fld.Name))
        map
    member x.AddressToString (address : int64) =
        let result = ref ""
        if x.Probe2str.TryGetValue(uint64 address, result) then "probe_" + !result
        else toString address

[<type: StructLayout(LayoutKind.Sequential, Pack=1, CharSet=CharSet.Ansi)>]
type signatureTokens = {
    mutable void_sig : uint32
    mutable bool_sig : uint32
    mutable void_u1_sig : uint32
    mutable void_u4_sig : uint32
    mutable void_i_sig : uint32
    mutable bool_i_sig : uint32
    mutable bool_u2_sig : uint32
    mutable i1_i1_sig : uint32
    mutable i2_i1_sig : uint32
    mutable i4_i1_sig : uint32
    mutable i8_i1_sig : uint32
    mutable r4_i1_sig : uint32
    mutable r8_i1_sig : uint32
    mutable i_i1_sig : uint32
    mutable void_i_i1_sig : uint32
    mutable void_i_i2_sig : uint32
    mutable void_i_u2_sig : uint32
    mutable void_i_i4_sig : uint32
    mutable void_i_i8_sig : uint32
    mutable void_i_r4_sig : uint32
    mutable void_i_r8_sig : uint32
    mutable void_i_i_sig : uint32
    mutable void_i4_i4_sig : uint32
    mutable void_i4_i_sig : uint32
    mutable void_i8_i4_sig : uint32
    mutable void_i8_i8_sig : uint32
    mutable void_r4_r4_sig : uint32
    mutable void_r8_r8_sig : uint32
    mutable bool_i_i4_sig : uint32
    mutable bool_i_i_sig : uint32
    mutable void_i_i_i_sig : uint32
    mutable void_i_i_i1_sig : uint32
    mutable void_i_i_i2_sig : uint32
    mutable void_i_i_i4_sig : uint32
    mutable void_i_i_i8_sig : uint32
    mutable void_i_i_r4_sig : uint32
    mutable void_i_i_r8_sig : uint32
    mutable void_i_i1_i_sig : uint32
    mutable void_i1_i1_i1_sig : uint32
    mutable void_i2_i1_i1_sig : uint32
    mutable void_i4_i1_i1_sig : uint32
    mutable void_i8_i1_i1_sig : uint32
    mutable void_r4_i1_i1_sig : uint32
    mutable void_r8_i1_i1_sig : uint32
    mutable void_i_i1_i1_sig : uint32
    mutable void_token_u2_bool_u4_u4_sig : uint32
    mutable void_offset_sig : uint32
    mutable void_u1_offset_sig : uint32
    mutable void_u2_offset_sig : uint32
    mutable void_i4_offset_sig : uint32
    mutable void_i8_offset_sig : uint32
    mutable void_r4_offset_sig : uint32
    mutable void_r8_offset_sig : uint32
    mutable void_i_offset_sig : uint32
    mutable void_token_offset_sig : uint32
    mutable void_i_i1_offset_sig : uint32
    mutable void_i_i2_offset_sig : uint32
    mutable void_i_i4_offset_sig : uint32
    mutable void_i_i8_offset_sig : uint32
    mutable void_i_r4_offset_sig : uint32
    mutable void_i_r8_offset_sig : uint32
    mutable void_i_i_offset_sig : uint32
    mutable void_i_token_offset_sig : uint32
    mutable void_i_i4_i4_offset_sig : uint32
    mutable void_u2_i4_i4_offset_sig : uint32
    mutable void_u2_i4_i_offset_sig : uint32
    mutable void_u2_i8_i4_offset_sig : uint32
    mutable void_u2_i8_i8_offset_sig : uint32
    mutable void_u2_r4_r4_offset_sig : uint32
    mutable void_u2_r8_r8_offset_sig : uint32
    mutable void_u2_i_i_offset_sig : uint32
    mutable void_u2_i_i4_offset_sig : uint32
    mutable void_i_i_i_offset_sig : uint32
    mutable void_i_i_i1_offset_sig : uint32
    mutable void_i_i_i2_offset_sig : uint32
    mutable void_i_i_i4_offset_sig : uint32
    mutable void_i_i_i8_offset_sig : uint32
    mutable void_i_i_r4_offset_sig : uint32
    mutable void_i_i_r8_offset_sig : uint32
    mutable void_i_i1_i_offset_sig : uint32
    mutable void_token_u4_u4_u4_sig : uint32
    mutable void_token_i_i_offset_sig : uint32
    mutable void_token_i_i4_offset_sig : uint32
    mutable void_token_i_i8_offset_sig : uint32
    mutable void_token_i_r4_offset_sig : uint32
    mutable void_token_i_r8_offset_sig : uint32
    mutable void_token_token_bool_u2_offset_sig : uint32
}
with
    member private x.SigToken2str =
        let map = System.Collections.Generic.Dictionary<uint32, string>()
        typeof<signatureTokens>.GetFields() |> Seq.iter (fun fld ->
            let token : uint32 = fld.GetValue x |> unbox
            if not <| map.ContainsKey token then map.Add(token, fld.Name))
        map
    member x.TokenToString (token : int32) =
        let result = ref ""
        if x.SigToken2str.TryGetValue(uint32 token, result) then !result
        else "<UNKNOWN TOKEN!>"

[<type: StructLayout(LayoutKind.Sequential, Pack=1, CharSet=CharSet.Ansi)>]
type rawMethodProperties = {
    mutable token : uint32
    mutable ilCodeSize : uint32
    mutable assemblyNameLength : uint32
    mutable moduleNameLength : uint32
    mutable maxStackSize : uint32
    mutable signatureTokensLength : uint32
}

[<type: StructLayout(LayoutKind.Sequential, Pack=1, CharSet=CharSet.Ansi)>]
type instrumentedMethodProperties = {
    mutable ilCodeSize : uint32
    mutable maxStackSize : uint32
}

[<type: StructLayout(LayoutKind.Sequential, Pack=1, CharSet=CharSet.Ansi)>]
type rawExceptionHandler = {
    mutable flags : int
    mutable tryOffset : uint32
    mutable tryLength : uint32
    mutable handlerOffset : uint32
    mutable handlerLength : uint32
    mutable matcher : uint32
}

type rawMethodBody = {
    properties : rawMethodProperties
    assembly : string
    moduleName : string
    tokens : signatureTokens
    il : byte array
    ehs : rawExceptionHandler array
}

type instrumentedMethodBody = {
    properties : instrumentedMethodProperties
    il : byte array
    ehs : rawExceptionHandler array
}

type evalStackArgType =
    | OpSymbolic = 1
    | OpI4 = 2
    | OpI8 = 3
    | OpR4 = 4
    | OpR8 = 5
    | OpRef = 6

type evalStackOperand =
    | NumericOp of evalStackArgType * int64
    | PointerOp of uint64 * uint64

[<type: StructLayout(LayoutKind.Sequential, Pack=1, CharSet=CharSet.Ansi)>]
type private execCommandStatic = {
    offset : uint32
    isBranch : uint32
    newCallStackFramesCount : uint32
    callStackFramesPops : uint32
    evaluationStackPushesCount : uint32
    evaluationStackPops : uint32
    newAddressesCount : uint32
}
type execCommand = {
    offset : uint32
    isBranch : uint32
    callStackFramesPops : uint32
    evaluationStackPops : uint32
    newCallStackFrames : int32 array
    evaluationStackPushes : evalStackOperand array
    newAddresses : UIntPtr array
    newAddressesTypes : Type array
    // TODO: add deleted addresses
}

type commandFromConcolic =
    | Instrument of rawMethodBody
    | ExecuteInstruction of execCommand
    | Terminate

type commandForConcolic =
    | ReadMethodBody
    | ReadString

type Communicator() =
    let pipeFile = Path.GetTempPath() + "concolic_fifo" // TODO: use pid also

    let confirmationByte = byte(0x55)
    let instrumentCommandByte = byte(0x56)
    let executeCommandByte = byte(0x57)
    let readMethodBodyByte = byte(0x58)
    let readStringByte = byte(0x59)
    let confirmation = Array.singleton confirmationByte

    let server = new NamedPipeServerStream(pipeFile, PipeDirection.InOut)
    let stream = server :> Stream

    let reportError (exn : IOException) =
        Logger.error "Error occured during communication with the concolic client! Message: %s" exn.Message
        false

    let fail format = Printf.ksprintf (fun (s : string) -> raise <| IOException s) format

    let unexpectedlyTerminated() = fail "Communication with CLR: interaction unexpectedly terminated"

    let readConfirmation () =
        let buffer : byte[] = Array.zeroCreate 1
        let bytesRead = stream.Read(buffer, 0, 1)
        if bytesRead <> 1 || buffer.[0] <> confirmationByte then
            fail "Communication with CLR: could not get the confirmation message. Instead read %d bytes with message [%s]" bytesRead (Array.map toString buffer |> join " ")

    let writeConfirmation () =
        stream.Write(confirmation, 0, 1)

    let readCount () =
        let countBytes : byte[] = Array.zeroCreate 4
        let countCount = stream.Read(countBytes, 0, 4)
        if countCount <> 4 then
            fail "Communication with CLR: could not get the amount of bytes of the next message. Instead read %d bytes" countCount
        BitConverter.ToInt32(countBytes, 0)

    let readBuffer () =
        let chunkSize = 8192
        let count = readCount()
        assert(count <> 0)
        if count < 0 then None
        else
            writeConfirmation()
            let buffer : byte[] = Array.zeroCreate count
            let mutable bytesRead = 0
            while bytesRead < count do
                let length = min chunkSize (count - bytesRead)
                let chunk : byte[] = Array.zeroCreate length
                let offset = bytesRead
                bytesRead <- bytesRead + server.Read(chunk, 0, length)
                Array.Copy(chunk, 0, buffer, offset, length)
            if bytesRead <> count then
                fail "Communication with CLR: expected %d bytes, but read %d bytes" count bytesRead
            else
                writeConfirmation()
                Some buffer

    let writeBuffer (buffer : byte[]) =
        if buffer.LongLength > int64(Int32.MaxValue) then
            fail "Communication with CLR: too large message (length = %s)!" (buffer.LongLength.ToString())
        let countBuffer = BitConverter.GetBytes(buffer.Length)
        assert(countBuffer.Length = 4)
        stream.Write(countBuffer, 0, 4)
        readConfirmation()
        stream.Write(buffer, 0, buffer.Length)
        readConfirmation()

    let readString () =
        match readBuffer() with
        | None -> unexpectedlyTerminated()
        | Some buffer -> Encoding.ASCII.GetString(buffer)

    let writeString (str : string) =
        let buffer = Encoding.ASCII.GetBytes(str)
        writeBuffer buffer

    let waitClient () =
        Logger.trace "Waiting for client connection..."
        server.WaitForConnection()
        Logger.trace "Client connected!"

    let handshake () =
        let message = "Hi!"
        writeString message
        let expectedMessage = "Hi!"
        let s = readString ()
        if s <> expectedMessage then
            fail "Communication with CLR: handshake failed: got %s instead of %s" s expectedMessage

    member private x.Deserialize<'a> (bytes : byte array, startIndex : int) =
        let result = Reflection.createObject typeof<'a> :?> 'a
        let size = Marshal.SizeOf(typeof<'a>)
        let unmanagedPtr = Marshal.AllocHGlobal(size)
        Marshal.Copy(bytes, startIndex, unmanagedPtr, size)
        Marshal.PtrToStructure(unmanagedPtr, result)
        Marshal.FreeHGlobal(unmanagedPtr)
        result

    member private x.Deserialize<'a> (bytes : byte array) = x.Deserialize<'a>(bytes, 0)

    member private x.Serialize<'a> (structure : 'a, bytes : byte array, startIndex : int) =
        let size = Marshal.SizeOf(typeof<'a>)
        let unmanagedPtr = Marshal.AllocHGlobal(size)
        Marshal.StructureToPtr(structure, unmanagedPtr, false)
        Marshal.Copy(unmanagedPtr, bytes, startIndex, size)
        Marshal.FreeHGlobal(unmanagedPtr)

    member private x.Serialize<'a> (structure : 'a) =
        let size = Marshal.SizeOf(typeof<'a>)
        let result = Array.zeroCreate size
        x.Serialize<'a>(structure, result, 0)
        result

    member private x.SerializeCommand command =
        let byte =
            match command with
            | ReadString -> readStringByte
            | ReadMethodBody -> readMethodBodyByte
        Array.singleton byte

    member x.Connect() =
        try
            waitClient()
            handshake()
            true
        with
        | :? IOException as e -> reportError e

    member private x.ReadStructure<'a>() =
        match readBuffer() with
        | Some bytes -> x.Deserialize<'a> bytes
        | None -> unexpectedlyTerminated()

    member x.ReadProbes() = x.ReadStructure<probes>()

    member x.SendCommand (command : commandForConcolic) =
        let bytes = x.SerializeCommand command
        writeBuffer bytes

    member x.SendStringAndReadItsIndex (str : string) : uint32 =
        x.SendCommand ReadString
        writeString str
        match readBuffer() with
        | Some bytes -> BitConverter.ToUInt32(bytes, 0)
        | None -> unexpectedlyTerminated()

    member x.ReadMethodBody() =
        match readBuffer() with
        | Some bytes ->
            let propertiesBytes, rest = Array.splitAt (Marshal.SizeOf typeof<rawMethodProperties>) bytes
            let properties = x.Deserialize<rawMethodProperties> propertiesBytes
            let sizeOfSignatureTokens = Marshal.SizeOf typeof<signatureTokens>
            if int properties.signatureTokensLength <> sizeOfSignatureTokens then
                fail "Size of received signature tokens buffer mismatch the expected! Probably you've altered the client-side signatures, but forgot to alter the server-side structure (or vice-versa)"
            let signatureTokenBytes, rest = Array.splitAt sizeOfSignatureTokens rest
            let assemblyNameBytes, rest = Array.splitAt (int properties.assemblyNameLength) rest
            let moduleNameBytes, rest = Array.splitAt (int properties.moduleNameLength) rest
            let signatureTokens = x.Deserialize<signatureTokens> signatureTokenBytes
            let assemblyName = Encoding.Unicode.GetString(assemblyNameBytes)
            let moduleName = Encoding.Unicode.GetString(moduleNameBytes)
            let ilBytes, ehBytes  = Array.splitAt (int properties.ilCodeSize) rest
            let ehSize = Marshal.SizeOf typeof<rawExceptionHandler>
            let ehCount = Array.length ehBytes / ehSize
            let ehs = Array.init ehCount (fun i -> x.Deserialize<rawExceptionHandler>(ehBytes, i * ehSize))
            {properties = properties; tokens = signatureTokens; assembly = assemblyName; moduleName = moduleName; il = ilBytes; ehs = ehs}
        | None -> unexpectedlyTerminated()

    member private x.ToUIntPtr =
        if IntPtr.Size = 4 then fun (bytes : byte[]) index -> BitConverter.ToUInt32(bytes, index) |> UIntPtr
        else fun (bytes : byte[]) index -> BitConverter.ToUInt64(bytes, index) |> UIntPtr

    member x.ReadExecuteCommand() =
        match readBuffer() with
        | Some bytes ->
            let staticSize = Marshal.SizeOf typeof<execCommandStatic>
            let staticBytes, dynamicBytes = Array.splitAt staticSize bytes
            let staticPart = x.Deserialize<execCommandStatic> staticBytes
            let callStackEntrySize = Marshal.SizeOf typeof<int32>
            let callStackOffset = (int staticPart.newCallStackFramesCount) * callStackEntrySize
            let newCallStackFrames = Array.init (int staticPart.newCallStackFramesCount) (fun i -> BitConverter.ToInt32(dynamicBytes, i * callStackEntrySize))
            let mutable offset = callStackOffset
            let evaluationStackPushes = Array.init (int staticPart.evaluationStackPushesCount) (fun _ ->
                let evalStackArgTypeNum = BitConverter.ToInt32(dynamicBytes, offset)
                offset <- offset + sizeof<int32>
                let evalStackArgType = LanguagePrimitives.EnumOfValue evalStackArgTypeNum
                match evalStackArgType with
                | evalStackArgType.OpRef -> // TODO: mb use UIntPtr? #do
                    let baseAddr = BitConverter.ToUInt64(dynamicBytes, offset)
                    offset <- offset + sizeof<uint64>
                    let shift = BitConverter.ToUInt64(dynamicBytes, offset)
                    offset <- offset + sizeof<uint64>
                    PointerOp(baseAddr, shift)
                | evalStackArgType.OpSymbolic
                | evalStackArgType.OpI4
                | evalStackArgType.OpI8
                | evalStackArgType.OpR4
                | evalStackArgType.OpR8 ->
                    let content = BitConverter.ToInt64(dynamicBytes, offset)
                    offset <- offset + sizeof<int64>
                    NumericOp(evalStackArgType, content)
                | _ -> __unreachable__())
            let newAddresses = Array.init (int staticPart.newAddressesCount) (fun _ ->
                let res = x.ToUIntPtr dynamicBytes offset in offset <- offset + IntPtr.Size; res)
            let newAddressesTypesLengths = Array.init (int staticPart.newAddressesCount) (fun _ ->
                let res = BitConverter.ToUInt64(dynamicBytes, offset) in offset <- offset + sizeof<uint64>; res)
            let newAddressesTypes = Array.init (int staticPart.newAddressesCount) (fun i ->
                let size = int newAddressesTypesLengths.[i]
                let rec readType () =
                    let token = BitConverter.ToInt32(dynamicBytes, offset)
                    offset <- offset + sizeof<int>
                    let assemblySize = BitConverter.ToInt32(dynamicBytes, offset)
                    offset <- offset + sizeof<int>
                    let assemblyBytes = dynamicBytes.[offset .. offset + assemblySize - 1]
                    offset <- offset + assemblySize
                    let assemblyName = Encoding.Unicode.GetString(assemblyBytes)
                    let assembly = System.Reflection.Assembly.Load(assemblyName)
                    let moduleSize = BitConverter.ToInt32(dynamicBytes, offset)
                    offset <- offset + sizeof<int>
                    let moduleBytes = dynamicBytes.[offset .. offset + moduleSize - 1]
                    offset <- offset + moduleSize
                    let moduleName = Encoding.Unicode.GetString(moduleBytes) |> Path.GetFileName
                    let typeModule = Reflection.resolveModuleFromAssembly assembly moduleName
                    let typeArgsCount = BitConverter.ToInt32(dynamicBytes, offset)
                    offset <- offset + sizeof<int>
                    let typeArgs = Array.init typeArgsCount (fun _ -> readType())
                    let resultType = Reflection.resolveTypeFromModule typeModule token
                    if Array.isEmpty typeArgs then resultType else resultType.MakeGenericType(typeArgs)
                // NOTE: Some types can not be resolved while concrete execution (GetClassIDInfo2 fails)
                // NOTE: These types have zero size
                if size = 0 then typeof<Void> else readType())
            { offset = staticPart.offset
              isBranch = staticPart.isBranch
              callStackFramesPops = staticPart.callStackFramesPops
              evaluationStackPops = staticPart.evaluationStackPops
              newCallStackFrames = newCallStackFrames
              evaluationStackPushes = evaluationStackPushes
              newAddresses = newAddresses
              newAddressesTypes = newAddressesTypes }
        | None -> unexpectedlyTerminated()

    // TODO: send struct via serialize
    member x.SendExecResponse ops lastPush (framesCount : int) =
        let mutable index = 0
        let writeFirstPart count =
            let count = count + sizeof<int>
            let count =
                match lastPush with
                | Some _ -> count + 2
                | None -> count + 1
            let bytes = Array.zeroCreate (count + sizeof<int>)
            let success = BitConverter.TryWriteBytes(Span(bytes, index, sizeof<int>), framesCount) in assert success
            index <- index + sizeof<int>
            match lastPush with
            | Some concreteness ->
                bytes.[index] <- 1uy
                bytes.[index + 1] <- if concreteness then 1uy else 0uy
                index <- index + 2
            | None ->
                bytes.[index] <- 0uy
                index <- index + 1
            bytes
        match ops with
        | Some ops ->
            let count = ops |> List.sumBy (fun (_, typ) ->
                if Types.IsValueType typ then sizeof<int> + sizeof<int64>
                else sizeof<int> + 2 * sizeof<int64>)
            let bytes = writeFirstPart count
            let success = BitConverter.TryWriteBytes(Span(bytes, index, sizeof<int>), ops.Length) in assert success
            index <- index + sizeof<int>
            ops |> List.iter (fun (obj : obj, typ) ->
                if Types.IsValueType typ then
                    let opType, (content : int64) =
                        if Types.IsInteger typ then
                            if Types.SizeOf typ = sizeof<int64> then evalStackArgType.OpI8, unbox obj
                            else evalStackArgType.OpI4, (unbox obj |> int64)
                        elif Types.IsReal typ then
                            if Types.SizeOf typ = sizeof<double> then evalStackArgType.OpR8, (unbox obj |> int64)
                            else evalStackArgType.OpR4, (unbox obj |> int64)
                        else
                            // TODO: support structs
                            __notImplemented__()
                    let success = BitConverter.TryWriteBytes(Span(bytes, index, sizeof<int>), LanguagePrimitives.EnumToValue opType) in assert success
                    index <- index + sizeof<int>
                    let success = BitConverter.TryWriteBytes(Span(bytes, index, sizeof<int64>), content) in assert success
                    index <- index + sizeof<int64>
                elif isNull obj then
                    // NOTE: null refs handling
                    let success = BitConverter.TryWriteBytes(Span(bytes, index, sizeof<int>), LanguagePrimitives.EnumToValue evalStackArgType.OpRef) in assert success
                    index <- index + sizeof<int>
                    let success = BitConverter.TryWriteBytes(Span(bytes, index, sizeof<uint64>), 0UL) in assert success
                    index <- index + sizeof<uint64>
                    let success = BitConverter.TryWriteBytes(Span(bytes, index, sizeof<uint64>), 0UL) in assert success
                    index <- index + sizeof<uint64>
                else
                    // NOTE: nonnull refs handling
                    let address, offset = obj :?> uint32 * uint64
                    let success = BitConverter.TryWriteBytes(Span(bytes, index, sizeof<int>), LanguagePrimitives.EnumToValue evalStackArgType.OpRef) in assert success
                    index <- index + sizeof<int>
                    let success = BitConverter.TryWriteBytes(Span(bytes, index, sizeof<uint64>), uint64 address) in assert success
                    index <- index + sizeof<int64>
                    let success = BitConverter.TryWriteBytes(Span(bytes, index, sizeof<uint64>), offset) in assert success
                    index <- index + sizeof<int64>)
            writeBuffer bytes
        | None ->
            let bytes = writeFirstPart 0
            writeBuffer bytes

    member x.SendMethodBody (mb : instrumentedMethodBody) =
        x.SendCommand ReadMethodBody
        let propBytes = x.Serialize mb.properties
        let ehSize = Marshal.SizeOf typeof<rawExceptionHandler>
        let ehBytes : byte[] = Array.zeroCreate (ehSize * mb.ehs.Length)
        Array.iteri (fun i eh -> x.Serialize<rawExceptionHandler>(eh, ehBytes, i * ehSize)) mb.ehs
        let message = Array.concat [propBytes; mb.il; ehBytes]
        Logger.trace "Sending method body! Total %d bytes" message.Length
        writeBuffer message

    member x.ReadCommand() =
        match readBuffer() with
        | Some bytes ->
            if bytes.Length <> 1 then fail "Invalid command number!"
            match bytes.[0] with
            | b when b = instrumentCommandByte ->
                x.ReadMethodBody() |> Instrument
            | b when b = executeCommandByte ->
                x.ReadExecuteCommand() |> ExecuteInstruction
            | b -> fail "Unexpected command %d from client machine!" b
        | None -> Terminate

    interface IDisposable with
        member x.Dispose() =
            server.Dispose()
