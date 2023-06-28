namespace VSharp

open System
open VSharp
open System.Reflection
open System.Reflection.Emit
open Microsoft.FSharp.Collections
open VSharp.CSharpUtils
open MonoMod.RuntimeDetour

exception UnexpectedExternCallException of string

module ExtMocking =
    let storageFieldName (method : MethodInfo) = $"{method.Name}{method.MethodHandle.Value}_<Storage>"
    let counterFieldName (method : MethodInfo) = $"{method.Name}{method.MethodHandle.Value}_<Counter>"

    type PatchMethod(baseMethod : MethodInfo, clausesCount : int) =
        let returnValues : obj[] = Array.zeroCreate clausesCount
        let patchedName = $"{baseMethod.Name}_<patched>"
        let storageFieldName = storageFieldName baseMethod
        let counterFieldName = counterFieldName baseMethod
        let returnType = baseMethod.ReturnType
        let callingConvention = baseMethod.CallingConvention
        let arguments = baseMethod.GetParameters() |> Array.map (fun (p : ParameterInfo) -> p.ParameterType)

        member x.SetClauses (clauses : obj[]) =
            clauses |> Array.iteri (fun i o -> returnValues[i] <- o)

        member x.InitializeType (typ : Type) =
            if returnType <> typeof<Void> then
                let field = typ.GetField(storageFieldName, BindingFlags.NonPublic ||| BindingFlags.Static)
                if field = null then
                    internalfail $"Could not detect field {storageFieldName} of externMock!"
                let storage = Array.CreateInstance(returnType, clausesCount)
                Array.Copy(returnValues, storage, clausesCount)
                field.SetValue(null, storage)

        member private x.GenerateNonVoidIL (typeBuilder : TypeBuilder) (ilGenerator : ILGenerator) =
                let storageField = typeBuilder.DefineField(storageFieldName, returnType.MakeArrayType(), FieldAttributes.Private ||| FieldAttributes.Static)
                let counterField = typeBuilder.DefineField(counterFieldName, typeof<int>, FieldAttributes.Private ||| FieldAttributes.Static)
                let normalCase = ilGenerator.DefineLabel()
                let count = returnValues.Length

                ilGenerator.Emit(OpCodes.Ldsfld, counterField)
                ilGenerator.Emit(OpCodes.Ldc_I4, count)
                ilGenerator.Emit(OpCodes.Blt, normalCase)

                ilGenerator.Emit(OpCodes.Ldstr, patchedName)
                ilGenerator.Emit(OpCodes.Newobj, typeof<UnexpectedExternCallException>.GetConstructor([|typeof<string>|]))
                ilGenerator.Emit(OpCodes.Ret)

                ilGenerator.MarkLabel(normalCase)
                ilGenerator.Emit(OpCodes.Ldsfld, storageField)
                ilGenerator.Emit(OpCodes.Ldsfld, counterField)
                ilGenerator.Emit(OpCodes.Ldelem, returnType) // Load storage[counter] on stack

                ilGenerator.Emit(OpCodes.Ldsfld, counterField)
                ilGenerator.Emit(OpCodes.Ldc_I4_1)
                ilGenerator.Emit(OpCodes.Add)
                ilGenerator.Emit(OpCodes.Stsfld, counterField)

                ilGenerator.Emit(OpCodes.Ret)

        member x.BuildPatch (typeBuilder : TypeBuilder) =
            let methodAttributes = MethodAttributes.Public ||| MethodAttributes.HideBySig ||| MethodAttributes.Static
            let methodBuilder =
                typeBuilder.DefineMethod(patchedName, methodAttributes, callingConvention)

            methodBuilder.SetReturnType returnType
            methodBuilder.SetParameters(arguments)

            let ilGenerator = methodBuilder.GetILGenerator()

            if returnType = typeof<Void> then
                ilGenerator.Emit(OpCodes.Ret)
            else
                x.GenerateNonVoidIL typeBuilder ilGenerator

            patchedName // return identifier

    type Type(name: string) =
        let mutable patchType = null
        let mutable mockedMethod = null
        let mutable mockImplementations = null
        let mutable patchMethod = None

        static member Deserialize name (baseMethod : MethodInfo) (methodImplementations : obj[]) =
            let mockType = Type(name)
            mockType.MockedMethod <- baseMethod
            mockType.MockImplementations <- methodImplementations
            mockType.PatchMethod <- PatchMethod(baseMethod, methodImplementations.Length)
            mockType

        member x.MockedMethod
            with get() = mockedMethod
            and private set(method) =
                mockedMethod <- method

        member x.MockImplementations
            with get() = mockImplementations
            and private set(methodImplementations) =
                mockImplementations <- methodImplementations

        member x.PatchMethod
            with get() =
                match patchMethod with
                | Some pm -> pm
                | None -> internalfail "ExternMocking patch method called before initialization"
            and private set(m) =
                patchMethod <- Some m

        member x.Build(moduleBuilder : ModuleBuilder, testId) =
            let typeBuilder = moduleBuilder.DefineType($"test_{testId}_{name}", TypeAttributes.Public)
            let patchName = x.PatchMethod.BuildPatch typeBuilder
            patchType <- typeBuilder.CreateType()
            patchType, patchName

        member x.SetClauses decode =
            let decodedClauses = x.MockImplementations |> Array.map decode
            x.PatchMethod.SetClauses decodedClauses
            x.PatchMethod.InitializeType patchType

    let moduleBuilder = lazy(
        let dynamicAssemblyName = "VSharpExternMocks"
        let assemblyName = AssemblyName(dynamicAssemblyName)
        let assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run)
        assemblyBuilder.DefineDynamicModule dynamicAssemblyName)

    let detours = ResizeArray<NativeDetour>()

    let buildAndPatch (testId : string) decode (mockType : Type) =
        let methodToPatch = mockType.MockedMethod

        let isExtern = Reflection.isExternalMethod methodToPatch
        let ptrFrom =
            if isExtern then ExternMocker.GetExternPtr(methodToPatch)
            else methodToPatch.MethodHandle.GetFunctionPointer()

        let moduleBuilder = moduleBuilder.Value
        let patchType, patchName = mockType.Build(moduleBuilder, testId)
        mockType.SetClauses decode
        let methodTo = patchType.GetMethod(patchName, BindingFlags.Static ||| BindingFlags.Public)
        let ptrTo = methodTo.MethodHandle.GetFunctionPointer()

        let d = ExternMocker.BuildAndApplyDetour(ptrFrom, ptrTo)
        detours.Add d

    let unPatch () =
        for d in detours do
            d.Undo()
        detours.Clear()
