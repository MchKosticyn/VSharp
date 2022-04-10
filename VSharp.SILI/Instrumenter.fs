namespace VSharp.Concolic

open VSharp
open System.Reflection
open System.Reflection.Emit
open System.Collections.Generic
open VSharp.Concolic
open VSharp.Interpreter.IL

type Instrumenter(communicator : Communicator, entryPoint : MethodBase, probes : probes) =
    // TODO: should we consider executed assembly build options here?
    let ldc_i : opcode = (if System.Environment.Is64BitOperatingSystem then OpCodes.Ldc_I8 else OpCodes.Ldc_I4) |> VSharp.OpCode
    let mutable currentStaticFieldID = 0
    let staticFieldIDs = Dictionary<int, FieldInfo>()
    let registerStaticFieldID (fieldInfo : FieldInfo) =
        if staticFieldIDs.ContainsValue(fieldInfo) |> not then
            currentStaticFieldID <- currentStaticFieldID + 1
            staticFieldIDs.Add(currentStaticFieldID, fieldInfo)
            currentStaticFieldID
        else
            let kvp = staticFieldIDs |> Seq.find (fun kvp -> kvp.Value = fieldInfo)
            kvp.Key

    static member private instrumentedFunctions = HashSet<MethodBase>()
    [<DefaultValue>] val mutable tokens : signatureTokens
    [<DefaultValue>] val mutable rewriter : ILRewriter
    [<DefaultValue>] val mutable m : MethodBase

    member x.StaticFieldByID id = staticFieldIDs[id]

    member private x.MkCalli(instr : ilInstr byref, signature : uint32) =
        instr <- x.rewriter.NewInstr OpCodes.Calli
        instr.arg <- Arg32 (int32 signature)

    member private x.PrependInstr(opcode, arg, beforeInstr : ilInstr byref) =
        let mutable newInstr = x.rewriter.CopyInstruction(beforeInstr)
        x.rewriter.InsertAfter(beforeInstr, newInstr)
        swap &newInstr &beforeInstr
        newInstr.opcode <- VSharp.OpCode opcode
        newInstr.arg <- arg

    member private x.PrependNop(beforeInstr : ilInstr byref) =
        let mutable newInstr = x.rewriter.CopyInstruction(beforeInstr)
        x.rewriter.InsertAfter(beforeInstr, newInstr)
        swap &newInstr &beforeInstr
        newInstr.opcode <- VSharp.OpCode OpCodes.Nop
        newInstr.arg <- NoArg
        newInstr

    member private x.PrependBranch(opcode, beforeInstr : ilInstr byref) =
        let mutable newInstr = x.rewriter.CopyInstruction(beforeInstr)
        x.rewriter.InsertAfter(beforeInstr, newInstr)
        swap &newInstr &beforeInstr
        newInstr.opcode <- VSharp.OpCode opcode
        newInstr.arg <- NoArg // In chain of prepends, the address of instruction constantly changes. Deferring it.
        newInstr

    member private x.AppendInstr (opcode : OpCode) arg (afterInstr : ilInstr) =
        let dupInstr = x.rewriter.NewInstr opcode
        dupInstr.arg <- arg
        x.rewriter.InsertAfter(afterInstr, dupInstr)

    member private x.AppendNop (afterInstr : ilInstr) =
        x.AppendInstr OpCodes.Nop NoArg afterInstr
        afterInstr.next

    member private x.PrependDup(beforeInstr : ilInstr byref) = x.PrependInstr(OpCodes.Dup, NoArg, &beforeInstr)
    member private x.AppendDup afterInstr = x.AppendInstr OpCodes.Dup NoArg afterInstr

    member private x.PrependProbe(methodAddress : uint64, args : (OpCode * ilInstrOperand) list, signature, beforeInstr : ilInstr byref) =
        let result = beforeInstr
        let mutable newInstr = x.rewriter.CopyInstruction(beforeInstr)
        x.rewriter.InsertAfter(beforeInstr, newInstr)
        swap &newInstr &beforeInstr

        match args with
        | (opcode, arg)::tail ->
            newInstr.opcode <- opcode |> VSharp.OpCode
            newInstr.arg <- arg
            for opcode, arg in tail do
                let newInstr = x.rewriter.NewInstr opcode
                newInstr.arg <- arg
                x.rewriter.InsertBefore(beforeInstr, newInstr)

            newInstr <- x.rewriter.NewInstr ldc_i
            newInstr.arg <- Arg64 (int64 methodAddress)
            x.rewriter.InsertBefore(beforeInstr, newInstr)
        | [] ->
            newInstr.opcode <- ldc_i
            newInstr.arg <- Arg64 (int64 methodAddress)

        x.MkCalli(&newInstr, signature)
        x.rewriter.InsertBefore(beforeInstr, newInstr)
        result

    member private x.PrependProbeWithOffset(methodAddress : uint64, args : (OpCode * ilInstrOperand) list, signature, beforeInstr : ilInstr byref) =
        x.PrependProbe(methodAddress, List.append args [(OpCodes.Ldc_I4, beforeInstr.offset |> int32 |> Arg32)], signature, &beforeInstr)

    member private x.AppendProbe(methodAddress : uint64, args : (OpCode * ilInstrOperand) list, signature, afterInstr : ilInstr) =
        let mutable newInstr = afterInstr
        x.MkCalli(&newInstr, signature)
        x.rewriter.InsertAfter(afterInstr, newInstr)

        let newInstr = x.rewriter.NewInstr ldc_i
        newInstr.arg <- Arg64 (int64 methodAddress)
        x.rewriter.InsertAfter(afterInstr, newInstr)

        for opcode, arg in List.rev args do
            let newInstr = x.rewriter.NewInstr opcode
            newInstr.arg <- arg
            x.rewriter.InsertAfter(afterInstr, newInstr)

    // NOTE: offset is sent from client to SILI
    member private x.AppendProbeWithOffset(methodAddress : uint64, args : (OpCode * ilInstrOperand) list, signature, afterInstr : ilInstr) =
        x.AppendProbe(methodAddress, List.append args [(OpCodes.Ldc_I4, afterInstr.offset |> int32 |> Arg32)], signature, afterInstr)

    member private x.AppendProbeWithOffsetMemUnmem(methodAddress : uint64, args : (OpCode * ilInstrOperand) list, signature, prependTarget : ilInstr byref, afterInstr : ilInstr) =
        x.AppendProbe(methodAddress, List.append args [(OpCodes.Ldc_I4, afterInstr.offset |> int32 |> Arg32)], signature, afterInstr)
        match afterInstr.stackState with
        | UnOp typ ->
            let probe, token = x.PrependMemUnmemForType(typ, 0, 0, &prependTarget)
            x.PrependProbe(probe, [(OpCodes.Ldc_I4, Arg32 0)], token, &prependTarget) |> ignore
        | _ -> __unreachable__()

    member private x.PlaceEnterProbe (firstInstr : ilInstr byref) =
        let isSpontaneous = if Reflection.isExternalMethod x.m || Reflection.isStaticConstructor x.m then 1 else 0
        let localsCount =
            match x.m.GetMethodBody() with
            | null -> 0
            | mb -> mb.LocalVariables.Count
        let argsCount = x.m.GetParameters().Length
        let argsCount = if Reflection.hasThis x.m then argsCount + 1 else argsCount
        if x.m = entryPoint then
            let args = [(OpCodes.Ldc_I4, Arg32 x.m.MetadataToken)
                        (OpCodes.Ldc_I4, Arg32 argsCount)
//                        (OpCodes.Ldc_I4, Arg32 1) // Arguments of entry point are concrete
                        (OpCodes.Ldc_I4, Arg32 0) // Arguments of entry point are symbolic
                        (OpCodes.Ldc_I4, x.rewriter.MaxStackSize |> int32 |> Arg32)
                        (OpCodes.Ldc_I4, Arg32 localsCount)]
            x.PrependProbe(probes.enterMain, args, x.tokens.void_token_u2_bool_u4_u4_sig, &firstInstr)
        else
            let args = [(OpCodes.Ldc_I4, Arg32 x.m.MetadataToken)
                        (OpCodes.Ldc_I4, x.rewriter.MaxStackSize |> int32 |> Arg32)
                        (OpCodes.Ldc_I4, Arg32 argsCount)
                        (OpCodes.Ldc_I4, Arg32 localsCount)
                        (OpCodes.Ldc_I4, Arg32 isSpontaneous)]
            x.PrependProbe(probes.enter, args, x.tokens.void_token_u4_u4_u4_i1_sig, &firstInstr)

    member private x.PrependMem_p(idx, order, instr : ilInstr byref) =
        x.PrependInstr(OpCodes.Conv_I, NoArg, &instr)
        x.PrependProbe(probes.mem_p_idx, [(OpCodes.Ldc_I4, Arg32 idx); (OpCodes.Ldc_I4, Arg32 order)], x.tokens.void_i_i1_i1_sig, &instr) |> ignore

    member private x.PrependMem_i1(idx, order, instr : ilInstr byref) =
        x.PrependProbe(probes.mem_1_idx, [(OpCodes.Ldc_I4, Arg32 idx); (OpCodes.Ldc_I4, Arg32 order)], x.tokens.void_i1_i1_i1_sig, &instr) |> ignore

    member private x.PrependMem_i2(idx, order, instr : ilInstr byref) =
        x.PrependProbe(probes.mem_2_idx, [(OpCodes.Ldc_I4, Arg32 idx); (OpCodes.Ldc_I4, Arg32 order)], x.tokens.void_i2_i1_i1_sig, &instr) |> ignore

    member private x.PrependMem_i4(idx, order, instr : ilInstr byref) =
        x.PrependProbe(probes.mem_4_idx, [(OpCodes.Ldc_I4, Arg32 idx); (OpCodes.Ldc_I4, Arg32 order)], x.tokens.void_i4_i1_i1_sig, &instr) |> ignore

    member private x.PrependMem_i8(idx, order, instr : ilInstr byref) =
        x.PrependProbe(probes.mem_8_idx, [(OpCodes.Ldc_I4, Arg32 idx); (OpCodes.Ldc_I4, Arg32 order)], x.tokens.void_i8_i1_i1_sig, &instr) |> ignore

    member private x.PrependMem_f4(idx, order, instr : ilInstr byref) =
        x.PrependProbe(probes.mem_f4_idx, [(OpCodes.Ldc_I4, Arg32 idx); (OpCodes.Ldc_I4, Arg32 order)], x.tokens.void_r4_i1_i1_sig, &instr) |> ignore

    member private x.PrependMem_f8(idx, order, instr : ilInstr byref) =
        x.PrependProbe(probes.mem_f8_idx, [(OpCodes.Ldc_I4, Arg32 idx); (OpCodes.Ldc_I4, Arg32 order)], x.tokens.void_r8_i1_i1_sig, &instr) |> ignore

    member private x.PrependMem2_p (instr : ilInstr byref) =
        x.PrependMem_p(1, 0, &instr)
        x.PrependMem_p(0, 1, &instr)

    member private x.PrependMem2_p_1 (instr : ilInstr byref) =
        x.PrependMem_i1(1, 0, &instr)
        x.PrependMem_p(0, 1, &instr)

    member private x.PrependMem2_p_2 (instr : ilInstr byref) =
        x.PrependMem_i2(1, 0, &instr)
        x.PrependMem_p(0, 1, &instr)

    member private x.PrependMem2_p_4 (instr : ilInstr byref) =
        x.PrependMem_i4(1, 0, &instr)
        x.PrependMem_p(0, 1, &instr)

    member private x.PrependMem2_p_8 (instr : ilInstr byref) =
        x.PrependMem_i8(1, 0, &instr)
        x.PrependMem_p(0, 1, &instr)

    member private x.PrependMem2_p_f4 (instr : ilInstr byref) =
        x.PrependMem_f4(1, 0, &instr)
        x.PrependMem_p(0, 1, &instr)

    member private x.PrependMem2_p_f8 (instr : ilInstr byref) =
        x.PrependMem_f8(1, 0, &instr)
        x.PrependMem_p(0, 1, &instr)

    member private x.PrependMem2_4_p (instr : ilInstr byref) =
        x.PrependMem_p(1, 0, &instr)
        x.PrependMem_i4(0, 1, &instr)

    member private x.PrependMem3_p (instr : ilInstr byref) =
        x.PrependMem_p(2, 0, &instr)
        x.PrependMem_p(1, 1, &instr)
        x.PrependMem_p(0, 2, &instr)

    member private x.PrependMem3_p_i1_p (instr : ilInstr byref) =
        x.PrependMem_p(2, 0, &instr)
        x.PrependMem_i1(1, 1, &instr)
        x.PrependMem_p(0, 2, &instr)

    member private x.PrependMem3_p_p_i1 (instr : ilInstr byref) =
        x.PrependMem_i1(2, 0, &instr)
        x.PrependMem_p(1, 1, &instr)
        x.PrependMem_p(0, 2, &instr)

    member private x.PrependMem3_p_p_i2 (instr : ilInstr byref) =
        x.PrependMem_i2(2, 0, &instr)
        x.PrependMem_p(1, 1, &instr)
        x.PrependMem_p(0, 2, &instr)

    member private x.PrependValidLeaveMain(instr : ilInstr byref) =
        match instr.stackState with
        | _ when Reflection.hasNonVoidResult x.m |> not ->
            x.PrependProbeWithOffset(probes.leaveMain_0, [], x.tokens.void_offset_sig, &instr) |> ignore
        | UnOp evaluationStackCellType.I1
        | UnOp evaluationStackCellType.I2
        | UnOp evaluationStackCellType.I4 ->
            x.PrependMem_i4(0, 0, &instr)
            x.PrependProbe(probes.unmem_4, [(OpCodes.Ldc_I4, Arg32 0)], x.tokens.i4_i1_sig, &instr) |> ignore
            x.PrependProbeWithOffset(probes.leaveMain_4, [], x.tokens.void_i4_offset_sig, &instr) |> ignore
            x.PrependProbe(probes.unmem_4, [(OpCodes.Ldc_I4, Arg32 0)], x.tokens.i4_i1_sig, &instr) |> ignore
        | UnOp evaluationStackCellType.I8 ->
            x.PrependMem_i8(0, 0, &instr)
            x.PrependProbe(probes.unmem_8, [(OpCodes.Ldc_I4, Arg32 0)], x.tokens.i8_i1_sig, &instr) |> ignore
            x.PrependProbeWithOffset(probes.leaveMain_8, [], x.tokens.void_i8_offset_sig, &instr) |> ignore
            x.PrependProbe(probes.unmem_8, [(OpCodes.Ldc_I4, Arg32 0)], x.tokens.i8_i1_sig, &instr) |> ignore
        | UnOp evaluationStackCellType.R4 ->
            x.PrependMem_f4(0, 0, &instr)
            x.PrependProbe(probes.unmem_f4, [(OpCodes.Ldc_I4, Arg32 0)], x.tokens.r4_i1_sig, &instr) |> ignore
            x.PrependProbeWithOffset(probes.leaveMain_f4, [], x.tokens.void_r4_offset_sig, &instr) |> ignore
            x.PrependProbe(probes.unmem_f4, [(OpCodes.Ldc_I4, Arg32 0)], x.tokens.r4_i1_sig, &instr) |> ignore
        | UnOp evaluationStackCellType.R8 ->
            x.PrependMem_f8(0, 0, &instr)
            x.PrependProbe(probes.unmem_f8, [(OpCodes.Ldc_I4, Arg32 0)], x.tokens.r8_i1_sig, &instr) |> ignore
            x.PrependProbeWithOffset(probes.leaveMain_f8, [], x.tokens.void_r8_offset_sig, &instr) |> ignore
            x.PrependProbe(probes.unmem_f8, [(OpCodes.Ldc_I4, Arg32 0)], x.tokens.r8_i1_sig, &instr) |> ignore
        | UnOp evaluationStackCellType.I ->
            x.PrependMem_p(0, 0, &instr)
            x.PrependProbe(probes.unmem_p, [(OpCodes.Ldc_I4, Arg32 0)], x.tokens.i_i1_sig, &instr) |> ignore
            x.PrependProbeWithOffset(probes.leaveMain_p, [], x.tokens.void_i_offset_sig, &instr) |> ignore
            x.PrependProbe(probes.unmem_p, [(OpCodes.Ldc_I4, Arg32 0)], x.tokens.i_i1_sig, &instr) |> ignore
        | UnOp evaluationStackCellType.Ref ->
            x.PrependMem_p(0, 0, &instr)
            x.PrependProbe(probes.unmem_p, [(OpCodes.Ldc_I4, Arg32 0)], x.tokens.i_i1_sig, &instr) |> ignore
            x.PrependProbeWithOffset(probes.leaveMain_p, [], x.tokens.void_i_offset_sig, &instr) |> ignore
            x.PrependProbe(probes.unmem_p, [(OpCodes.Ldc_I4, Arg32 0)], x.tokens.i_i1_sig, &instr) |> ignore
        | UnOp evaluationStackCellType.Struct
        | UnOp evaluationStackCellType.RefLikeStruct ->
            x.PrependMem_p(0, 0, &instr)
            x.PrependProbe(probes.unmem_p, [(OpCodes.Ldc_I4, Arg32 0)], x.tokens.i_i1_sig, &instr) |> ignore
            x.PrependProbeWithOffset(probes.leaveMain_p, [], x.tokens.void_i_offset_sig, &instr) |> ignore
            x.PrependProbe(probes.unmem_p, [(OpCodes.Ldc_I4, Arg32 0)], x.tokens.i_i1_sig, &instr) |> ignore
        | _ -> internalfailf "PrependValidLeaveMain: unexpected stack state! %O" instr.stackState

    member private x.PlaceLeaveProbe(instr : ilInstr byref) =
        if x.m = entryPoint then
            x.PrependValidLeaveMain(&instr)
        else
            let returnsSomething = Reflection.hasNonVoidResult x.m
            let args = [(OpCodes.Ldc_I4, (if returnsSomething then 1 else 0) |> Arg32)]
            x.PrependProbeWithOffset(probes.leave, args, x.tokens.void_u1_offset_sig, &instr) |> ignore

    member x.MethodName with get() = x.m.Name

    member private x.PrependLdcDefault(t : System.Type, instr : ilInstr byref) =
        match t with
        | _ when not t.IsValueType -> x.PrependInstr(OpCodes.Ldnull, NoArg, &instr)
        | _ when t = typeof<bool> -> x.PrependInstr(OpCodes.Ldc_I4_0, NoArg, &instr)
        | _ when t = typeof<int8> -> x.PrependInstr(OpCodes.Ldc_I4_0, NoArg, &instr)
        | _ when t = typeof<uint8> -> x.PrependInstr(OpCodes.Ldc_I4_0,NoArg, &instr)
        | _ when t = typeof<int16> -> x.PrependInstr(OpCodes.Ldc_I4_0, NoArg, &instr)
        | _ when t = typeof<uint16> -> x.PrependInstr(OpCodes.Ldc_I4_0, NoArg, &instr)
        | _ when t = typeof<int> -> x.PrependInstr(OpCodes.Ldc_I4_0, NoArg, &instr)
        | _ when t = typeof<uint> -> x.PrependInstr(OpCodes.Ldc_I4_0, NoArg, &instr)
        | _ when t = typeof<int64> -> x.PrependInstr(OpCodes.Ldc_I8, (Arg64 0L), &instr)
        | _ when t = typeof<uint64> -> x.PrependInstr(OpCodes.Ldc_I8, (Arg64 0L), &instr)
        | _ when t = typeof<single> -> x.PrependInstr(OpCodes.Ldc_R4, (Arg32 0), &instr)
        | _ when t = typeof<double> -> x.PrependInstr(OpCodes.Ldc_R8, (Arg64 0L), &instr)
        | _ -> __unreachable__()

    member private x.SizeOfIndirection = function
        | OpCodeValues.Ldind_I1
        | OpCodeValues.Ldind_U1
        | OpCodeValues.Stind_I1 -> 1
        | OpCodeValues.Ldind_I2
        | OpCodeValues.Ldind_U2
        | OpCodeValues.Stind_I2 -> 2
        | OpCodeValues.Ldind_I4
        | OpCodeValues.Ldind_U4
        | OpCodeValues.Ldind_R4
        | OpCodeValues.Stind_I4
        | OpCodeValues.Stind_R4 -> 4
        | OpCodeValues.Ldind_I8
        | OpCodeValues.Ldind_R8
        | OpCodeValues.Stind_I8
        | OpCodeValues.Stind_R8 -> 8
        | OpCodeValues.Ldind_I
        | OpCodeValues.Ldind_Ref
        | OpCodeValues.Stind_I
        | OpCodeValues.Stind_Ref -> System.IntPtr.Size
        | _ -> __unreachable__()

    member private x.PrependMemUnmemForType(t : evaluationStackCellType, idx, order, instr : ilInstr byref) =
        match t with
        | evaluationStackCellType.I1 ->
            x.PrependMem_i1(idx, order, &instr)
            probes.unmem_1, x.tokens.i1_i1_sig
        | evaluationStackCellType.I2 ->
            x.PrependMem_i2(idx, order, &instr)
            probes.unmem_2, x.tokens.i2_i1_sig
        | evaluationStackCellType.I4 ->
            x.PrependMem_i4(idx, order, &instr)
            probes.unmem_4, x.tokens.i4_i1_sig
        | evaluationStackCellType.I8 ->
            x.PrependMem_i8(idx, order, &instr)
            probes.unmem_8, x.tokens.i8_i1_sig
        | evaluationStackCellType.R4 ->
            x.PrependMem_f4(idx, order, &instr)
            probes.unmem_f4, x.tokens.r4_i1_sig
        | evaluationStackCellType.R8 ->
            x.PrependMem_f8(idx, order, &instr)
            probes.unmem_f8, x.tokens.r8_i1_sig
        | evaluationStackCellType.I ->
            x.PrependMem_p(idx, order, &instr)
            probes.unmem_p, x.tokens.i_i1_sig
        | evaluationStackCellType.Ref ->
            x.PrependMem_p(idx, order, &instr)
            probes.unmem_p, x.tokens.i_i1_sig
        | evaluationStackCellType.Struct
            // TODO: support struct
