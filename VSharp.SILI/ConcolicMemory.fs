namespace VSharp.Concolic

open System
open System.Collections.Generic
open System.Reflection
open VSharp
open VSharp.Core
open VSharp.CSharpUtils

type private FieldWithOffset =
    | Struct of FieldInfo * int * FieldWithOffset array
    | Primitive of FieldInfo * int
    | Ref of FieldInfo * int

type ConcolicMemory(communicator : Communicator) =
    let physicalAddresses = Dictionary<UIntPtr, concreteHeapAddress Lazy>()
    let virtualAddresses = Dictionary<concreteHeapAddress, UIntPtr>()
    let unmarshalledAddresses = HashSet()

    let zeroHeapAddress = VectorTime.zero

    // ------------------------- Parsing objects from concolic memory -------------------------

    let parseVectorArray bytes elemType =
        let elemSize = TypeUtils.internalSizeOf elemType |> int
        // NOTE: skipping array header
        let mutable offset = LayoutUtils.ArrayLengthOffset(true, 0)
        let length = BitConverter.ToInt64(bytes, offset) |> int
        offset <- offset + 8
        let parseOneElement i =
            let offset = offset + i * elemSize
            Reflection.bytesToObj bytes[offset .. offset + elemSize - 1] elemType
        Array.init length parseOneElement

    let parseString bytes =
        let mutable offset = LayoutUtils.StringLengthOffset
        let length = BitConverter.ToInt32(bytes, offset) |> int
        offset <- LayoutUtils.StringElementsOffset
        let elemSize = sizeof<char>
        let parseOneChar i =
            let offset = offset + i * elemSize
            let obj = Reflection.bytesToObj bytes[offset .. offset + elemSize - 1] typeof<char>
            obj :?> char
        Array.init length parseOneChar

    let rec parseFields (bytes : byte array) (fieldOffsets : FieldWithOffset array) =
        let parseOneField (field : FieldWithOffset) =
            match field with
            | Primitive(fieldInfo, offset)
            | Ref(fieldInfo, offset) ->
                let fieldType = fieldInfo.FieldType
                let fieldSize = TypeUtils.internalSizeOf fieldType |> int
                fieldInfo, Reflection.bytesToObj bytes[offset .. offset + fieldSize - 1] fieldType
            | Struct(fieldInfo, offset, fields) ->
                let fieldType = fieldInfo.FieldType
                let fieldSize = TypeUtils.internalSizeOf fieldType |> int
                let bytes = bytes[offset .. offset + fieldSize - 1]
                let data = parseFields bytes fields |> FieldsData |> box
                fieldInfo, data
        Array.map parseOneField fieldOffsets

    // ------------------------- Basic helper functions -------------------------

    let rec fieldsWithOffsets (t : Type) : FieldWithOffset array =
        assert(not t.IsPrimitive && not t.IsArray)
        let fields = Reflection.fieldsOf false t
        let getFieldOffset (_, info : FieldInfo) =
            let fieldType = info.FieldType
            match fieldType with
            | _ when fieldType.IsPrimitive || fieldType.IsEnum ->
                Primitive(info, Reflection.memoryFieldOffset info)
            | _ when TypeUtils.isStruct fieldType ->
                let fields = fieldsWithOffsets fieldType
                Struct(info, Reflection.memoryFieldOffset info, fields)
            | _ ->
                assert(not fieldType.IsValueType)
                Ref(info, Reflection.memoryFieldOffset info)
        Array.map getFieldOffset fields

    let chooseRefOffsets (fieldsWithOffsets : FieldWithOffset array) =
        let rec handleOffset (position, offsets) field =
            match field with
            | Ref(_, offset) -> position, position + offset :: offsets
            | Struct(_, offset, fields) ->
                let fieldsOffsets = Array.fold handleOffset (position + offset, offsets) fields |> snd
                position, fieldsOffsets
            | Primitive _ -> position, offsets
        Array.fold handleOffset (0, List.empty) fieldsWithOffsets |> snd |> List.toArray

    let arrayRefOffsets (elemType : Type) =
        match elemType with
        | _ when elemType.IsPrimitive || elemType.IsEnum -> Array.empty
        | _ when elemType.IsValueType -> fieldsWithOffsets elemType |> chooseRefOffsets
        | _ ->
            assert(not elemType.IsValueType)
            Array.singleton 0

    let readHeapBytes address offset size (t : Type) =
        match t with
        | _ when t.IsPrimitive || t.IsEnum ->
            let bytes = communicator.ReadHeapBytes address offset size Array.empty
            Reflection.bytesToObj bytes t
        | _ when TypeUtils.isStruct t ->
            let fieldOffsets = fieldsWithOffsets t
            let refOffsets = chooseRefOffsets fieldOffsets
            let bytes = communicator.ReadHeapBytes address offset size refOffsets
            parseFields bytes fieldOffsets |> FieldsData |> box
        | _ ->
            assert(not t.IsValueType)
            let bytes = communicator.ReadHeapBytes address offset size (Array.singleton 0)
            Reflection.bytesToObj bytes t

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
                let t = Types.ToDotNetType elemType
                let size = TypeUtils.internalSizeOf t |> int
                let metadata = if isSting then LayoutUtils.StringElementsOffset else LayoutUtils.ArrayElementsOffset
                let offset = linearIndex * size + metadata
                let address = cm.GetPhysicalAddress address
                readHeapBytes address offset size t
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
            let fieldInfo = Reflection.getFieldInfo fieldId
            let t = fieldInfo.FieldType
            let offset = Reflection.memoryFieldOffset fieldInfo
            let size = TypeUtils.internalSizeOf t |> int
            readHeapBytes address offset size t

        member x.ReadBoxedLocation address actualType =
            let address = (x :> IConcreteMemory).GetPhysicalAddress address
            match actualType with
            | _ when actualType.IsPrimitive || actualType.IsEnum ->
                let size = TypeUtils.internalSizeOf actualType |> int
                let metadataSize = LayoutUtils.MetadataSize typeof<Object>
                let bytes = communicator.ReadHeapBytes address metadataSize size Array.empty
                Reflection.bytesToObj bytes actualType
            | _ ->
                assert(actualType.IsValueType)
                let fieldOffsets = fieldsWithOffsets actualType
                let refOffsets = chooseRefOffsets fieldOffsets
                let bytes = communicator.ReadWholeObject address refOffsets
                parseFields bytes fieldOffsets |> FieldsData |> box

        member x.GetAllArrayData address arrayType =
            let cm = x :> IConcreteMemory
            let elemType, dims, isVector = arrayType
            let elemType = Types.ToDotNetType elemType
            let elemSize = TypeUtils.internalSizeOf elemType |> int
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
