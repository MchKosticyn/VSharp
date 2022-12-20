namespace VSharp.Concolic

open System
open System.Collections.Generic
open VSharp
open VSharp.Core
open VSharp.CSharpUtils
open Reflection

type ReadConcreteBytes =
    | NoBytes
    | ConcreteBytes of UIntPtr * byte[]

type ConcolicMemory(communicator : Communicator) =
    let mutable physicalAddresses = Dictionary<UIntPtr, concreteHeapAddress Lazy>()
    let mutable virtualAddresses = Dictionary<concreteHeapAddress, UIntPtr>()
    let mutable unmarshalledAddresses = HashSet()
    let mutable concreteBytes = Dictionary<UIntPtr, byte[]>()

    let zeroHeapAddress = VectorTime.zero
            
    member private x.GetConcreteBytesData (address : UIntPtr) =
        if not (concreteBytes.ContainsKey address)
        then internalfailf "concrete data dictionary does not contain %O address!" address
        concreteBytes[address]

    member private x.ReadHeapBytes address offset size (t : Type) =
        let bytes = x.GetConcreteBytesData address
        match t with
        | _ when t.IsPrimitive || t.IsEnum ->
            bytesToObj bytes t
        | _ when TypeUtils.isStruct t ->
            let fieldOffsets = fieldsWithOffsets t
            parseFields bytes fieldOffsets |> FieldsData |> box
        | _ ->
            assert(not t.IsValueType)
            bytesToObj bytes t
    
    member private x.ConcreteBytes
        with get() = concreteBytes
        and set newData =
            concreteBytes <- newData
    
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
        x.ConcreteBytes <- Dictionary(anotherConcolicMemory.ConcreteBytes)
        
    member x.WriteConcreteBytes ref newBytes =
        concreteBytes.Add(ref, newBytes)

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
                x.ReadHeapBytes address offset size elemType
            if isVector then
                assert(List.length indices = 1)
                readElement (List.head indices)
            else
                let lens = Array.init dims (fun dim -> cm.ReadArrayLength address dim arrayType :?> int)
                let lbs = Array.init dims (fun dim -> cm.ReadArrayLowerBound address dim arrayType :?> int)
                let linearIndex = ArrayModule.linearizeArrayIndex indices lens lbs
                readElement linearIndex

        member x.ReadArrayLength address dim arrayType =
            let tata = arrayRefOffsets
            let _, _, isVector = arrayType
            let address = (x :> IConcreteMemory).GetPhysicalAddress address
            let offset = LayoutUtils.ArrayLengthOffset(isVector, dim)
            if isVector then x.ReadHeapBytes address offset sizeof<int> typeof<int>
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
            x.ReadHeapBytes address offset size t

        member x.ReadBoxedLocation address actualType =
            let address = (x :> IConcreteMemory).GetPhysicalAddress address
            let bytes = x.GetConcreteBytesData address
            match actualType with
            | _ when actualType.IsPrimitive || actualType.IsEnum ->
                bytesToObj bytes actualType
            | _ ->
                assert(actualType.IsValueType)
                let fieldOffsets = fieldsWithOffsets actualType
                parseFields bytes fieldOffsets |> FieldsData |> box

        member x.GetAllArrayData address arrayType =
            let elemType, dims, isVector = arrayType
            let address = (x :> IConcreteMemory).GetPhysicalAddress address
            let bytes = x.GetConcreteBytesData address
            if isVector then
                let array = parseVectorArray bytes elemType
                Array.mapi (fun i value -> List.singleton i, value) array
            else internalfailf "GetAllArrayData: getting array data from non-vector array (rank = %O) is not implemented!" dims

        // NOTE: 'Unmarshall' function gets all bytes from concolic memory and gives control of 'address' to SILI
        member x.Unmarshall address typ =
            internalfailf "Unmarshalling should be done from concolic now! unexpected call on address: %O" address

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
