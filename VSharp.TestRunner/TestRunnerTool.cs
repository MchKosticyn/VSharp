using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using VSharp.Concolic;
using VSharp.Utils;
using static VSharp.Reflection;

namespace VSharp.TestRunner
{
    public static unsafe class TestRunner
    {
        [DllImport("libvsharpConcolic", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern void SyncInfoGettersPointers(Int64 arrayGetterPtr, Int64 objectGetterPtr, Int64 instrumentPtr);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void ArrayInfoSender(IntPtr arrayPtr, UIntPtr *objID, int *elemSize, int *refOffsetsLength,
            int **refOffsets, byte *typ, UInt64 typeLength);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void ObjectInfoSender(IntPtr arrayPtr, UIntPtr *objID, int *refOffsetsLength, int **refOffsets,
            byte *typ, UInt64 typeLength);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void InstrumentSender(uint token, uint codeSize, uint assemblyNameLength, uint moduleNameLength,
            uint maxStackSize, uint ehsSize, uint signatureTokensLength, byte* signatureTokensPtr, char* assemblyNamePtr,
            char* moduleNamePtr, byte* byteCodePtr, byte* ehsPtr,
            // result
            byte **instrumentedBody, int *length, int *resultMaxStackSize, byte **resultEhs, int *ehsLength);

        public static IntPtr ArrayInfoAction;
        public static IntPtr ObjectInfoAction;
        public static IntPtr InstrumentAction;
        public static ArrayInfoSender ArraySender;
        public static ObjectInfoSender ObjectSender;
        public static InstrumentSender Instrument;

        private static IEnumerable<string> _extraAssemblyLoadDirs;

        private static Assembly TryLoadAssemblyFrom(object sender, ResolveEventArgs args)
        {
            var existingInstance = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(assembly => assembly.FullName == args.Name);
            if (existingInstance != null)
            {
                return existingInstance;
            }
            foreach (string path in _extraAssemblyLoadDirs)
            {
                string assemblyPath = Path.Combine(path, new AssemblyName(args.Name).Name + ".dll");
                if (!File.Exists(assemblyPath))
                    return null;
                Assembly assembly = Assembly.LoadFrom(assemblyPath);
                return assembly;
            }

            return null;
        }

        private static bool StructurallyEqual(object expected, object got)
        {
            Debug.Assert(expected != null && got != null && expected.GetType() == got.GetType());
            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            var fields = expected.GetType().GetFields(flags);
            foreach (var field in fields)
            {
                if (!TypeUtils.isSubtypeOrEqual(field.FieldType, typeof(MulticastDelegate)) &&
                    !field.Name.Contains("threadid", StringComparison.OrdinalIgnoreCase) &&
                    !CompareObjects(field.GetValue(expected), field.GetValue(got)))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool ContentwiseEqual(Array expected, Array got)
        {
            Debug.Assert(expected != null && got != null && expected.GetType() == got.GetType());
            if (expected.Rank != got.Rank)
                return false;
            for (int i = 0; i < expected.Rank; ++i)
                if (expected.GetLength(i) != got.GetLength(i) || expected.GetLowerBound(i) != got.GetLowerBound(i))
                    return false;
            var enum1 = expected.GetEnumerator();
            var enum2 = got.GetEnumerator();
            while (enum1.MoveNext() && enum2.MoveNext())
            {
                if (!CompareObjects(enum1.Current, enum2.Current))
                    return false;
            }
            return true;
        }

        private static bool CompareObjects(object expected, object got)
        {
            if (expected == null)
                return got == null;
            if (got == null)
                return false;
            var type = expected.GetType();
            if (type != got.GetType())
                return false;
            if (type.IsPrimitive || expected is string || type.IsEnum)
            {
                // TODO: compare double with epsilon?
                return got.Equals(expected);
            }

            if (expected is Array array)
                return ContentwiseEqual(array, got as Array);
            return StructurallyEqual(expected, got);
        }

        private static void GetArrayInfo(IntPtr arrayPtr, UIntPtr *objID, int *elemSize, int *refOffsetsLength,
            int **refOffsets, byte *typ, UInt64 typeLength)
        {
            var type = new byte[typeLength];
            Marshal.Copy((IntPtr)typ, type, 0, (int)typeLength);
            var offset = 0;
            var realType = ConcolicUtils.parseType(type, ref offset);

            // checking if the type is an array; non-vector arrays are not implemented!
            Debug.Assert(realType.IsSZArray);

            var elemType = realType.GetElementType();
            *elemSize = TypeUtils.internalSizeOf(elemType);

            var offsets = arrayRefOffsets(realType);
            *refOffsets = (int*)Marshal.UnsafeAddrOfPinnedArrayElement(offsets, 0);

            *refOffsetsLength = offsets.Length;
        }

        private static void GetObjectInfo(IntPtr objectPtr, UIntPtr* objID, int* refOffsetsLength, int** refOffsets,
            byte* typ, UInt64 typeLength)
        {
            var type = new byte[typeLength];
            Marshal.Copy((IntPtr)typ, type, 0, (int)typeLength);
            var offset = 0;
            var realType = ConcolicUtils.parseType(type, ref offset);

            if (realType == typeof(String) || realType.IsEnum || realType.IsPrimitive)
            {
                *refOffsetsLength = 0;
                *refOffsets = (int*)Marshal.AllocHGlobal(0);

                return;
            }

            // checking if the type is a value type
            Debug.Assert(!realType.IsArray);

            var fieldOffsets = fieldsWithOffsets(realType);
            var offsets = chooseRefOffsets(fieldOffsets);
            *refOffsets = (int*)Marshal.UnsafeAddrOfPinnedArrayElement(offsets, 0);

            *refOffsetsLength = offsets.Length;
        }

        private static void Instrumenter(uint token, uint codeSize, uint assemblyNameLength, uint moduleNameLength,
            uint maxStackSize, uint ehsSize, uint signatureTokensLength, byte* signatureTokensPtr, char* assemblyNamePtr,
            char* moduleNamePtr, byte* byteCodePtr, byte* ehsPtr,
            // result
            byte **instrumentedBody, int *length, int *resultMaxStackSize, byte **resultEhs, int *ehsLength)
        {
            // Serialization
            var tokensLength = Marshal.SizeOf(typeof(signatureTokens));
            if (signatureTokensLength != tokensLength)
                throw new NotImplementedException(
                    "Size of received signature tokens buffer mismatch the expected! Probably you've altered the client-side signatures, but forgot to alter the server-side structure (or vice-versa)");
            var signatureTokensBytes = new byte[tokensLength];
            Marshal.Copy((IntPtr)signatureTokensPtr, signatureTokensBytes, 0, tokensLength);
            var tokens = Communicator.Deserialize<signatureTokens>(signatureTokensBytes);
            var assemblyNameBytes = new byte[assemblyNameLength];
            Marshal.Copy((IntPtr)assemblyNamePtr, assemblyNameBytes, 0, (int)assemblyNameLength);
            var assembly = Encoding.Unicode.GetString(assemblyNameBytes);
            var moduleNameBytes = new byte[moduleNameLength];
            Marshal.Copy((IntPtr)moduleNamePtr, moduleNameBytes, 0, (int)moduleNameLength);
            var module = Encoding.Unicode.GetString(assemblyNameBytes);
            var codeBytes = new byte[codeSize];
            Marshal.Copy((IntPtr)byteCodePtr, codeBytes, 0, (int)codeSize);
            var ehsBytes = new byte[ehsSize];
            Marshal.Copy((IntPtr)ehsPtr, ehsBytes, 0, (int)ehsSize);
            var ehSize = Marshal.SizeOf(typeof(rawExceptionHandler));
            var count = ehsSize / ehSize;
            var ehs = new rawExceptionHandler[count];
            for (var i = 0; i < count; i++)
            {
                ehs[i] = Communicator.Deserialize<rawExceptionHandler>(ehsBytes, i * ehSize);
            }

            // Instrumentation
            var properties = new rawMethodProperties(token, codeSize, assemblyNameLength, moduleNameLength,
                maxStackSize, signatureTokensLength);
            var methodBody = new rawMethodBody(properties, assembly, module, tokens, codeBytes, ehs);
            var instrumenter = new Instrumenter(null!, null!, null!);
            var instrumented = instrumenter.Instrument(methodBody);

            // Deserialization
            *instrumentedBody = (byte*)Marshal.UnsafeAddrOfPinnedArrayElement(instrumented.il, 0);
            *length = instrumented.il.Length;
            *resultMaxStackSize = (int)instrumented.properties.maxStackSize;
            var ehBytes = new byte[ehSize * instrumented.ehs.Length];
            var instrumentedEhs = instrumented.ehs;
            for (int i = 0; i < instrumentedEhs.Length; i++)
            {
                Communicator.Serialize(instrumentedEhs[i], ehBytes, i * ehSize);
            }
            *resultEhs = (byte*)Marshal.UnsafeAddrOfPinnedArrayElement(ehBytes, 0);
            *ehsLength = ehBytes.Length;
        }

        private static bool ReproduceTests(IEnumerable<FileInfo> tests, bool shouldReproduceError, bool checkResult)
        {
            ArraySender = new ArrayInfoSender(GetArrayInfo);
            ObjectSender = new ObjectInfoSender(GetObjectInfo);
            Instrument = new InstrumentSender(Instrumenter);
            ArrayInfoAction = Marshal.GetFunctionPointerForDelegate(ArraySender);
            ObjectInfoAction = Marshal.GetFunctionPointerForDelegate(ObjectSender);
            InstrumentAction = Marshal.GetFunctionPointerForDelegate(Instrument);
            SyncInfoGettersPointers(ArrayInfoAction.ToInt64(), ObjectInfoAction.ToInt64(), InstrumentAction.ToInt64());

            AppDomain.CurrentDomain.AssemblyResolve += TryLoadAssemblyFrom;

            foreach (FileInfo fi in tests)
            {
                try
                {
                    testInfo ti;
                    using (FileStream stream = new FileStream(fi.FullName, FileMode.Open, FileAccess.Read))
                    {
                        ti = UnitTest.DeserializeTestInfo(stream);
                    }

                    _extraAssemblyLoadDirs = ti.extraAssemblyLoadDirs;
                    UnitTest test = UnitTest.DeserializeFromTestInfo(ti);
                    // _extraAssemblyLoadDirs = test.ExtraAssemblyLoadDirs;

                        var method = test.Method;

                    Console.Out.WriteLine("Starting test reproducing for method {0}", method);
                    if (!checkResult)
                        Console.Out.WriteLine("Result check is disabled");
                    object[] parameters = test.Args ?? method.GetParameters()
                        .Select(t => Reflection.defaultOf(t.ParameterType)).ToArray();
                    object thisArg = test.ThisArg;
                    if (thisArg == null && !method.IsStatic)
                        thisArg = Reflection.createObject(method.DeclaringType);

                    var ex = test.Exception;
                    try
                    {
                        object result = null;
                        if (!test.IsError || shouldReproduceError)
                            result = method.Invoke(thisArg, parameters);
                        if (ex != null)
                        {
                            Console.Error.WriteLine("Test {0} failed! The expected exception {1} was not thrown",
                                fi.Name, ex);
                            return false;
                        }
                        if (checkResult && !CompareObjects(test.Expected, result))
                        {
                            // TODO: use NUnit?
                            Console.Error.WriteLine("Test {0} failed! Expected {1}, but got {2}", fi.Name,
                                test.Expected ?? "null",
                                result ?? "null");
                            return false;
                        }
                    }
                    catch (TargetInvocationException e)
                    {
                        if (e.InnerException != null && e.InnerException.GetType() == ex)
                            Console.WriteLine("Test {0} throws the expected exception!", fi.Name);
                        else if (e.InnerException != null && ex != null)
                        {
                            Console.Error.WriteLine("Test {0} throws {1} when the expected exception was {2}!", fi.Name, e.InnerException, ex);
                            throw e.InnerException;
                        }
                        else throw;
                    }
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine("Error ({0}): {1}", fi.Name, e);
                    return false;
                }

                Console.Out.WriteLine("{0} passed!", fi.Name);
            }

            return true;
        }

        public static bool ReproduceTest(FileInfo file, bool checkResult)
        {
            return ReproduceTests(new[] {file}, true, checkResult);
        }

        public static bool ReproduceTests(DirectoryInfo testsDir)
        {
            var tests = testsDir.EnumerateFiles("*.vst");
            var testsList = tests.ToList();
            if (testsList.Count > 0)
            {
                return ReproduceTests(testsList, false, true);
            }

            Console.Error.WriteLine("No *.vst tests found in {0}", testsDir.FullName);
            return false;
        }
    }
}
