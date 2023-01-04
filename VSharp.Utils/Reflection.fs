namespace VSharp

open System
open System.Collections.Generic
open System.Reflection

type concreteData =
    | StringData of char[]
    | VectorData of obj[]
    | ComplexArrayData of Array // TODO: support non-vector arrays
    | FieldsData of (FieldInfo * obj)[]

module public Reflection =

    type FieldWithOffset =
        | Struct of FieldInfo * int * FieldWithOffset array
        | Primitive of FieldInfo * int
        | Ref of FieldInfo * int

    // ----------------------------- Binding Flags ------------------------------

    let staticBindingFlags =
        let (|||) = Microsoft.FSharp.Core.Operators.(|||)
        BindingFlags.IgnoreCase ||| BindingFlags.Static ||| BindingFlags.NonPublic ||| BindingFlags.Public
    let instanceBindingFlags =
        let (|||) = Microsoft.FSharp.Core.Operators.(|||)
        BindingFlags.IgnoreCase ||| BindingFlags.Instance ||| BindingFlags.NonPublic ||| BindingFlags.Public
    let allBindingFlags =
        let (|||) = Microsoft.FSharp.Core.Operators.(|||)
        staticBindingFlags ||| instanceBindingFlags

    // ------------------------------- Assemblies -------------------------------

    let loadAssembly (assemblyName : string) =
        let assemblies = AppDomain.CurrentDomain.GetAssemblies()
        let dynamicAssemblies = assemblies |> Array.filter (fun a -> a.IsDynamic)
        let dynamicOption = dynamicAssemblies |> Array.tryFind (fun a -> a.FullName.Contains(assemblyName))
        match dynamicOption with
        | Some a -> a
        | None ->
            match assemblies |> Array.tryFindBack (fun assembly -> assembly.FullName = assemblyName) with
            | Some assembly -> assembly
            | None ->
                Assembly.Load(assemblyName)

    // --------------------------- Metadata Resolving ---------------------------

    let resolveModule (assemblyName : string) (moduleName : string) =
        let assembly =
            try
                loadAssembly assemblyName
            with _ ->
                Assembly.LoadFile(moduleName)
        if assembly.IsDynamic then assembly.Modules |> Seq.find (fun m -> m.ScopeName = moduleName)
        else assembly.Modules |> Seq.find (fun m -> m.FullyQualifiedName = moduleName)

    let resolveMethodBase (assemblyName : string) (moduleName : string) (token : int32) =
        let m = resolveModule assemblyName moduleName
        m.ResolveMethod(token)

    let private retrieveMethodsGenerics (method : MethodBase) =
        match method with
        | :? MethodInfo as mi -> mi.GetGenericArguments()
        | :? ConstructorInfo -> null
        | _ -> __notImplemented__()

    let resolveModuleFromAssembly (assembly : Assembly) (moduleName : string) =
        assembly.GetModule moduleName

    let resolveTypeFromModule (m : Module) typeToken =
        m.ResolveType(typeToken, null, null)

    let resolveField (method : MethodBase) fieldToken =
        let methodsGenerics = retrieveMethodsGenerics method
        let typGenerics = method.DeclaringType.GetGenericArguments()
        method.Module.ResolveField(fieldToken, typGenerics, methodsGenerics)

    let resolveType (method : MethodBase) typeToken =
        let typGenerics = method.DeclaringType.GetGenericArguments()
        let methodGenerics = retrieveMethodsGenerics method
        method.Module.ResolveType(typeToken, typGenerics, methodGenerics)

    let resolveMethod (method : MethodBase) methodToken =
        let typGenerics = method.DeclaringType.GetGenericArguments()
        let methodGenerics = retrieveMethodsGenerics method
        method.Module.ResolveMethod(methodToken, typGenerics, methodGenerics)

    let resolveToken (method : MethodBase) token =
        let typGenerics = method.DeclaringType.GetGenericArguments()
        let methodGenerics = retrieveMethodsGenerics method
        method.Module.ResolveMember(token, typGenerics, methodGenerics)

    // --------------------------------- Methods --------------------------------

    // TODO: what if return type is generic?
    let getMethodReturnType : MethodBase -> Type = function
        | :? ConstructorInfo -> typeof<Void>
        | :? MethodInfo as m -> m.ReturnType
        | _ -> internalfail "unknown MethodBase"

    let getMethodArgumentType index (method : MethodBase) =
        if method.IsStatic then method.GetParameters().[index].ParameterType
        elif index = 0 then method.DeclaringType
        else method.GetParameters().[index - 1].ParameterType

    let hasNonVoidResult m = (getMethodReturnType m).FullName <> typeof<Void>.FullName

    let hasThis (m : MethodBase) = m.CallingConvention.HasFlag(CallingConventions.HasThis)

    let getFullTypeName (typ : Type) = typ.ToString()

    let getFullMethodName (methodBase : MethodBase) =
        let returnType = getMethodReturnType methodBase |> getFullTypeName
        let declaringType = getFullTypeName methodBase.DeclaringType
        let parameters =
            methodBase.GetParameters()
            |> Seq.map (fun param -> getFullTypeName param.ParameterType)
            |> if methodBase.IsStatic then id else Seq.cons "this"
            |> join ", "