//            x.PrependInstr(OpCodes.Box, NoArg, &instr)
//            x.PrependMem_p(idx, order, &instr)
//            probes.unmem_p, x.tokens.i_i1_sig
        | evaluationStackCellType.RefLikeStruct ->
            __notImplemented__()
            // TODO: support struct
//            x.PrependInstr(OpCodes.Box, NoArg, &instr)
//            x.PrependMem_p(idx, order, &instr)
//            probes.unmem_p, x.tokens.i_i1_sig
        | _ -> __unreachable__()

    member private x.PrependMemUnmemAndProbeWithOffset(methodAddress : uint64, args : (OpCode * ilInstrOperand) list, signature, beforeInstr : ilInstr byref)=
        match beforeInstr.stackState with
        | UnOp typ ->
            let probe, token = x.PrependMemUnmemForType(typ, 0, 0, &beforeInstr)
            x.PrependProbe(probe, [(OpCodes.Ldc_I4, Arg32 0)], token, &beforeInstr) |> ignore
        | s -> internalfailf "PrependMemUnmemAndProbeWithOffset: unexpected stack state %O" s
        x.PrependProbeWithOffset(methodAddress, args, signature, &beforeInstr)
    member private x.AcceptTypeToken (t : System.Type) =
        let correctType (t : System.Type) =
            match t with
            | _ when t.IsByRef -> t.GetElementType()
            | _ -> t
        let str = (correctType t).ToString()
        match t with
        | _ when t.Module = x.m.Module && t.IsTypeDefinition && not (t.IsGenericType || t.IsGenericTypeDefinition) ->
            t.MetadataToken
        | _ when t.IsTypeDefinition && not (t.IsGenericType || t.IsGenericTypeDefinition) ->
            communicator.SendStringAndParseTypeRef str |> int
