namespace VSharp.Concolic

open System
open System.Collections.Generic
open VSharp
open VSharp.Core
open VSharp.CSharpUtils
open Reflection

type ConcolicMemory(communicator : Communicator) =
    let mutable physicalAddresses = Dictionary<UIntPtr, concreteHeapAddress Lazy>()
    let mutable virtualAddresses = Dictionary<concreteHeapAddress, UIntPtr>()
    let mutable unmarshalledAddresses = HashSet()

    let zeroHeapAddress = VectorTime.zero

    let readHeapBytes address offset size (t : Type) =
        match t with
        | _ when t.IsPrimitive || t.IsEnum ->
            let bytes = communicator.ReadHeapBytes address offset size Array.empty
            bytesToObj bytes t
        | _ when TypeUtils.isStruct t ->
            let fieldOffsets = fieldsWithOffsets t
            let refOffsets = chooseRefOffsets fieldOffsets
            let bytes = communicator.ReadHeapBytes address offset size refOffsets
            parseFields bytes fieldOffsets |> FieldsData |> box
        | _ ->
            assert(not t.IsValueType)
            let bytes = communicator.ReadHeapBytes address offset size (Array.singleton 0)
            bytesToObj bytes t

    member private x.PhysicalAddresses
        with get() = physicalAddresses
        and set anotherPhysicalAddresses =
            physicalAddresses <- anotherPhysicalAddresses

    member private x.VirtualAddresses
        with get() = virtualAddresses
        and set anotherVirtualAddresses =
            virtualAddresses <- anotherVirtualAddresses

    member private x.UnmarshalledAddresses
        with get() = unmarshalledAddresses
        and set anotherUnmarshalledAddresses =
            unmarshalledAddresses <- anotherUnmarshalledAddresses

    member x.CopyFrom (anotherConcolicMemory : ConcolicMemory) =
        x.PhysicalAddresses <- Dictionary(anotherConcolicMemory.PhysicalAddresses)
        x.VirtualAddresses <- Dictionary(anotherConcolicMemory.VirtualAddresses)
        x.UnmarshalledAddresses <- HashSet(anotherConcolicMemory.UnmarshalledAddresses)

    interface IConcreteMemory with
        // TODO: support non-vector arrays
        member x.Contains address =
            virtualAddresses.ContainsKey address && not (unmarshalledAddresses.Contains address)

        member x.GetPhysicalAddress virtAddress =
            if virtAddress = zeroHeapAddress then UIntPtr.Zero
            else virtualAddresses[virtAddress]

        member x.ReadArrayIndex address indices arrayType isSting =
            let cm = (x :> IConcreteMemory)
            let elemType, dims, isVector = arrayType
            let readElement linearIndex =
                let size = TypeUtils.internalSizeOf elemType
                let metadata = if isSting then LayoutUtils.StringElementsOffset else LayoutUtils.ArrayElementsOffset
                let offset = linearIndex * size + metadata
                let address = cm.GetPhysicalAddress address
                readHeapBytes address offset size elemType
            if isVector then
                assert(List.length indices = 1)
                readElement (List.head indices)
            else
                let lens = Array.init dims (fun dim -> cm.ReadArrayLength address dim arrayType :?> int)
                let lbs = Array.init dims (fun dim -> cm.ReadArrayLowerBound address dim arrayType :?> int)
                let linearIndex = ArrayModule.linearizeArrayIndex indices lens lbs
                readElement linearIndex

        member x.ReadArrayLength address dim arrayType =
            let _, _, isVector = arrayType
            let address = (x :> IConcreteMemory).GetPhysicalAddress address
            let offset = LayoutUtils.ArrayLengthOffset(isVector, dim)
            if isVector then readHeapBytes address offset sizeof<int> typeof<int>
            else internalfail "Length reading for non-vector array is not implemented!"

        member x.ReadArrayLowerBound _ _ arrayType =
            let _, _, isVector = arrayType
            if isVector then 0 :> obj
            else internalfail "Lower bound reading for non-vector array is not implemented!"

        member x.ReadClassField address fieldId =
            let address = (x :> IConcreteMemory).GetPhysicalAddress address
            let fieldInfo = getFieldInfo fieldId
            let t = fieldInfo.FieldType
            let offset = memoryFieldOffset fieldInfo
            let size = TypeUtils.internalSizeOf t |> int
            readHeapBytes address offset size t

        member x.ReadBoxedLocation address actualType =
            let address = (x :> IConcreteMemory).GetPhysicalAddress address
            match actualType with
            | _ when actualType.IsPrimitive || actualType.IsEnum ->
                let size = TypeUtils.internalSizeOf actualType |> int
                let metadataSize = LayoutUtils.MetadataSize typeof<Object>
                let bytes = communicator.ReadHeapBytes address metadataSize size Array.empty
                bytesToObj bytes actualType
            | _ ->
                assert(actualType.IsValueType)
                let fieldOffsets = fieldsWithOffsets actualType
                let refOffsets = chooseRefOffsets fieldOffsets
                let bytes = communicator.ReadWholeObject address refOffsets
                parseFields bytes fieldOffsets |> FieldsData |> box

        member x.GetAllArrayData address arrayType =
            let cm = x :> IConcreteMemory
            let elemType, dims, isVector = arrayType
            let elemSize = TypeUtils.internalSizeOf elemType
            let physAddress = cm.GetPhysicalAddress address
            let refOffsets = arrayRefOffsets elemType
            let bytes = communicator.ReadArray physAddress elemSize refOffsets
            if isVector then
                let array = parseVectorArray bytes elemType
                Array.mapi (fun i value -> List.singleton i, value) array
            else internalfailf "GetAllArrayData: getting array data from non-vector array (rank = %O) is not implemented!" dims

        // NOTE: 'Unmarshall' function gets all bytes from concolic memory and gives control of 'address' to SILI
        member x.Unmarshall address typ =
            let success = unmarshalledAddresses.Add address
            assert(success)
            let address = (x :> IConcreteMemory).GetPhysicalAddress address
            match typ with
            // NOTE: sending references offsets to resolve references' bytes inside concolic
            | _ when typ.IsSZArray ->
                let elemType = typ.GetElementType()
                let refOffsets = arrayRefOffsets elemType
                let elemSize = TypeUtils.internalSizeOf elemType |> int
                let bytes = communicator.UnmarshallArray address elemSize refOffsets
                parseVectorArray bytes elemType |> VectorData
            | _ when typ.IsArray ->
                let rank = typ.GetArrayRank()
                internalfailf "Unmarshalling non-vector array (rank = %O) is not implemented!" rank
            | _ when typ = typeof<String> ->
                let bytes = communicator.Unmarshall address Array.empty
                parseString bytes |> StringData
            | _ ->
                assert(not typ.IsValueType)
                let fieldOffsets = fieldsWithOffsets typ
                let refOffsets = chooseRefOffsets fieldOffsets
                let bytes = communicator.Unmarshall address refOffsets
                parseFields bytes fieldOffsets |> FieldsData

        member x.Allocate physAddress virtAddress =
            physicalAddresses.Add(physAddress, virtAddress)

        member x.DeleteAddress (physAddress : UIntPtr) =
            let virtAddress = physicalAddresses[physAddress]
            let success = physicalAddresses.Remove physAddress in assert(success)
            if virtAddress.IsValueCreated then
                let success = virtualAddresses.Remove(virtAddress.Value) in assert(success)

        member x.GetVirtualAddress physAddress =
            if physAddress = UIntPtr.Zero then zeroHeapAddress
            else
                let virtualAddress = physicalAddresses[physAddress].Value
                if virtualAddresses.ContainsKey virtualAddress |> not then
                    virtualAddresses.Add(virtualAddress, physAddress)
                virtualAddress

        member x.Copy() : IConcreteMemory =
            // TODO: need to copy concolic memory? It will be refilled via concolic
//            let concolicMemory = ConcolicMemory(communicator)
//            concolicMemory.CopyFrom(x)
//            concolicMemory
            Memory.EmptyConcreteMemory()