//        let typeParams =
//            if not methodBase.IsGenericMethod then ""
//            else methodBase.GetGenericArguments() |> Seq.map getFullTypeName |> join ", " |> sprintf "[%s]"
        sprintf "%s %s.%s(%s)" returnType declaringType methodBase.Name parameters

    let public methodToString (m : MethodBase) =
        let hasThis = hasThis m
        let returnsSomething = hasNonVoidResult m
        let argsCount = m.GetParameters().Length
        if m.DeclaringType = null then m.Name
        else sprintf "%s %s.%s(%s)" (if returnsSomething then "nonvoid" else "void") m.DeclaringType.Name m.Name (if hasThis then sprintf "%d+1" argsCount else toString argsCount)

    let isArrayConstructor (methodBase : MethodBase) =
        methodBase.IsConstructor && methodBase.DeclaringType.IsArray

    let isDelegateConstructor (methodBase : MethodBase) =
        methodBase.IsConstructor && TypeUtils.isSubtypeOrEqual methodBase.DeclaringType typedefof<Delegate>

    let isDelegate (methodBase : MethodBase) =
        TypeUtils.isSubtypeOrEqual methodBase.DeclaringType typedefof<Delegate> && methodBase.Name = "Invoke"

    let isGenericOrDeclaredInGenericType (methodBase : MethodBase) =
        methodBase.IsGenericMethod || methodBase.DeclaringType.IsGenericType

    let isStaticConstructor (m : MethodBase) =
        m.IsStatic && m.Name = ".cctor"

    let isExternalMethod (methodBase : MethodBase) =
        let isInternalCall = methodBase.GetMethodImplementationFlags() &&& MethodImplAttributes.InternalCall
        let isPInvokeImpl = methodBase.Attributes.HasFlag(MethodAttributes.PinvokeImpl)
        int isInternalCall <> 0 || isPInvokeImpl

    let getAllMethods (t : Type) = t.GetMethods(allBindingFlags)

    let getMethodDescriptor (m : MethodBase) =
        let declaringType = m.DeclaringType
        let declaringTypeVars =
            if declaringType.IsGenericType then declaringType.GetGenericArguments() |> Array.map (fun t -> t.TypeHandle.Value)
            else [||]
        let methodVars =
            if m.IsGenericMethod then m.GetGenericArguments() |> Array.map (fun t -> t.TypeHandle.Value)
            else [||]
        m.MethodHandle.Value, declaringTypeVars, methodVars, m.ReflectedType.TypeHandle.Value

    let compareMethods (m1 : MethodBase) (m2 : MethodBase) =
        compare (getMethodDescriptor m1) (getMethodDescriptor m2)

    // --------------------------------- Fields ---------------------------------

    // TODO: add cache: map from wrapped field to unwrapped

    let wrapField (field : FieldInfo) =
        {declaringType = field.DeclaringType; name = field.Name; typ = field.FieldType}

    let getFieldInfo (field : fieldId) =
        let result = field.declaringType.GetField(field.name, allBindingFlags)
        if result <> null then result
        else field.declaringType.GetRuntimeField(field.name)

    let rec private retrieveFields isStatic f (t : Type) =
        let staticFlag = if isStatic then BindingFlags.Static else BindingFlags.Instance
        let flags = BindingFlags.Public ||| BindingFlags.NonPublic ||| staticFlag
        let fields = t.GetFields(flags) |> Array.sortBy (fun field -> field.Name)
        let ourFields = f fields
        if isStatic || t.BaseType = null then ourFields
        else Array.append (retrieveFields false f t.BaseType) ourFields

    let retrieveNonStaticFields t = retrieveFields false id t

    let fieldsOf isStatic (t : Type) =
        let extractFieldInfo (field : FieldInfo) =
            if TypeUtils.isSubtypeOrEqual field.FieldType typeof<MulticastDelegate> then None
            else Some (wrapField field, field)
        retrieveFields isStatic (FSharp.Collections.Array.choose extractFieldInfo) t

    let fieldIntersects (field : fieldId) =
        let fieldInfo = getFieldInfo field
        let offset = CSharpUtils.LayoutUtils.GetFieldOffset fieldInfo
        let size = TypeUtils.internalSizeOf fieldInfo.FieldType
        let intersects o s = o + s > offset && o < offset + size
        let fields = fieldsOf false field.declaringType
        let checkIntersects (_, fieldInfo : FieldInfo) =
            let o = CSharpUtils.LayoutUtils.GetFieldOffset fieldInfo
            let s = TypeUtils.internalSizeOf fieldInfo.FieldType
            intersects o s
        let intersectingFields = Array.filter checkIntersects fields
        Array.length intersectingFields > 1

    // Returns pair (valueFieldInfo, hasValueFieldInfo)
    let fieldsOfNullable typ =
        let fs = fieldsOf false typ
        match fs with
        | [|(f1, _); (f2, _)|] when f1.name.Contains("value", StringComparison.OrdinalIgnoreCase) && f2.name.Contains("hasValue", StringComparison.OrdinalIgnoreCase) -> f1, f2
        | [|(f1, _); (f2, _)|] when f1.name.Contains("hasValue", StringComparison.OrdinalIgnoreCase) && f2.name.Contains("value", StringComparison.OrdinalIgnoreCase) -> f2, f1
        | _ -> internalfailf "%O has unexpected fields {%O}! Probably your .NET implementation is not supported :(" (getFullTypeName typ) (fs |> Array.map (fun (f, _) -> f.name) |> join ", ")

    let stringLengthField, stringFirstCharField =
        let fs = fieldsOf false typeof<string>
        match fs with
        | [|(f1, _); (f2, _)|] when f1.name.Contains("length", StringComparison.OrdinalIgnoreCase) && f2.name.Contains("firstChar", StringComparison.OrdinalIgnoreCase) -> f1, f2
        | [|(f1, _); (f2, _)|] when f1.name.Contains("firstChar", StringComparison.OrdinalIgnoreCase) && f2.name.Contains("length", StringComparison.OrdinalIgnoreCase) -> f2, f1
        | _ -> internalfailf "System.String has unexpected fields {%O}! Probably your .NET implementation is not supported :(" (fs |> Array.map (fun (f, _) -> f.name) |> join ", ")

    let emptyStringField =
        let fs = fieldsOf true typeof<string>
        match fs |> Array.tryFind (fun (f, _) -> f.name.Contains("empty", StringComparison.OrdinalIgnoreCase)) with
        | Some(f, _) -> f
        | None -> internalfailf "System.String has unexpected static fields {%O}! Probably your .NET implementation is not supported :(" (fs |> Array.map (fun (f, _) -> f.name) |> join ", ")

    // --------------------------------- Offsets ---------------------------------

    let relativeFieldOffset fieldId =
        if fieldId = stringFirstCharField then 0
        else getFieldInfo fieldId |> CSharpUtils.LayoutUtils.GetFieldOffset

    let memoryFieldOffset (fieldInfo : FieldInfo) =
        match fieldInfo with
        | _ when fieldInfo.DeclaringType <> typeof<String> ->
            let metadataSize = CSharpUtils.LayoutUtils.MetadataSize fieldInfo.DeclaringType
            metadataSize + CSharpUtils.LayoutUtils.GetFieldOffset fieldInfo
        | _ when fieldInfo.Name.Contains("length", StringComparison.OrdinalIgnoreCase) ->
            CSharpUtils.LayoutUtils.StringLengthOffset
        | _ when fieldInfo.Name.Contains("firstChar", StringComparison.OrdinalIgnoreCase) ->
            CSharpUtils.LayoutUtils.StringElementsOffset
        | _ -> __unreachable__()

    // ----------------------------------- Creating objects ----------------------------------

    let defaultOf (t : Type) =
        if t.IsValueType && not (TypeUtils.isNullable t) && not t.ContainsGenericParameters
            then Activator.CreateInstance t
            else null

    let createObject (t : Type) =
        match t with
        | _ when t = typeof<String> -> String.Empty :> obj
        | _ when TypeUtils.isNullable t -> null
        | _ when t.IsArray -> Array.CreateInstance(typeof<obj>, 1)
        | _ -> System.Runtime.Serialization.FormatterServices.GetUninitializedObject t

    let BitConverterToUIntPtr =
        if IntPtr.Size = 4 then fun (bytes : byte[]) index -> BitConverter.ToUInt32(bytes, index) |> UIntPtr
        else fun (bytes : byte[]) index -> BitConverter.ToUInt64(bytes, index) |> UIntPtr

    let rec bytesToObj (bytes : byte[]) t =
        assert(bytes.Length = (TypeUtils.internalSizeOf t |> int))
        let span = ReadOnlySpan<byte>(bytes)
        match t with
        | _ when t = typeof<byte> -> Array.head bytes :> obj
        | _ when t = typeof<sbyte> -> sbyte (Array.head bytes) :> obj
        | _ when t = typeof<int16> -> BitConverter.ToInt16 span :> obj
        | _ when t = typeof<uint16> -> BitConverter.ToUInt16 span :> obj
        | _ when t = typeof<int> -> BitConverter.ToInt32 span :> obj
        | _ when t = typeof<uint32> -> BitConverter.ToUInt32 span :> obj
        | _ when t = typeof<int64> -> BitConverter.ToInt64 span :> obj
        | _ when t = typeof<uint64> -> BitConverter.ToUInt64 span :> obj
        | _ when t = typeof<float32> -> BitConverter.ToSingle span :> obj
        | _ when t = typeof<double> -> BitConverter.ToDouble span :> obj
        | _ when t = typeof<bool> -> BitConverter.ToBoolean span :> obj
        | _ when t = typeof<char> -> BitConverter.ToChar span :> obj
        | _ when t.IsEnum ->
            let i = t.GetEnumUnderlyingType() |> bytesToObj bytes
            Enum.ToObject(t, i)
        | _ when not t.IsValueType -> BitConverterToUIntPtr bytes 0
        | _ -> internalfailf "creating object from bytes: unexpected object type %O" t

    // ------------------------- Parsing objects from bytes -----------------------------

    let rec private fieldsWithOffsetsHelper (t : Type) k =
        assert(not t.IsPrimitive && not t.IsArray)
        let fields = fieldsOf false t
        let getFieldOffset (_, info : FieldInfo) k =
            let fieldType = info.FieldType
            match fieldType with
            | _ when fieldType.IsPrimitive || fieldType.IsEnum ->
                Primitive(info, memoryFieldOffset info) |> k
            | _ when TypeUtils.isStruct fieldType ->
                fieldsWithOffsetsHelper fieldType (fun fields ->
                Struct(info, memoryFieldOffset info, fields) |> k)
            | _ ->
                assert(not fieldType.IsValueType)
                Ref(info, memoryFieldOffset info) |> k
        Cps.Seq.mapk getFieldOffset fields (Array.ofSeq >> k)

    let fieldsWithOffsets (t : Type) : FieldWithOffset array =
        fieldsWithOffsetsHelper t id

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

    let rec parseFields (bytes : byte array) (fieldOffsets : FieldWithOffset array) =
        let parseOneField (field : FieldWithOffset) =
            match field with
            | Primitive(fieldInfo, offset)
            | Ref(fieldInfo, offset) ->
                let fieldType = fieldInfo.FieldType
                let fieldSize = TypeUtils.internalSizeOf fieldType |> int
                fieldInfo, bytesToObj bytes[offset .. offset + fieldSize - 1] fieldType
            | Struct(fieldInfo, offset, fields) ->
                let fieldType = fieldInfo.FieldType
                let fieldSize = TypeUtils.internalSizeOf fieldType |> int
                let bytes = bytes[offset .. offset + fieldSize - 1]
                let data = parseFields bytes fields |> FieldsData |> box
                fieldInfo, data
        Array.map parseOneField fieldOffsets

    let parseVectorArray bytes elemType =
        let elemSize = TypeUtils.internalSizeOf elemType |> int
        // NOTE: skipping array header
        let mutable offset = VSharp.CSharpUtils.LayoutUtils.ArrayLengthOffset(true, 0)
        let length = BitConverter.ToInt64(bytes, offset) |> int
        offset <- offset + 8
        let parseOneElement i =
            let offset = offset + i * elemSize
            match elemType with
            | _ when TypeUtils.isStruct elemType -> parseFields bytes[offset .. offset + elemSize - 1] (fieldsWithOffsets elemType) |> FieldsData |> box
            | _ -> bytesToObj bytes[offset .. offset + elemSize - 1] elemType