//        | _ when t.IsGenericParameter -> t.MetadataToken
        | _ -> communicator.SendStringAndParseTypeSpec str |> int

    member x.PlaceProbes() =
        let instructions = x.rewriter.CopyInstructions()
        assert(not <| Array.isEmpty instructions)
        let mutable atLeastOneReturnFound = false
        let mutable hasPrefix = false
        let mutable prefixCell = instructions.[0]
        let mutable prefix : ilInstr byref = &prefixCell
        x.PlaceEnterProbe(&instructions.[0]) |> ignore
        for i in 0 .. instructions.Length - 1 do
            let instr = &instructions.[i]
            if not hasPrefix then prefix <- instr
            match instr.opcode with
            | OpCode op ->
                let prependTarget = if hasPrefix then &prefix else &instr
                let dumpedInfo = x.rewriter.ILInstrToString probes instr
                let idx = communicator.SendStringAndReadItsIndex dumpedInfo
                x.PrependProbe(probes.dumpInstruction, [OpCodes.Ldc_I4, idx |> int |> Arg32], x.tokens.void_u4_sig, &prependTarget) |> ignore
                let opcodeValue = LanguagePrimitives.EnumOfValue op.Value
                match opcodeValue with
                // Prefixes
                | OpCodeValues.Unaligned_
                | OpCodeValues.Volatile_
                | OpCodeValues.Tail_
                | OpCodeValues.Constrained_
                | OpCodeValues.Readonly_  ->
                    hasPrefix <- true

                // Concrete instructions
                | OpCodeValues.Ldarga_S ->
                    let index = int instr.Arg8
                    let size = x.m |> Reflection.getMethodArgumentType index |> TypeUtils.internalSizeOf |> int32
                    x.AppendProbe(probes.ldarga, [(OpCodes.Ldc_I4, Arg32 index); (OpCodes.Ldc_I4, Arg32 size)], x.tokens.void_i_u2_size_sig, instr)
                    x.AppendDup instr
                | OpCodeValues.Ldloca_S ->
                    let index = int instr.Arg8
                    let size = x.m.GetMethodBody().LocalVariables.[index].LocalType |> TypeUtils.internalSizeOf |> int32
                    x.AppendProbe(probes.ldloca, [(OpCodes.Ldc_I4, Arg32 index); (OpCodes.Ldc_I4, Arg32 size)], x.tokens.void_i_u2_size_sig, instr)
                    x.AppendDup instr
                | OpCodeValues.Ldarga ->
                    let index = int instr.Arg16
                    let size = x.m |> Reflection.getMethodArgumentType index |> TypeUtils.internalSizeOf |> int32
                    x.AppendProbe(probes.ldarga, [(OpCodes.Ldc_I4, Arg32 index); (OpCodes.Ldc_I4, Arg32 size)], x.tokens.void_i_u2_size_sig, instr)
                    x.AppendDup instr
                | OpCodeValues.Ldloca ->
                    let index = int instr.Arg16
                    let size = x.m.GetMethodBody().LocalVariables.[index].LocalType |> TypeUtils.internalSizeOf |> int32
                    x.AppendProbe(probes.ldloca, [(OpCodes.Ldc_I4, Arg32 index); (OpCodes.Ldc_I4, Arg32 size)], x.tokens.void_i_u2_size_sig, instr)
                    x.AppendDup instr
                | OpCodeValues.Ldnull
                | OpCodeValues.Ldc_I4_M1
                | OpCodeValues.Ldc_I4_0
                | OpCodeValues.Ldc_I4_1
                | OpCodeValues.Ldc_I4_2
                | OpCodeValues.Ldc_I4_3
                | OpCodeValues.Ldc_I4_4
                | OpCodeValues.Ldc_I4_5
                | OpCodeValues.Ldc_I4_6
                | OpCodeValues.Ldc_I4_7
                | OpCodeValues.Ldc_I4_8
                | OpCodeValues.Ldc_I4_S
                | OpCodeValues.Ldc_I4
                | OpCodeValues.Ldc_I8
                | OpCodeValues.Ldc_R4
                | OpCodeValues.Ldc_R8 -> x.AppendProbe(probes.ldc, [], x.tokens.void_sig, instr)
                | OpCodeValues.Pop -> x.AppendProbe(probes.pop, [], x.tokens.void_sig, instr)
                | OpCodeValues.Ldtoken -> x.AppendProbe(probes.ldtoken, [], x.tokens.void_sig, instr)
                | OpCodeValues.Arglist -> x.AppendProbe(probes.arglist, [], x.tokens.void_sig, instr)
                | OpCodeValues.Ldftn -> x.AppendProbe(probes.ldftn, [], x.tokens.void_sig, instr)
                | OpCodeValues.Sizeof -> x.AppendProbe(probes.sizeof, [], x.tokens.void_sig, instr)

                // Branchings
                | OpCodeValues.Brfalse_S
                | OpCodeValues.Brfalse -> x.PrependMemUnmemAndProbeWithOffset(probes.brfalse, [], x.tokens.void_offset_sig, &prependTarget) |> ignore
                | OpCodeValues.Brtrue_S
                | OpCodeValues.Brtrue -> x.PrependMemUnmemAndProbeWithOffset(probes.brtrue, [], x.tokens.void_offset_sig, &prependTarget) |> ignore
                | OpCodeValues.Switch -> x.PrependMemUnmemAndProbeWithOffset(probes.switch, [], x.tokens.void_offset_sig, &prependTarget) |> ignore

                // Symbolic stack instructions
                | OpCodeValues.Ldarg_0 -> x.AppendProbeWithOffset(probes.ldarg_0, [], x.tokens.void_offset_sig, instr)
                | OpCodeValues.Ldarg_1 -> x.AppendProbeWithOffset(probes.ldarg_1, [], x.tokens.void_offset_sig, instr)
                | OpCodeValues.Ldarg_2 -> x.AppendProbeWithOffset(probes.ldarg_2, [], x.tokens.void_offset_sig, instr)
                | OpCodeValues.Ldarg_3 -> x.AppendProbeWithOffset(probes.ldarg_3, [], x.tokens.void_offset_sig, instr)
                | OpCodeValues.Ldloc_0 -> x.AppendProbeWithOffset(probes.ldloc_0, [], x.tokens.void_offset_sig, instr)
                | OpCodeValues.Ldloc_1 -> x.AppendProbeWithOffset(probes.ldloc_1, [], x.tokens.void_offset_sig, instr)
                | OpCodeValues.Ldloc_2 -> x.AppendProbeWithOffset(probes.ldloc_2, [], x.tokens.void_offset_sig, instr)
                | OpCodeValues.Ldloc_3 -> x.AppendProbeWithOffset(probes.ldloc_3, [], x.tokens.void_offset_sig, instr)
                | OpCodeValues.Stloc_0 -> x.AppendProbeWithOffset(probes.stloc_0, [], x.tokens.void_offset_sig, instr)
                | OpCodeValues.Stloc_1 -> x.AppendProbeWithOffset(probes.stloc_1, [], x.tokens.void_offset_sig, instr)
                | OpCodeValues.Stloc_2 -> x.AppendProbeWithOffset(probes.stloc_2, [], x.tokens.void_offset_sig, instr)
                | OpCodeValues.Stloc_3 -> x.AppendProbeWithOffset(probes.stloc_3, [], x.tokens.void_offset_sig, instr)
                | OpCodeValues.Ldarg_S -> x.AppendProbeWithOffset(probes.ldarg_S, [(OpCodes.Ldc_I4, instr.Arg8 |> int |> Arg32)], x.tokens.void_u1_offset_sig, instr)
                | OpCodeValues.Starg_S -> x.AppendProbeWithOffset(probes.starg_S, [(OpCodes.Ldc_I4, instr.Arg8 |> int |> Arg32)], x.tokens.void_u1_offset_sig, instr)
                | OpCodeValues.Ldloc_S -> x.AppendProbeWithOffset(probes.ldloc_S, [(OpCodes.Ldc_I4, instr.Arg8 |> int |> Arg32)], x.tokens.void_u1_offset_sig, instr)
                | OpCodeValues.Stloc_S -> x.AppendProbeWithOffset(probes.stloc_S, [(OpCodes.Ldc_I4, instr.Arg8 |> int |> Arg32)], x.tokens.void_u1_offset_sig, instr)
                | OpCodeValues.Ldarg -> x.AppendProbeWithOffset(probes.ldarg, [(OpCodes.Ldc_I4, instr.Arg16 |> int |> Arg32)], x.tokens.void_u2_offset_sig, instr)
                | OpCodeValues.Starg -> x.AppendProbeWithOffset(probes.starg, [(OpCodes.Ldc_I4, instr.Arg16 |> int |> Arg32)], x.tokens.void_u2_offset_sig, instr)
                | OpCodeValues.Ldloc -> x.AppendProbeWithOffset(probes.ldloc, [(OpCodes.Ldc_I4, instr.Arg16 |> int |> Arg32)], x.tokens.void_u2_offset_sig, instr)
                | OpCodeValues.Stloc -> x.AppendProbeWithOffset(probes.stloc, [(OpCodes.Ldc_I4, instr.Arg16 |> int |> Arg32)], x.tokens.void_u2_offset_sig, instr)
                | OpCodeValues.Dup -> x.AppendProbeWithOffset(probes.dup, [], x.tokens.void_offset_sig, instr)

                | OpCodeValues.Add
                | OpCodeValues.Sub
                | OpCodeValues.Mul
                | OpCodeValues.Div
                | OpCodeValues.Div_Un
                | OpCodeValues.Rem
                | OpCodeValues.Rem_Un
                | OpCodeValues.And
                | OpCodeValues.Or
                | OpCodeValues.Xor
                | OpCodeValues.Shl
                | OpCodeValues.Shr
                | OpCodeValues.Shr_Un
                | OpCodeValues.Add_Ovf
                | OpCodeValues.Add_Ovf_Un
                | OpCodeValues.Mul_Ovf
                | OpCodeValues.Mul_Ovf_Un
                | OpCodeValues.Sub_Ovf
                | OpCodeValues.Sub_Ovf_Un
                | OpCodeValues.Ceq
                | OpCodeValues.Cgt
                | OpCodeValues.Cgt_Un
                | OpCodeValues.Clt
                | OpCodeValues.Clt_Un ->
                    // calli track_binop
                    // branch_true A
                    // calli mem2
                    // ldc op
                    // calli unmem 0
                    // calli unmem 1
                    // calli exec
                    // calli unmem 0
                    // calli unmem 1
                    // A: binop

                    let isUnchecked =
                        match opcodeValue with
                        | OpCodeValues.Add_Ovf
                        | OpCodeValues.Add_Ovf_Un
                        | OpCodeValues.Mul_Ovf
                        | OpCodeValues.Mul_Ovf_Un
                        | OpCodeValues.Sub_Ovf
                        | OpCodeValues.Sub_Ovf_Un -> false
                        | _ -> true

                    // Track
                    x.PrependProbe(probes.binOp, [], x.tokens.bool_sig, &prependTarget) |> ignore
                    let br = x.PrependBranch(OpCodes.Brtrue_S, &prependTarget)

                    // Mem and get exec with unmem
                    let execProbe, execSig, unmem1Probe, unmem1Sig, unmem2Probe, unmem2Sig =
                        match instr.stackState with // TODO: unify getting stackState #do
                        | BinOp(evaluationStackCellType.I4, evaluationStackCellType.I4)
                        | BinOp(evaluationStackCellType.I1, evaluationStackCellType.I1)
                        | BinOp(evaluationStackCellType.I1, evaluationStackCellType.I2)
                        | BinOp(evaluationStackCellType.I1, evaluationStackCellType.I4)
                        | BinOp(evaluationStackCellType.I2, evaluationStackCellType.I1)
                        | BinOp(evaluationStackCellType.I2, evaluationStackCellType.I2)
                        | BinOp(evaluationStackCellType.I2, evaluationStackCellType.I4)
                        | BinOp(evaluationStackCellType.I4, evaluationStackCellType.I1)
                        | BinOp(evaluationStackCellType.I4, evaluationStackCellType.I2) ->
                            x.PrependProbe(probes.mem2_4, [], x.tokens.void_i4_i4_sig, &prependTarget) |> ignore
                            (if isUnchecked then probes.execBinOp_4 else probes.execBinOp_4_ovf), x.tokens.void_u2_i4_i4_offset_sig,
                                probes.unmem_4, x.tokens.i4_i1_sig, probes.unmem_4, x.tokens.i4_i1_sig
                        | BinOp(evaluationStackCellType.I4, evaluationStackCellType.I8) ->
                            x.PrependProbe(probes.mem2_8_4, [], x.tokens.void_i8_i4_sig, &prependTarget) |> ignore
                            (if isUnchecked then probes.execBinOp_8_4 else probes.execBinOp_8_4_ovf), x.tokens.void_u2_i8_i4_offset_sig,
                                probes.unmem_8, x.tokens.i8_i1_sig, probes.unmem_4, x.tokens.i4_i1_sig
                        | BinOp(evaluationStackCellType.I8, evaluationStackCellType.I8) ->
                            x.PrependProbe(probes.mem2_8, [], x.tokens.void_i8_i8_sig, &prependTarget) |> ignore
                            (if isUnchecked then probes.execBinOp_8 else probes.execBinOp_8_ovf), x.tokens.void_u2_i8_i8_offset_sig,
                                probes.unmem_8, x.tokens.i8_i1_sig, probes.unmem_8, x.tokens.i8_i1_sig
                        | BinOp(evaluationStackCellType.R4, evaluationStackCellType.R4) ->
                            x.PrependProbe(probes.mem2_f4, [], x.tokens.void_r4_r4_sig, &prependTarget) |> ignore
                            (if isUnchecked then probes.execBinOp_f4 else probes.execBinOp_f4_ovf), x.tokens.void_u2_r4_r4_offset_sig,
                                probes.unmem_f4, x.tokens.r4_i1_sig, probes.unmem_f4, x.tokens.r4_i1_sig
                        | BinOp(evaluationStackCellType.R8, evaluationStackCellType.R8) ->
                            x.PrependProbe(probes.mem2_f8, [], x.tokens.void_r8_r8_sig, &prependTarget) |> ignore
                            (if isUnchecked then probes.execBinOp_f8 else probes.execBinOp_f8_ovf), x.tokens.void_u2_r8_r8_offset_sig,
                                probes.unmem_f8, x.tokens.r8_i1_sig, probes.unmem_f8, x.tokens.r8_i1_sig
                        | BinOp(evaluationStackCellType.I, evaluationStackCellType.I)
                        | BinOp(evaluationStackCellType.I, evaluationStackCellType.Ref)
                        | BinOp(evaluationStackCellType.Ref, evaluationStackCellType.I)
                        | BinOp(evaluationStackCellType.Ref, evaluationStackCellType.Ref) ->
                            x.PrependMem2_p &prependTarget
                            (if isUnchecked then probes.execBinOp_p else probes.execBinOp_p_ovf), x.tokens.void_u2_i_i_offset_sig,
                                probes.unmem_p, x.tokens.i_i1_sig, probes.unmem_p, x.tokens.i_i1_sig
                        | BinOp(evaluationStackCellType.I1, evaluationStackCellType.I)
                        | BinOp(evaluationStackCellType.I2, evaluationStackCellType.I)
                        | BinOp(evaluationStackCellType.I4, evaluationStackCellType.I)
                        | BinOp(evaluationStackCellType.I1, evaluationStackCellType.Ref)
                        | BinOp(evaluationStackCellType.I2, evaluationStackCellType.Ref)
                        | BinOp(evaluationStackCellType.I4, evaluationStackCellType.Ref) ->
                            x.PrependMem2_p_4 &prependTarget
                            (if isUnchecked then probes.execBinOp_p_4 else probes.execBinOp_p_4_ovf), x.tokens.void_u2_i_i4_offset_sig,
                                probes.unmem_p, x.tokens.i_i1_sig, probes.unmem_4, x.tokens.i4_i1_sig
                        | BinOp(evaluationStackCellType.I, evaluationStackCellType.I1)
                        | BinOp(evaluationStackCellType.I, evaluationStackCellType.I2)
                        | BinOp(evaluationStackCellType.I, evaluationStackCellType.I4)
                        | BinOp(evaluationStackCellType.Ref, evaluationStackCellType.I1)
                        | BinOp(evaluationStackCellType.Ref, evaluationStackCellType.I2)
                        | BinOp(evaluationStackCellType.Ref, evaluationStackCellType.I4) ->
                            x.PrependMem2_4_p &prependTarget
                            (if isUnchecked then probes.execBinOp_4_p else probes.execBinOp_4_p_ovf), x.tokens.void_u2_i4_i_offset_sig,
                                probes.unmem_4, x.tokens.i4_i1_sig, probes.unmem_p, x.tokens.i_i1_sig
                        | BinOp(x, y) -> internalfailf "Unexpected binop ([%O]%O) evaluation stack types: %O, %O" i opcodeValue x y
                        | stack -> internalfailf "Unexpected binop (%O) evaluation stack types! stack: %O" opcodeValue stack

                    x.PrependInstr(OpCodes.Ldc_I4, op.Value |> int |> Arg32 , &prependTarget)
                    x.PrependProbe(unmem1Probe, [(OpCodes.Ldc_I4, Arg32 0)], unmem1Sig, &prependTarget) |> ignore
                    x.PrependProbe(unmem2Probe, [(OpCodes.Ldc_I4, Arg32 1)], unmem2Sig, &prependTarget) |> ignore
                    x.PrependProbeWithOffset(execProbe, [], execSig, &prependTarget) |> ignore
                    x.PrependProbe(unmem1Probe, [(OpCodes.Ldc_I4, Arg32 0)], unmem1Sig, &prependTarget) |> ignore
                    x.PrependProbe(unmem2Probe, [(OpCodes.Ldc_I4, Arg32 1)], unmem2Sig, &prependTarget) |> ignore
                    br.arg <- Target prependTarget

                | OpCodeValues.Neg
                | OpCodeValues.Not ->
                    match instr.opcode with
                    | OpCode op -> x.AppendProbeWithOffset(probes.unOp, [(OpCodes.Ldc_I4, op.Value |> int |> Arg32)], x.tokens.void_u2_offset_sig, instr)
                    | _ -> __unreachable__()

                | OpCodeValues.Conv_I1
                | OpCodeValues.Conv_I2
                | OpCodeValues.Conv_I4
                | OpCodeValues.Conv_I8
                | OpCodeValues.Conv_R4
                | OpCodeValues.Conv_R8
                | OpCodeValues.Conv_U4
                | OpCodeValues.Conv_U8
                | OpCodeValues.Conv_R_Un
                | OpCodeValues.Conv_U2
                | OpCodeValues.Conv_U1
                | OpCodeValues.Conv_I
                | OpCodeValues.Conv_U ->
                    x.AppendProbeWithOffsetMemUnmem(probes.conv, [], x.tokens.void_offset_sig, &prependTarget, instr)
                | OpCodeValues.Conv_Ovf_I1_Un
                | OpCodeValues.Conv_Ovf_I2_Un
                | OpCodeValues.Conv_Ovf_I4_Un
                | OpCodeValues.Conv_Ovf_I8_Un
                | OpCodeValues.Conv_Ovf_U1_Un
                | OpCodeValues.Conv_Ovf_U2_Un
                | OpCodeValues.Conv_Ovf_U4_Un
                | OpCodeValues.Conv_Ovf_U8_Un
                | OpCodeValues.Conv_Ovf_I_Un
                | OpCodeValues.Conv_Ovf_U_Un
                | OpCodeValues.Conv_Ovf_I1
                | OpCodeValues.Conv_Ovf_U1
                | OpCodeValues.Conv_Ovf_I2
                | OpCodeValues.Conv_Ovf_U2
                | OpCodeValues.Conv_Ovf_I4
                | OpCodeValues.Conv_Ovf_U4
                | OpCodeValues.Conv_Ovf_I8
                | OpCodeValues.Conv_Ovf_U8
                | OpCodeValues.Conv_Ovf_I
                | OpCodeValues.Conv_Ovf_U ->
                    x.AppendProbeWithOffsetMemUnmem(probes.conv, [], x.tokens.void_offset_sig, &prependTarget, instr)

                | OpCodeValues.Ldind_I1
                | OpCodeValues.Ldind_U1
                | OpCodeValues.Ldind_I2
                | OpCodeValues.Ldind_U2
                | OpCodeValues.Ldind_I4
                | OpCodeValues.Ldind_U4
                | OpCodeValues.Ldind_I8
                | OpCodeValues.Ldind_I
                | OpCodeValues.Ldind_R4
                | OpCodeValues.Ldind_R8
                | OpCodeValues.Ldind_Ref ->
                    // dup
                    // calli track_ldind
                    // ldind
                    x.PrependDup &prependTarget
                    x.PrependProbeWithOffset(probes.ldind, [], x.tokens.void_i_offset_sig, &prependTarget) |> ignore

                | OpCodeValues.Stind_Ref
                | OpCodeValues.Stind_I1
                | OpCodeValues.Stind_I2
                | OpCodeValues.Stind_I4
                | OpCodeValues.Stind_I8
                | OpCodeValues.Stind_R4
                | OpCodeValues.Stind_R8
                | OpCodeValues.Stind_I ->
                    // calli mem2
                    // calli unmem 0
                    // calli track_stind
                    // branch_true A
                    // calli unmem 0
                    // calli unmem 1
                    // calli exec
                    // br B
                    // A: calli unmem 0
                    // calli unmem 1
                    // stind
                    // B:

                    let execProbe, execSig, unmem2Probe, unmem2Sig =
                        match opcodeValue with
                        | OpCodeValues.Stind_I ->
                            match instr.stackState with
                            | BinOp(evaluationStackCellType.I, evaluationStackCellType.I)
                            | BinOp(evaluationStackCellType.I, evaluationStackCellType.Ref) -> ()
                            | _ -> internalfail "Stack validation failed"
                            x.PrependMem2_p &prependTarget
                            probes.execStind_ref, x.tokens.void_i_i_offset_sig, probes.unmem_p, x.tokens.i_i1_sig
                        | OpCodeValues.Stind_Ref ->
                            match instr.stackState with
                            | BinOp(evaluationStackCellType.Ref, evaluationStackCellType.I)
                            | BinOp(evaluationStackCellType.Ref, evaluationStackCellType.Ref) -> ()
                            | _ -> internalfail "Stack validation failed"
                            x.PrependMem2_p &prependTarget
                            probes.execStind_ref, x.tokens.void_i_i_offset_sig, probes.unmem_p, x.tokens.i_i1_sig
                        | OpCodeValues.Stind_I1 ->
                            match instr.stackState with
                            | BinOp(evaluationStackCellType.I1, evaluationStackCellType.I)
                            | BinOp(evaluationStackCellType.I1, evaluationStackCellType.Ref) -> ()
                            | BinOp(evaluationStackCellType.I2, evaluationStackCellType.I)
                            | BinOp(evaluationStackCellType.I2, evaluationStackCellType.Ref) -> ()
                            | BinOp(evaluationStackCellType.I4, evaluationStackCellType.I)
                            | BinOp(evaluationStackCellType.I4, evaluationStackCellType.Ref) -> ()
                            | _ -> internalfail "Stack validation failed"
                            x.PrependMem2_p_1 &prependTarget
                            probes.execStind_I1, x.tokens.void_i_i1_offset_sig, probes.unmem_1, x.tokens.i1_i1_sig
                        | OpCodeValues.Stind_I2 ->
                            match instr.stackState with
                            | BinOp(evaluationStackCellType.I2, evaluationStackCellType.I)
                            | BinOp(evaluationStackCellType.I2, evaluationStackCellType.Ref) -> ()
                            | BinOp(evaluationStackCellType.I4, evaluationStackCellType.I)
                            | BinOp(evaluationStackCellType.I4, evaluationStackCellType.Ref) -> ()
                            | _ -> internalfail "Stack validation failed"
                            x.PrependMem2_p_2 &prependTarget
                            probes.execStind_I2, x.tokens.void_i_i2_offset_sig, probes.unmem_2, x.tokens.i2_i1_sig
                        | OpCodeValues.Stind_I4 ->
                            match instr.stackState with
                            | BinOp(evaluationStackCellType.I4, evaluationStackCellType.I)
                            | BinOp(evaluationStackCellType.I4, evaluationStackCellType.Ref) -> ()
                            | _ -> internalfail "Stack validation failed"
                            x.PrependMem2_p_4 &prependTarget
                            probes.execStind_I4, x.tokens.void_i_i4_offset_sig, probes.unmem_4, x.tokens.i4_i1_sig
                        | OpCodeValues.Stind_I8 ->
                            match instr.stackState with
                            | BinOp(evaluationStackCellType.I8, evaluationStackCellType.I)
                            | BinOp(evaluationStackCellType.I8, evaluationStackCellType.Ref) -> ()
                            | _ -> internalfail "Stack validation failed"
                            x.PrependMem2_p_8 &prependTarget
                            probes.execStind_I8, x.tokens.void_i_i8_offset_sig, probes.unmem_8, x.tokens.i8_i1_sig
                        | OpCodeValues.Stind_R4 ->
                            match instr.stackState with
                            | BinOp(evaluationStackCellType.R4, evaluationStackCellType.I)
                            | BinOp(evaluationStackCellType.R4, evaluationStackCellType.Ref) -> ()
                            | _ -> internalfail "Stack validation failed"
                            x.PrependMem2_p_f4 &prependTarget
                            probes.execStind_R4, x.tokens.void_i_r4_offset_sig, probes.unmem_f4, x.tokens.r4_i1_sig
                        | OpCodeValues.Stind_R8 ->
                            match instr.stackState with
                            | BinOp(evaluationStackCellType.R8, evaluationStackCellType.I)
                            | BinOp(evaluationStackCellType.R8, evaluationStackCellType.Ref) -> ()
                            | _ -> internalfail "Stack validation failed"
                            x.PrependMem2_p_f8 &prependTarget
                            probes.execStind_R8, x.tokens.void_i_r8_offset_sig, probes.unmem_f8, x.tokens.r8_i1_sig
                        | _ -> __unreachable__()

