module VSharp.Utils.ConcolicUtils

open System
open System.IO
open System.Text
open VSharp
open Reflection

type CorElementType =
    | ELEMENT_TYPE_END            = 0x0uy
    | ELEMENT_TYPE_VOID           = 0x1uy
    | ELEMENT_TYPE_BOOLEAN        = 0x2uy
    | ELEMENT_TYPE_CHAR           = 0x3uy
    | ELEMENT_TYPE_I1             = 0x4uy
    | ELEMENT_TYPE_U1             = 0x5uy
    | ELEMENT_TYPE_I2             = 0x6uy
    | ELEMENT_TYPE_U2             = 0x7uy
    | ELEMENT_TYPE_I4             = 0x8uy
    | ELEMENT_TYPE_U4             = 0x9uy
    | ELEMENT_TYPE_I8             = 0xauy
    | ELEMENT_TYPE_U8             = 0xbuy
    | ELEMENT_TYPE_R4             = 0xcuy
    | ELEMENT_TYPE_R8             = 0xduy
    | ELEMENT_TYPE_STRING         = 0xeuy

    | ELEMENT_TYPE_PTR            = 0xfuy
    | ELEMENT_TYPE_BYREF          = 0x10uy

    | ELEMENT_TYPE_VALUETYPE      = 0x11uy
    | ELEMENT_TYPE_CLASS          = 0x12uy
    | ELEMENT_TYPE_VAR            = 0x13uy
    | ELEMENT_TYPE_ARRAY          = 0x14uy
    | ELEMENT_TYPE_GENERICINST    = 0x15uy
    | ELEMENT_TYPE_TYPEDBYREF     = 0x16uy

    | ELEMENT_TYPE_I              = 0x18uy
    | ELEMENT_TYPE_U              = 0x19uy
    | ELEMENT_TYPE_FNPTR          = 0x1Buy
    | ELEMENT_TYPE_OBJECT         = 0x1Cuy
    | ELEMENT_TYPE_SZARRAY        = 0x1Duy
    | ELEMENT_TYPE_MVAR           = 0x1euy

    | ELEMENT_TYPE_CMOD_REQD      = 0x1Fuy
    | ELEMENT_TYPE_CMOD_OPT       = 0x20uy

    | ELEMENT_TYPE_INTERNAL       = 0x21uy
    | ELEMENT_TYPE_MAX            = 0x22uy

    | ELEMENT_TYPE_MODIFIER       = 0x40uy
    | ELEMENT_TYPE_SENTINEL       = 0x41uy
    | ELEMENT_TYPE_PINNED         = 0x45uy

let corElementTypeToType (elemType : CorElementType) =
    match elemType with
    | CorElementType.ELEMENT_TYPE_BOOLEAN -> Some(typeof<bool>)
    | CorElementType.ELEMENT_TYPE_CHAR    -> Some(typeof<char>)
    | CorElementType.ELEMENT_TYPE_I1      -> Some(typeof<int8>)
    | CorElementType.ELEMENT_TYPE_U1      -> Some(typeof<uint8>)
    | CorElementType.ELEMENT_TYPE_I2      -> Some(typeof<int16>)
    | CorElementType.ELEMENT_TYPE_U2      -> Some(typeof<uint16>)
    | CorElementType.ELEMENT_TYPE_I4      -> Some(typeof<int32>)
    | CorElementType.ELEMENT_TYPE_U4      -> Some(typeof<uint32>)
    | CorElementType.ELEMENT_TYPE_I8      -> Some(typeof<int64>)
    | CorElementType.ELEMENT_TYPE_U8      -> Some(typeof<uint64>)
    | CorElementType.ELEMENT_TYPE_R4      -> Some(typeof<float32>)
    | CorElementType.ELEMENT_TYPE_R8      -> Some(typeof<double>)
    | CorElementType.ELEMENT_TYPE_I       -> Some(typeof<IntPtr>)
    | CorElementType.ELEMENT_TYPE_U       -> Some(typeof<UIntPtr>)
    | _ -> None

let parseType(bytes : byte array, offset : int byref) =
    let mutable currentOffset = offset
    let rec readType() =
        let isValid = bytes[currentOffset] = 1uy
        currentOffset <- currentOffset + sizeof<byte>
        if isValid then
            let isArray = BitConverter.ToBoolean(bytes, currentOffset)
            currentOffset <- currentOffset + sizeof<bool>
            if isArray then
                let corElementType = Microsoft.FSharp.Core.LanguagePrimitives.EnumOfValue<byte, CorElementType>(bytes.[currentOffset])
                currentOffset <- currentOffset + sizeof<byte>
                let rank = BitConverter.ToInt32(bytes, currentOffset)
                currentOffset <- currentOffset + sizeof<int32>
                let t = Option.defaultWith readType (corElementTypeToType corElementType)
                if rank = 1 then t.MakeArrayType() else t.MakeArrayType(rank)
            else
                let token = BitConverter.ToInt32(bytes, currentOffset)
                currentOffset <- currentOffset + sizeof<int>
                let assemblySize = BitConverter.ToInt32(bytes, currentOffset)
                currentOffset <- currentOffset + sizeof<int>
                // NOTE: truncating null terminator
                let assemblyBytes = bytes.[currentOffset .. currentOffset + assemblySize - 3]
                currentOffset <- currentOffset + assemblySize
                let assemblyName = Encoding.Unicode.GetString(assemblyBytes)
                let assembly = loadAssembly assemblyName
                let moduleSize = BitConverter.ToInt32(bytes, currentOffset)
                currentOffset <- currentOffset + sizeof<int>
                let moduleBytes = bytes.[currentOffset .. currentOffset + moduleSize - 1]
                currentOffset <- currentOffset + moduleSize
                let moduleName = Encoding.Unicode.GetString(moduleBytes) |> Path.GetFileName
                let typeModule = resolveModuleFromAssembly assembly moduleName
                let typeArgsCount = BitConverter.ToInt32(bytes, currentOffset)
                currentOffset <- currentOffset + sizeof<int>
                let typeArgs = Array.init typeArgsCount (fun _ -> readType())
                let resultType = resolveTypeFromModule typeModule token
                if Array.isEmpty typeArgs then resultType else resultType.MakeGenericType(typeArgs)
        else typeof<Void>
    let parsedType = readType()
    offset <- currentOffset
    parsedType