//            bytesToObj bytes[offset .. offset + elemSize - 1] elemType
        Array.init length parseOneElement

    let parseString bytes =
        let mutable offset = VSharp.CSharpUtils.LayoutUtils.StringLengthOffset
        let length = BitConverter.ToInt32(bytes, offset) |> int
        offset <- VSharp.CSharpUtils.LayoutUtils.StringElementsOffset
        let elemSize = sizeof<char>
        let parseOneChar i =
            let offset = offset + i * elemSize
            let obj = bytesToObj bytes[offset .. offset + elemSize - 1] typeof<char>
            obj :?> char
        Array.init length parseOneChar

    // --------------------------------- Substitute generics ---------------------------------

    let private substituteMethod methodType (m : MethodBase) getMethods =
        let method = getMethods methodType |> Array.tryFind (fun (x : #MethodBase) -> x.MetadataToken = m.MetadataToken)
        match method with
        | Some x -> x
        | None -> internalfailf "unable to find method %s token" m.Name

    let private substituteMethodInfo methodType (mi : MethodInfo) groundK genericK =
        let getMethods (t : Type) = getAllMethods t
        let substituteGeneric (mi : MethodInfo) =
            let args = mi.GetGenericArguments()
            let genericMethod = mi.GetGenericMethodDefinition()
            let mi = substituteMethod methodType genericMethod getMethods
            genericK mi args
        if mi.IsGenericMethod then substituteGeneric mi
        else groundK (substituteMethod methodType mi getMethods :> MethodBase)

    let private substituteCtorInfo methodType ci k =
        let getCtor (t : Type) = t.GetConstructors(allBindingFlags)
        k (substituteMethod methodType ci getCtor :> MethodBase)

    let private substituteMethodBase<'a> methodType (m : MethodBase) (groundK : MethodBase -> 'a) genericK =
        match m with
        | _ when not <| isGenericOrDeclaredInGenericType m -> groundK m
        | :? MethodInfo as mi ->
            substituteMethodInfo methodType mi groundK genericK
        | :? ConstructorInfo as ci ->
            substituteCtorInfo methodType ci groundK
        | _ -> __unreachable__()

    // --------------------------------- Generalization ---------------------------------

    let getGenericTypeDefinition (typ : Type) =
        if typ.IsGenericType then
            let args = typ.GetGenericArguments()
            let genericType = typ.GetGenericTypeDefinition()
            let parameters = genericType.GetGenericArguments()
            genericType, args, parameters
        else typ, [||], [||]

    let generalizeMethodBase (methodBase : MethodBase) =
        let genericType, tArgs, tParams = getGenericTypeDefinition methodBase.DeclaringType
        let genericCase m args = m :> MethodBase, args, m.GetGenericArguments()
        let groundCase m = m, [||], [||]
        let genericMethod, mArgs, mParams = substituteMethodBase genericType methodBase groundCase genericCase
        let genericArgs = Array.append mArgs tArgs
        let genericDefs = Array.append mParams tParams
        genericMethod, genericArgs, genericDefs

    let fullGenericMethodName (methodBase : MethodBase) =
        let genericMethod = generalizeMethodBase methodBase |> fst3
        getFullMethodName genericMethod

    // --------------------------------- Concretization ---------------------------------

    let rec concretizeType (subst : Type -> Type) (typ : Type) =
        if typ.IsGenericParameter then subst typ
        elif typ.IsGenericType then
            let args = typ.GetGenericArguments()
            let args' = args |> Array.map (concretizeType subst)
            if args = args' then typ
            else
                typ.GetGenericTypeDefinition().MakeGenericType(args')
        else typ

    let concretizeMethodBase (m : MethodBase) (subst : Type -> Type) =
        let concreteType = concretizeType subst m.DeclaringType
        let substArgsIntoMethod (mi : MethodInfo) args =
            mi.MakeGenericMethod(args |> Array.map subst) :> MethodBase
        substituteMethodBase concreteType m id substArgsIntoMethod

    let concretizeParameter (p : ParameterInfo) (subst : Type -> Type) =
        assert(p.Member :? MethodBase)
        let method = concretizeMethodBase (p.Member :?> MethodBase) subst
        method.GetParameters() |> Array.find (fun pi -> pi.Name = p.Name)

    let concretizeField (f : fieldId) (subst : Type -> Type) =
        let declaringType = concretizeType subst f.declaringType
        {declaringType = declaringType; name = f.name; typ = concretizeType subst f.typ}

    // --------------------------------- Types ---------------------------------

    let private cachedTypes = Dictionary<Type, bool>()

    let rec private isReferenceOrContainsReferencesHelper (t : Type) =
        if t.IsValueType |> not then true
        else
            t.GetFields(allBindingFlags)
            |> Array.exists (fun field -> field.FieldType = t || isReferenceOrContainsReferencesHelper field.FieldType)

    let isReferenceOrContainsReferences (t : Type) =
        let result = ref false
        if cachedTypes.TryGetValue(t, result) then result.Value
        else
            let result = isReferenceOrContainsReferencesHelper t
            cachedTypes.Add(t, result)
            result