//                    x.PrependProbe(probes.unmem_p, [(OpCodes.Ldc_I4, Arg32 0)], x.tokens.i_i1_sig, &prependTarget) |> ignore
//                    x.PrependProbe(unmem2Probe, [(OpCodes.Ldc_I4, Arg32 1)], unmem2Sig, &prependTarget) |> ignore
                    x.PrependProbe(probes.unmem_p, [(OpCodes.Ldc_I4, Arg32 0)], x.tokens.i_i1_sig, &prependTarget) |> ignore
                    x.PrependInstr(OpCodes.Ldc_I4, Arg32 (x.SizeOfIndirection opcodeValue), &prependTarget)
                    x.PrependProbe(probes.stind, [], x.tokens.bool_i_i4_sig, &prependTarget) |> ignore
                    let br_true = x.PrependBranch(OpCodes.Brtrue_S, &prependTarget)
                    x.PrependProbe(probes.unmem_p, [(OpCodes.Ldc_I4, Arg32 0)], x.tokens.i_i1_sig, &prependTarget) |> ignore
                    x.PrependProbe(unmem2Probe, [(OpCodes.Ldc_I4, Arg32 1)], unmem2Sig, &prependTarget) |> ignore
                    x.PrependProbeWithOffset(execProbe, [], execSig, &prependTarget) |> ignore
                    let br = x.PrependBranch(OpCodes.Br, &prependTarget)
                    let unmem_p = x.PrependProbe(probes.unmem_p, [(OpCodes.Ldc_I4, Arg32 0)], x.tokens.i_i1_sig, &prependTarget)
                    br_true.arg <- Target unmem_p
                    x.PrependProbe(unmem2Probe, [(OpCodes.Ldc_I4, Arg32 1)], unmem2Sig, &prependTarget) |> ignore
                    br.arg <- Target instr.next

                | OpCodeValues.Mkrefany -> x.AppendProbe(probes.mkrefany, [], x.tokens.void_sig, instr)
                | OpCodeValues.Newarr ->
                    x.AppendProbeWithOffset(probes.newarr, [], x.tokens.void_i_token_offset_sig, instr)
                    x.AppendInstr OpCodes.Ldc_I4 instr.arg instr
                    x.AppendInstr OpCodes.Conv_I NoArg instr
                    x.AppendDup instr
                | OpCodeValues.Localloc ->
                    x.AppendProbeWithOffset(probes.newarr, [], x.tokens.void_i_offset_sig, instr)
                    x.AppendDup instr
                | OpCodeValues.Cpobj ->
                    // calli mem2
                    // calli unmem 0
                    // calli unmem 1
                    // calli unmem 0
                    // calli unmem 1
                    // calli track_cpobj
                    // branch_true A
                    // ldc token
                    // calli unmem 0
                    // calli unmem 1
                    // calli exec
                    // A: cpobj
                    x.PrependMem2_p &prependTarget
                    x.PrependProbe(probes.unmem_p, [(OpCodes.Ldc_I4, Arg32 0)], x.tokens.i_i1_sig, &prependTarget) |> ignore
                    x.PrependProbe(probes.unmem_p, [(OpCodes.Ldc_I4, Arg32 1)], x.tokens.i_i1_sig, &prependTarget) |> ignore
                    x.PrependProbe(probes.unmem_p, [(OpCodes.Ldc_I4, Arg32 0)], x.tokens.i_i1_sig, &prependTarget) |> ignore
                    x.PrependProbe(probes.unmem_p, [(OpCodes.Ldc_I4, Arg32 1)], x.tokens.i_i1_sig, &prependTarget) |> ignore
                    x.PrependProbe(probes.cpobj, [], x.tokens.bool_i_i_sig, &prependTarget) |> ignore
                    let br = x.PrependBranch(OpCodes.Brtrue_S, &prependTarget)
                    x.PrependInstr(OpCodes.Ldc_I4, instr.arg, &prependTarget)
                    x.PrependProbe(probes.unmem_p, [(OpCodes.Ldc_I4, Arg32 0)], x.tokens.i_i1_sig, &prependTarget) |> ignore
                    x.PrependProbe(probes.unmem_p, [(OpCodes.Ldc_I4, Arg32 1)], x.tokens.i_i1_sig, &prependTarget) |> ignore
                    x.PrependProbeWithOffset(probes.execCpobj, [], x.tokens.void_token_i_i_offset_sig, &prependTarget) |> ignore
                    br.arg <- Target prependTarget
                | OpCodeValues.Ldobj ->
                    x.PrependDup &prependTarget
                    x.PrependProbeWithOffset(probes.ldobj, [], x.tokens.void_i_offset_sig, &prependTarget) |> ignore
                | OpCodeValues.Ldstr ->
                    x.AppendProbe(probes.ldstr, [], x.tokens.void_i_sig, instr)
                    x.AppendInstr OpCodes.Conv_I NoArg instr
                    x.AppendInstr OpCodes.Dup NoArg instr
                | OpCodeValues.Castclass ->
                    x.PrependDup &prependTarget
                    x.PrependInstr(OpCodes.Conv_I, NoArg, &prependTarget)
                    x.PrependInstr(OpCodes.Ldc_I4, instr.arg, &prependTarget)
                    x.PrependProbe(probes.disableInstrumentation, [], x.tokens.void_sig, &prependTarget) |> ignore
                    x.PrependProbeWithOffset(probes.castclass, [], x.tokens.void_i_token_offset_sig, &prependTarget) |> ignore
                    x.PrependProbe(probes.enableInstrumentation, [], x.tokens.void_sig, &prependTarget) |> ignore
                | OpCodeValues.Isinst ->
                    x.PrependDup &prependTarget
                    x.PrependInstr(OpCodes.Ldc_I4, instr.arg, &prependTarget)
                    x.PrependProbeWithOffset(probes.isinst, [], x.tokens.void_i_token_offset_sig, &prependTarget) |> ignore
                | OpCodeValues.Unbox ->
                     x.PrependDup &prependTarget
                     x.PrependInstr(OpCodes.Ldc_I4, instr.arg, &prependTarget)
                     x.PrependProbeWithOffset(probes.unbox, [], x.tokens.void_i_token_offset_sig, &prependTarget) |> ignore
                     x.PrependProbe(probes.disableInstrumentation, [], x.tokens.void_sig, &prependTarget) |> ignore
                     x.AppendProbe(probes.enableInstrumentation, [], x.tokens.void_sig, instr)
                | OpCodeValues.Unbox_Any ->
                     x.PrependDup &prependTarget
                     x.PrependInstr(OpCodes.Conv_I, NoArg, &prependTarget)
                     x.PrependInstr(OpCodes.Ldc_I4, instr.arg, &prependTarget)
                     x.PrependProbe(probes.disableInstrumentation, [], x.tokens.void_sig, &prependTarget) |> ignore
                     x.PrependProbeWithOffset(probes.unboxAny, [], x.tokens.void_i_token_offset_sig, &prependTarget) |> ignore
                     x.AppendProbe(probes.enableInstrumentation, [], x.tokens.void_sig, instr)
                | OpCodeValues.Ldfld ->
                    let isStruct =
                        match instr.stackState with
                        | UnOp evaluationStackCellType.Struct
                        | UnOp evaluationStackCellType.RefLikeStruct -> true
                        | _ -> false
                    let fieldInfo = Reflection.resolveField x.m instr.Arg32
                    if not isStruct then
                        x.PrependMem_p(0, 0, &prependTarget)
                        x.PrependProbe(probes.unmem_p, [(OpCodes.Ldc_I4, Arg32 0)], x.tokens.i_i1_sig, &instr) |> ignore
                    let fieldOffset = CSharpUtils.LayoutUtils.GetFieldOffset fieldInfo
                    x.PrependInstr(OpCodes.Ldc_I4, Arg32 fieldOffset, &prependTarget)
                    let fieldSize = TypeUtils.internalSizeOf fieldInfo.FieldType
                    x.PrependInstr(OpCodes.Ldc_I4, Arg32 fieldSize, &prependTarget)
                    if isStruct then
                        x.PrependProbeWithOffset(probes.ldfld_struct, [], x.tokens.void_i4_i4_offset_sig, &prependTarget) |> ignore
                    else
                        x.PrependProbeWithOffset(probes.ldfld, [], x.tokens.void_i_i4_i4_offset_sig, &prependTarget) |> ignore
                        x.PrependProbe(probes.unmem_p, [(OpCodes.Ldc_I4, Arg32 0)], x.tokens.i_i1_sig, &instr) |> ignore
                | OpCodeValues.Ldflda ->
                    x.PrependDup &prependTarget
                    x.PrependInstr(OpCodes.Conv_I, NoArg, &prependTarget)
                    x.PrependInstr(OpCodes.Ldc_I4, instr.arg, &prependTarget)
                    x.PrependProbeWithOffset(probes.ldflda, [], x.tokens.void_i_token_offset_sig, &prependTarget) |> ignore
                | OpCodeValues.Stfld ->
                    // box [if struct]
                    // calli mem2
                    // ldc token
                    // calli unmem 0
                    // calli unmem 1
                    // calli track_stfld
                    // calli unmem 0
                    // calli unmem 1
                    // unbox [if struct]
                    // stfld

                    let isStruct, isRefLikeStruct =
                        match instr.stackState with
                        | UnOp evaluationStackCellType.Struct -> true, false
                        | UnOp evaluationStackCellType.RefLikeStruct -> false, true
                        | _ -> false, false
                    let fieldInfo = Reflection.resolveField x.m instr.Arg32
                    let fieldOffset = CSharpUtils.LayoutUtils.GetFieldOffset fieldInfo
                    let fieldSize = TypeUtils.internalSizeOf fieldInfo.FieldType |> int

                    if isRefLikeStruct then
                        match instr.stackState with
                        | Some (_ :: (_, src) :: _) ->
                            src |> Set.iter (fun srcOffset ->
                                let srcInstr = instructions |> Array.find (fun i -> i.offset = srcOffset)
                                x.AppendProbe(probes.mem_refLikeStruct, [OpCodes.Dup, NoArg; OpCodes.Conv_I, NoArg], x.tokens.void_i_sig, srcInstr))
                        | _ -> __unreachable__()
                        let args = [(OpCodes.Ldc_I4, Arg32 fieldOffset); (OpCodes.Ldc_I4, Arg32 fieldSize)]
                        x.PrependProbeWithOffset(probes.stfld_refLikeStruct, args, x.tokens.void_i4_i4_offset_sig, &prependTarget) |> ignore
                    else
                        if isStruct then
                            let typeToken = fieldInfo.DeclaringType |> x.AcceptTypeToken
                            // TODO: potential bug test:
                            //       ldloca
                            //       stfld symbolicValue
                            //       ldloc
                            //       ldfld
                            // TODO: if ldloca with stfld will use one address of location,
                            //       but ldloc with ldfld will use another address (because of Box)
                            x.PrependInstr(OpCodes.Box, Arg32 typeToken, &prependTarget)
                        let probe, signature, unmem2Probe, unmem2Sig =
                            match instr.stackState with
                            | BinOp(evaluationStackCellType.I1, evaluationStackCellType.I)
                            | BinOp(evaluationStackCellType.I2, evaluationStackCellType.I)
                            | BinOp(evaluationStackCellType.I4, evaluationStackCellType.I)
                            | BinOp(evaluationStackCellType.I1, evaluationStackCellType.Ref)
                            | BinOp(evaluationStackCellType.I2, evaluationStackCellType.Ref)
                            | BinOp(evaluationStackCellType.I4, evaluationStackCellType.Ref) ->
                                x.PrependMem2_p_4 &prependTarget
                                probes.stfld_4, x.tokens.void_i4_i_i4_offset_sig, probes.unmem_4, x.tokens.i4_i1_sig
                            | BinOp(evaluationStackCellType.I8, evaluationStackCellType.I)
                            | BinOp(evaluationStackCellType.I8, evaluationStackCellType.Ref) ->
                                x.PrependMem2_p_8 &prependTarget
                                probes.stfld_8, x.tokens.void_i4_i_i8_offset_sig, probes.unmem_8, x.tokens.i8_i1_sig
                            | BinOp(evaluationStackCellType.R4, evaluationStackCellType.I)
                            | BinOp(evaluationStackCellType.R4, evaluationStackCellType.Ref) ->
                                x.PrependMem2_p_f4 &prependTarget
                                probes.stfld_f4, x.tokens.void_i4_i_r4_offset_sig, probes.unmem_f4, x.tokens.r4_i1_sig
                            | BinOp(evaluationStackCellType.R8, evaluationStackCellType.I)
                            | BinOp(evaluationStackCellType.R8, evaluationStackCellType.Ref) ->
                                x.PrependMem2_p_f8 &prependTarget
                                probes.stfld_f8, x.tokens.void_i4_i_r8_offset_sig, probes.unmem_f8, x.tokens.r8_i1_sig
                            | BinOp(evaluationStackCellType.I, evaluationStackCellType.I)
                            | BinOp(evaluationStackCellType.I, evaluationStackCellType.Ref)
                            | BinOp(evaluationStackCellType.Ref, evaluationStackCellType.I)
                            | BinOp(evaluationStackCellType.Ref, evaluationStackCellType.Ref) ->
                                x.PrependMem2_p &prependTarget
                                probes.stfld_p, x.tokens.void_i4_i_i_offset_sig, probes.unmem_p, x.tokens.i_i1_sig
                            | BinOp(evaluationStackCellType.Struct, evaluationStackCellType.I)
                            | BinOp(evaluationStackCellType.Struct, evaluationStackCellType.Ref)
                            | BinOp(evaluationStackCellType.RefLikeStruct, evaluationStackCellType.I)
                            | BinOp(evaluationStackCellType.RefLikeStruct, evaluationStackCellType.Ref) ->
                                x.PrependMem2_p &prependTarget
                                probes.stfld_struct, x.tokens.void_i4_i4_i_i_offset_sig, probes.unmem_p, x.tokens.i_i1_sig
                            | _ -> __unreachable__()

                        x.PrependInstr(OpCodes.Ldc_I4, Arg32 fieldOffset, &prependTarget)
                        if isStruct then
                            x.PrependInstr(OpCodes.Ldc_I4, Arg32 fieldSize, &prependTarget)
                        x.PrependProbe(probes.unmem_p, [(OpCodes.Ldc_I4, Arg32 0)], x.tokens.i_i1_sig, &prependTarget) |> ignore
                        x.PrependProbe(unmem2Probe, [(OpCodes.Ldc_I4, Arg32 1)], unmem2Sig, &prependTarget) |> ignore
                        x.PrependProbeWithOffset(probe, [], signature, &prependTarget) |> ignore
                        x.PrependProbe(probes.unmem_p, [(OpCodes.Ldc_I4, Arg32 0)], x.tokens.i_i1_sig, &prependTarget) |> ignore
                        x.PrependProbe(unmem2Probe, [(OpCodes.Ldc_I4, Arg32 1)], unmem2Sig, &prependTarget) |> ignore
                        if isStruct then
                            // TODO: ref-like struct!!
                            let typeToken = fieldInfo.DeclaringType |> x.AcceptTypeToken
                            x.PrependInstr(OpCodes.Unbox_Any, Arg32 typeToken, &prependTarget)
                | OpCodeValues.Ldsfld ->
                    x.PrependInstr(OpCodes.Ldc_I4, instr.arg, &prependTarget)
                    x.PrependProbeWithOffset(probes.ldsfld, [], x.tokens.void_token_offset_sig, &prependTarget) |> ignore
                | OpCodeValues.Ldsflda ->
                    let fieldInfo = Reflection.resolveField x.m instr.Arg32
                    let fieldSize = TypeUtils.internalSizeOf fieldInfo.FieldType |> int32
                    let id = registerStaticFieldID fieldInfo |> int16
                    x.AppendProbe(probes.ldsflda, [], x.tokens.void_i_i4_i2_sig, instr)
                    x.AppendInstr OpCodes.Ldc_I4 (Arg16 id) instr
                    x.AppendInstr OpCodes.Ldc_I4 (Arg32 fieldSize) instr
                    x.AppendInstr OpCodes.Conv_I NoArg instr
                    x.AppendDup instr
                | OpCodeValues.Stsfld ->
                    x.PrependInstr(OpCodes.Ldc_I4, instr.arg, &prependTarget)
                    x.PrependProbeWithOffset(probes.stsfld, [], x.tokens.void_token_offset_sig, &prependTarget) |> ignore
                | OpCodeValues.Stobj -> __notImplemented__() // ?????????????????
                | OpCodeValues.Box ->
                    x.AppendProbeWithOffset(probes.box, [], x.tokens.void_i_offset_sig, instr)
                    x.AppendInstr OpCodes.Conv_I NoArg instr
                    x.AppendDup instr
                | OpCodeValues.Ldlen ->
                    x.PrependInstr(OpCodes.Conv_I, NoArg, &prependTarget)
                    x.PrependProbe(probes.mem_p, [], x.tokens.void_i_sig, &prependTarget) |> ignore
                    x.PrependProbe(probes.unmem_p, [(OpCodes.Ldc_I4, Arg32 0)], x.tokens.i_i1_sig, &prependTarget) |> ignore
                    x.PrependProbeWithOffset(probes.ldlen, [], x.tokens.void_i_offset_sig, &prependTarget) |> ignore
                    x.PrependProbe(probes.unmem_p, [(OpCodes.Ldc_I4, Arg32 0)], x.tokens.i_i1_sig, &prependTarget) |> ignore
                | OpCodeValues.Ldelema
                | OpCodeValues.Ldelem_I1
                | OpCodeValues.Ldelem_U1
                | OpCodeValues.Ldelem_I2
                | OpCodeValues.Ldelem_U2
                | OpCodeValues.Ldelem_I4
                | OpCodeValues.Ldelem_U4
                | OpCodeValues.Ldelem_I8
                | OpCodeValues.Ldelem_I
                | OpCodeValues.Ldelem_R4
                | OpCodeValues.Ldelem_R8
                | OpCodeValues.Ldelem_Ref
                | OpCodeValues.Ldelem ->
                    let track = if opcodeValue = OpCodeValues.Ldelema then probes.ldelema else probes.ldelem
                    let exec = if opcodeValue = OpCodeValues.Ldelema then probes.execLdelema else probes.execLdelem
                    // calli mem2
                    // calli unmem 0
                    // calli unmem 1
                    // calli unmem 0
                    // calli unmem 1
                    // ldc size of elem
                    // calli track_ldelem(a)
                    // branch_true A
                    // calli unmem 0
                    // calli unmem 1
                    // calli exec
                    // A: ldelem(a)

                    let elemSize =
                        match opcodeValue with
                        | OpCodeValues.Ldelem_I
                        | OpCodeValues.Ldelem_Ref -> sizeof<System.IntPtr>
                        | OpCodeValues.Ldelem_I1 -> sizeof<int8>
                        | OpCodeValues.Ldelem_I2 -> sizeof<int16>
                        | OpCodeValues.Ldelem_I4 -> sizeof<int32>
                        | OpCodeValues.Ldelem_I8 -> sizeof<int64>
                        | OpCodeValues.Ldelem_R4 -> sizeof<float>
                        | OpCodeValues.Ldelem_R8 -> sizeof<double>
                        | OpCodeValues.Ldelem
                        | OpCodeValues.Ldelema ->
                            Reflection.resolveType x.m instr.Arg32 |> TypeUtils.internalSizeOf |> int
                        | _ -> __unreachable__()

                    x.PrependMem2_p &prependTarget
                    x.PrependProbe(probes.unmem_p, [(OpCodes.Ldc_I4, Arg32 0)], x.tokens.i_i1_sig, &prependTarget) |> ignore
                    x.PrependProbe(probes.unmem_p, [(OpCodes.Ldc_I4, Arg32 1)], x.tokens.i_i1_sig, &prependTarget) |> ignore
                    x.PrependProbe(probes.unmem_p, [(OpCodes.Ldc_I4, Arg32 0)], x.tokens.i_i1_sig, &prependTarget) |> ignore
                    x.PrependProbe(probes.unmem_p, [(OpCodes.Ldc_I4, Arg32 1)], x.tokens.i_i1_sig, &prependTarget) |> ignore
                    x.PrependInstr(OpCodes.Ldc_I4, Arg32 elemSize, &prependTarget)
                    x.PrependProbe(track, [], x.tokens.bool_i_i_i4_sig, &prependTarget) |> ignore
                    let br = x.PrependBranch(OpCodes.Brtrue_S, &prependTarget)
                    x.PrependProbe(probes.unmem_p, [(OpCodes.Ldc_I4, Arg32 0)], x.tokens.i_i1_sig, &prependTarget) |> ignore
                    x.PrependProbe(probes.unmem_p, [(OpCodes.Ldc_I4, Arg32 1)], x.tokens.i_i1_sig, &prependTarget) |> ignore
                    x.PrependProbeWithOffset(exec, [], x.tokens.void_i_i_offset_sig, &prependTarget) |> ignore
                    br.arg <- Target prependTarget

                | OpCodeValues.Stelem_I
                | OpCodeValues.Stelem_I1
                | OpCodeValues.Stelem_I2
                | OpCodeValues.Stelem_I4
                | OpCodeValues.Stelem_I8
                | OpCodeValues.Stelem_R4
                | OpCodeValues.Stelem_R8
                | OpCodeValues.Stelem_Ref
                | OpCodeValues.Stelem ->
                    // TODO: remove unmem before exec, take it from storage!
                    // box [if struct]
                    // calli mem3
                    // calli unmem 0
                    // calli unmem 1
                    // ldc size of elem
                    // calli track_stelem
                    // brtrue A
                    // calli unmem 0
                    // calli unmem 1
                    // calli unmem 2
                    // calli exec
                    // br B
                    // A: calli unmem 0
                    // calli unmem 1
                    // calli unmem 2
                    // unbox [if struct]
                    // stelem
                    // B:

                    let isStruct =
                        match instr.stackState with
                        | UnOp evaluationStackCellType.Struct ->
                            assert(op = OpCodes.Stelem)
                            true
                        | UnOp evaluationStackCellType.RefLikeStruct ->
                            // Ref-like structs can't be stored into arrays
                            __unreachable__()
                        | _ -> false
                    let typeTokenArg = instr.arg

                    if isStruct then
                        x.PrependInstr(OpCodes.Box, typeTokenArg, &prependTarget)

                    let mutable elemSize = 0
                    let execProbe, execSig, unmem3Probe, unmem3Sig =
                        match opcodeValue, instr.stackState with
                        | OpCodeValues.Stelem_I, _
                        | OpCodeValues.Stelem, TernOp(evaluationStackCellType.I, _, evaluationStackCellType.Ref) ->
                            x.PrependMem_p(2, 0, &prependTarget)
                            elemSize <- sizeof<System.IntPtr>
                            probes.execStelem_Ref, x.tokens.void_i_i_i_offset_sig, probes.unmem_p, x.tokens.i_i1_sig
                        | OpCodeValues.Stelem_Ref, _
                        | OpCodeValues.Stelem, TernOp(evaluationStackCellType.Ref, _, evaluationStackCellType.Ref) ->
                            x.PrependMem_p(2, 0, &prependTarget)
                            elemSize <- sizeof<System.IntPtr>
                            probes.execStind_ref, x.tokens.void_i_i_i_offset_sig, probes.unmem_p, x.tokens.i_i1_sig
                        | OpCodeValues.Stelem_I1, _
                        | OpCodeValues.Stelem, TernOp(evaluationStackCellType.I1, _, evaluationStackCellType.Ref) ->
                            x.PrependMem_i1(2, 0, &prependTarget)
                            elemSize <- sizeof<int8>
                            probes.execStelem_I1, x.tokens.void_i_i_i1_offset_sig, probes.unmem_1, x.tokens.i1_i1_sig
                        | OpCodeValues.Stelem_I2, _
                        | OpCodeValues.Stelem, TernOp(evaluationStackCellType.I2, _, evaluationStackCellType.Ref) ->
                            x.PrependMem_i2(2, 0, &prependTarget)
                            elemSize <- sizeof<int16>
                            probes.execStelem_I2, x.tokens.void_i_i_i2_offset_sig, probes.unmem_2, x.tokens.i2_i1_sig
                        | OpCodeValues.Stelem_I4, _
                        | OpCodeValues.Stelem, TernOp(evaluationStackCellType.I4, _, evaluationStackCellType.Ref) ->
                            x.PrependMem_i4(2, 0, &prependTarget)
                            elemSize <- sizeof<int32>
                            probes.execStelem_I4, x.tokens.void_i_i_i4_offset_sig, probes.unmem_4, x.tokens.i4_i1_sig
                        | OpCodeValues.Stelem_I8, _
                        | OpCodeValues.Stelem, TernOp(evaluationStackCellType.I8, _, evaluationStackCellType.Ref) ->
                            x.PrependMem_i8(2, 0, &prependTarget)
                            elemSize <- sizeof<int64>
                            probes.execStelem_I8, x.tokens.void_i_i_i8_offset_sig, probes.unmem_8, x.tokens.i8_i1_sig
                        | OpCodeValues.Stelem_R4, _
                        | OpCodeValues.Stelem, TernOp(evaluationStackCellType.R4, _, evaluationStackCellType.Ref) ->
                            x.PrependMem_f4(2, 0, &prependTarget)
                            elemSize <- sizeof<float>
                            probes.execStelem_R4, x.tokens.void_i_i_r4_offset_sig, probes.unmem_f4, x.tokens.r4_i1_sig
                        | OpCodeValues.Stelem_R8, _
                        | OpCodeValues.Stelem, TernOp(evaluationStackCellType.R8, _, evaluationStackCellType.Ref) ->
                            x.PrependMem_f8(2, 0, &prependTarget)
                            elemSize <- sizeof<double>
                            probes.execStelem_R8, x.tokens.void_i_i_r8_offset_sig, probes.unmem_f8, x.tokens.r8_i1_sig
                        | OpCodeValues.Stelem, TernOp(evaluationStackCellType.Struct, _, evaluationStackCellType.Ref)
                        | OpCodeValues.Stelem, TernOp(evaluationStackCellType.RefLikeStruct, _, evaluationStackCellType.Ref) ->
                            x.PrependMem_p(2, 0, &prependTarget)
                            elemSize <- Reflection.resolveType x.m instr.Arg32 |> TypeUtils.internalSizeOf |> int
                            probes.execStelem_Struct, x.tokens.void_i_i_i_offset_sig, probes.unmem_p, x.tokens.i_i1_sig
                        | _ -> __unreachable__()

                    x.PrependMem_p(1, 1, &prependTarget)
                    x.PrependMem_p(0, 2, &prependTarget)

                    x.PrependProbe(probes.unmem_p, [(OpCodes.Ldc_I4, Arg32 0)], x.tokens.i_i1_sig, &prependTarget) |> ignore
                    x.PrependProbe(probes.unmem_p, [(OpCodes.Ldc_I4, Arg32 1)], x.tokens.i_i1_sig, &prependTarget) |> ignore
                    x.PrependInstr(OpCodes.Ldc_I4, Arg32 elemSize, &prependTarget)
                    x.PrependProbe(probes.stelem, [], x.tokens.bool_i_i_i4_sig, &prependTarget) |> ignore
                    let brtrue = x.PrependBranch(OpCodes.Brtrue_S, &prependTarget)
                    x.PrependProbe(probes.unmem_p, [(OpCodes.Ldc_I4, Arg32 0)], x.tokens.i_i1_sig, &prependTarget) |> ignore
                    x.PrependProbe(probes.unmem_p, [(OpCodes.Ldc_I4, Arg32 1)], x.tokens.i_i1_sig, &prependTarget) |> ignore
                    x.PrependProbe(unmem3Probe, [(OpCodes.Ldc_I4, Arg32 2)], unmem3Sig, &prependTarget) |> ignore
                    x.PrependProbeWithOffset(execProbe, [], execSig, &prependTarget) |> ignore
                    let br = x.PrependBranch(OpCodes.Br, &prependTarget)
                    let tgt = x.PrependProbe(probes.unmem_p, [(OpCodes.Ldc_I4, Arg32 0)], x.tokens.i_i1_sig, &prependTarget)
                    x.PrependProbe(probes.unmem_p, [(OpCodes.Ldc_I4, Arg32 1)], x.tokens.i_i1_sig, &prependTarget) |> ignore
                    x.PrependProbe(unmem3Probe, [(OpCodes.Ldc_I4, Arg32 2)], unmem3Sig, &prependTarget) |> ignore
                    if isStruct then
                        x.PrependInstr(OpCodes.Unbox_Any, typeTokenArg, &prependTarget)
                    brtrue.arg <- Target tgt
                    x.AppendInstr OpCodes.Nop NoArg instr
                    br.arg <- Target instr.next

                | OpCodeValues.Ckfinite ->  x.AppendProbe(probes.ckfinite, [], x.tokens.void_sig, instr)
                | OpCodeValues.Ldvirtftn ->
                     x.PrependDup &prependTarget
                     x.PrependInstr(OpCodes.Ldc_I4, instr.arg, &prependTarget)
                     x.PrependProbeWithOffset(probes.ldvirtftn, [], x.tokens.void_i_token_offset_sig, &prependTarget) |> ignore
                | OpCodeValues.Initobj ->
                     x.PrependDup &prependTarget
                     x.PrependProbe(probes.initobj, [], x.tokens.void_i_sig, &prependTarget) |> ignore
                | OpCodeValues.Cpblk ->
                    // calli mem3
                    // calli unmem 0
                    // calli unmem 1
                    // calli unmem 2
                    // calli unmem 0
                    // calli unmem 1
                    // calli track_cpblk
                    // branch_true A
                    // calli unmem 0
                    // calli unmem 1
                    // calli unmem 2
                    // calli exec
                    // A: cpblk
                    x.PrependMem3_p &prependTarget
                    x.PrependProbe(probes.unmem_p, [(OpCodes.Ldc_I4, Arg32 0)], x.tokens.i_i1_sig, &prependTarget) |> ignore
                    x.PrependProbe(probes.unmem_p, [(OpCodes.Ldc_I4, Arg32 1)], x.tokens.i_i1_sig, &prependTarget) |> ignore
                    x.PrependProbe(probes.unmem_p, [(OpCodes.Ldc_I4, Arg32 2)], x.tokens.i_i1_sig, &prependTarget) |> ignore
                    x.PrependProbe(probes.unmem_p, [(OpCodes.Ldc_I4, Arg32 0)], x.tokens.i_i1_sig, &prependTarget) |> ignore
                    x.PrependProbe(probes.unmem_p, [(OpCodes.Ldc_I4, Arg32 1)], x.tokens.i_i1_sig, &prependTarget) |> ignore
                    x.PrependProbe(probes.cpblk, [], x.tokens.bool_i_i_sig, &prependTarget) |> ignore
                    let br = x.PrependBranch(OpCodes.Brtrue_S, &prependTarget)
                    x.PrependProbe(probes.unmem_p, [(OpCodes.Ldc_I4, Arg32 0)], x.tokens.i_i1_sig, &prependTarget) |> ignore
                    x.PrependProbe(probes.unmem_p, [(OpCodes.Ldc_I4, Arg32 1)], x.tokens.i_i1_sig, &prependTarget) |> ignore
                    x.PrependProbe(probes.unmem_p, [(OpCodes.Ldc_I4, Arg32 2)], x.tokens.i_i1_sig, &prependTarget) |> ignore
                    x.PrependProbeWithOffset(probes.execCpblk, [], x.tokens.void_i_i_i_offset_sig, &prependTarget) |> ignore
                    br.arg <- Target prependTarget
                | OpCodeValues.Initblk ->
                    // calli mem3
                    // calli unmem 0
                    // calli unmem 1
                    // calli unmem 2
                    // calli unmem 0
                    // calli track_initblk
                    // branch_true A
                    // calli unmem 0
                    // calli unmem 1
                    // calli unmem 2
                    // calli exec
                    // A: initblk
                    x.PrependMem3_p_i1_p &prependTarget
                    x.PrependProbe(probes.unmem_p, [(OpCodes.Ldc_I4, Arg32 0)], x.tokens.i_i1_sig, &prependTarget) |> ignore
                    x.PrependProbe(probes.unmem_1, [(OpCodes.Ldc_I4, Arg32 1)], x.tokens.i1_i1_sig, &prependTarget) |> ignore
                    x.PrependProbe(probes.unmem_p, [(OpCodes.Ldc_I4, Arg32 2)], x.tokens.i_i1_sig, &prependTarget) |> ignore
                    x.PrependProbe(probes.unmem_p, [(OpCodes.Ldc_I4, Arg32 0)], x.tokens.i_i1_sig, &prependTarget) |> ignore
                    x.PrependProbe(probes.initblk, [], x.tokens.bool_i_sig, &prependTarget) |> ignore
                    let br = x.PrependBranch(OpCodes.Brtrue_S, &prependTarget)
                    x.PrependProbe(probes.unmem_1, [(OpCodes.Ldc_I4, Arg32 1)], x.tokens.i1_i1_sig, &prependTarget) |> ignore
                    x.PrependProbe(probes.unmem_p, [(OpCodes.Ldc_I4, Arg32 0)], x.tokens.i_i1_sig, &prependTarget) |> ignore
                    x.PrependProbe(probes.unmem_1, [(OpCodes.Ldc_I4, Arg32 1)], x.tokens.i1_i1_sig, &prependTarget) |> ignore
                    x.PrependProbe(probes.unmem_p, [(OpCodes.Ldc_I4, Arg32 2)], x.tokens.i_i1_sig, &prependTarget) |> ignore
                    x.PrependProbeWithOffset(probes.execInitblk, [], x.tokens.void_i_i1_i_offset_sig, &prependTarget) |> ignore
                    br.arg <- Target prependTarget

                | OpCodeValues.Rethrow ->
                    atLeastOneReturnFound <- true
                    x.PrependProbeWithOffset(probes.rethrow, [], x.tokens.void_offset_sig, &prependTarget) |> ignore

                | OpCodeValues.Call
                | OpCodeValues.Callvirt
                | OpCodeValues.Newobj ->
                    // ldc argsCount
                    // calli track_call
                    // branch_true A
                    // if callee is internal call {
                    //    mem args
                    //    ldc default return value
                    //    mem default value
                    //    calli exec
                    //    unmem default value
                    //    if opcode <> newobj {
                    //      branch B
                    //    } else {
                    //      unmem args
                    //    }
                    // } else {
                    //    calli exec
                    // }
                    // A : nop
                    // calli pushFrame
                    // call callee
                    // calli finalizeCall
                    // B: nop
                    // if call opcode is newobj {
                    //    dup
                    //    conv.i
                    //    calli newobj_probe
                    // }

                    match instr.arg with
                    | Arg32 token ->
                        // TODO: if method is F# internal call, instr.arg <- token of static ctor of type #do
                        // TODO: if method is C# internal call, instr.arg <- token of C# implementation #do
                        let callee = Reflection.resolveMethod x.m token

                        if opcodeValue = OpCodeValues.Newobj then
                            if callee.DeclaringType.IsValueType then
                                x.AppendProbe(probes.newobjStruct, [], x.tokens.void_sig, instr)
                            else
                                x.AppendProbe(probes.newobj, [], x.tokens.void_i_sig, instr)
                                x.AppendInstr OpCodes.Conv_I NoArg instr
                                x.AppendDup(instr)
                        let returnValues = if Reflection.hasNonVoidResult callee then 1 else 0
                        let nop = x.AppendNop instr
                        x.AppendProbe(probes.finalizeCall, [(OpCodes.Ldc_I4, Arg32 returnValues)], x.tokens.void_u1_sig, instr)

                        let parameters = callee.GetParameters()
                        let hasThis = Reflection.hasThis callee && opcodeValue <> OpCodeValues.Newobj
                        let argsCount = parameters.Length
                        let argsCount = if hasThis then argsCount + 1 else argsCount
//                        let types = communicator.SendMethodTokenAndParseTypes token
                        x.PrependProbe(probes.call, [(OpCodes.Ldc_I4, Arg32 argsCount)], x.tokens.bool_u2_sig, &prependTarget) |> ignore
                        let br_true = x.PrependBranch(OpCodes.Brtrue_S, &prependTarget)
                        let calleeMethod = Application.getMethod callee
                        let isInternalCall = calleeMethod.IsInternalCall
                        if isInternalCall then
                            let unmems = List<uint64 * uint32>()
                            match instr.stackState with
                            | Some list ->
                                let types = List.take argsCount list |> Array.ofList
                                for i = 0 to argsCount - 1 do
                                    let t = fst types.[i]
                                    let unmem, token = x.PrependMemUnmemForType(t, argsCount - i - 1, i, &prependTarget)
                                    unmems.Add(unmem, token)
                            | None -> internalfail "unexpected stack state"
                            // TODO: if internal call raised exception, raise it in concolic
                            let retType = Reflection.getMethodReturnType callee
                            if retType <> typeof<System.Void> then
                                x.PrependLdcDefault(retType, &instr)
                                let probe, token = x.PrependMemUnmemForType(EvaluationStackTyper.abstractType retType, argsCount, argsCount, &prependTarget)
                                x.PrependProbeWithOffset(probes.execCall, [(OpCodes.Ldc_I4, Arg32 argsCount)], x.tokens.void_i4_offset_sig, &prependTarget) |> ignore
                                x.PrependProbe(probe, [(OpCodes.Ldc_I4, Arg32 argsCount)], token, &prependTarget) |> ignore
                            else
                                x.PrependProbeWithOffset(probes.execCall, [(OpCodes.Ldc_I4, Arg32 argsCount)], x.tokens.void_i4_offset_sig, &prependTarget) |> ignore
                            if opcodeValue <> OpCodeValues.Newobj then
                                let br = x.PrependBranch(OpCodes.Br, &prependTarget)
                                br.arg <- Target nop
                            else
                                let argsTypes = parameters |> Array.map (fun p -> p.ParameterType)
                                let argsTypes =
                                    if hasThis
                                        then Array.append [|callee.DeclaringType|] argsTypes
                                        else argsTypes
                                for i = argsCount - 1 downto 0 do
                                    let probe, token = unmems.[i]
                                    x.PrependProbe(probe, [(OpCodes.Ldc_I4, Arg32 (argsCount - 1 - i))], token, &prependTarget) |> ignore
                                    let t = argsTypes.[argsCount - 1 - i]
                                    if not t.IsValueType && t.IsAssignableTo(typeof<obj>) then
                                        let token = x.AcceptTypeToken t
                                        x.PrependProbe(probes.disableInstrumentation, [], x.tokens.void_sig, &prependTarget) |> ignore
                                        x.PrependInstr(OpCodes.Unbox_Any, Arg32 token, &prependTarget)
                                        x.PrependProbe(probes.enableInstrumentation, [], x.tokens.void_sig, &prependTarget) |> ignore
                        else x.PrependProbeWithOffset(probes.execCall, [(OpCodes.Ldc_I4, Arg32 argsCount)], x.tokens.void_i4_offset_sig, &prependTarget) |> ignore

                        let callStart = x.PrependNop(&prependTarget)
                        br_true.arg <- Target callStart
                        // TODO: this code produces proper unmems
//                        let argsTypes = parameters |> Array.map (fun p -> p.ParameterType)
//                        let argsTypes =
//                            if hasThis
//                                then Array.append [|callee.DeclaringType|] argsTypes
//                                else argsTypes
//                        for i = argsCount - 1 downto 0 do
//                            let probe, token = unmems.[i]
//                            x.PrependProbe(probe, [(OpCodes.Ldc_I4, Arg32 (argsCount - 1 - i))], token, &prependTarget) |> ignore
//                            let t = argsTypes.[argsCount - 1 - i]
//                            if not t.IsValueType && t.IsAssignableTo(typeof<obj>) then
//                                let token = x.AcceptTypeToken t
//                                x.PrependProbe(probes.disableInstrumentation, [], x.tokens.void_sig, &prependTarget) |> ignore
//                                x.PrependInstr(OpCodes.Unbox_Any, Arg32 token, &prependTarget)
//                                x.PrependProbe(probes.enableInstrumentation, [], x.tokens.void_sig, &prependTarget) |> ignore
                        let expectedToken = if opcodeValue = OpCodeValues.Callvirt then 0 else callee.MetadataToken
                        let pushFrameArgs = [(OpCodes.Ldc_I4, Arg32 token)
                                             (OpCodes.Ldc_I4, Arg32 expectedToken)
                                             (OpCodes.Ldc_I4, Arg32 (if opcodeValue = OpCodeValues.Newobj then 1 else 0))
                                             (OpCodes.Ldc_I4, Arg32 argsCount)]
                        x.PrependProbeWithOffset(probes.pushFrame, pushFrameArgs, x.tokens.void_token_token_bool_u2_offset_sig, &prependTarget) |> ignore
                    | _ -> __unreachable__()
                | OpCodeValues.Calli -> __notImplemented__()

                | OpCodeValues.Ret ->
                    assert (not hasPrefix)
                    atLeastOneReturnFound <- true
                    x.PlaceLeaveProbe &instr
                | OpCodeValues.Throw ->
                    x.PrependProbeWithOffset(probes.throw, [], x.tokens.void_offset_sig, &prependTarget) |> ignore
                    atLeastOneReturnFound <- true

                // Ignored instructions
                | OpCodeValues.Nop
                | OpCodeValues.Break
                | OpCodeValues.Jmp
                | OpCodeValues.Refanyval
                | OpCodeValues.Refanytype
                | OpCodeValues.Endfinally
                | OpCodeValues.Br_S
                | OpCodeValues.Br
                | OpCodeValues.Leave
                | OpCodeValues.Leave_S
                | OpCodeValues.Endfilter -> ()
                | _ -> __unreachable__()

                if hasPrefix && op.OpCodeType <> OpCodeType.Prefix then
                    hasPrefix <- false
                instructions.[i] <- instr
            | SwitchArg -> ()
        assert atLeastOneReturnFound

    member x.Skip (body : rawMethodBody) =
        { properties = {ilCodeSize = body.properties.ilCodeSize; maxStackSize = body.properties.maxStackSize}; il = body.il; ehs = body.ehs}

    member private x.NeedToSkip = Array.empty

    member x.Instrument(body : rawMethodBody) =
        assert(x.rewriter = null)
        x.tokens <- body.tokens
        // TODO: call Application.getMethod and take ILRewriter there!
        x.rewriter <- ILRewriter(body)
        x.m <- x.rewriter.Method
        let t = x.m.DeclaringType
        if typeof<System.Exception>.IsAssignableFrom(t) then
            internalfailf "Incorrect instrumentation: exception %O is thrown!" t
        let shouldInstrument = Array.contains (Reflection.methodToString x.m) x.NeedToSkip |> not
        let result =
            if shouldInstrument && Instrumenter.instrumentedFunctions.Add x.m then
                Logger.trace "Instrumenting %s (token = %X)" (Reflection.methodToString x.m) body.properties.token
                x.rewriter.Import()
                x.rewriter.PrintInstructions "before instrumentation" probes
//                Logger.trace "Placing probes..."
                x.PlaceProbes()
//                Logger.trace "Done placing probes!"
                x.rewriter.PrintInstructions "after instrumentation" probes
//                Logger.trace "Exporting..."
                let result = x.rewriter.Export()
//                x.rewriter.PrintInstructions "after export" probes
//                Logger.trace "Exported!"
                result
            else
                Logger.trace "Duplicate JITting of %s" x.MethodName
                x.Skip body

        x.rewriter <- null
        result
