// using System;
// using VSharp.Test;
// using NotImplementedException = System.NotImplementedException;
//
// namespace IntegrationTests;
//
// public class Class
// {
//     public int Field;
//
//     public int Method(int arg)
//     {
//         return arg;
//     }
// }
//
// public abstract class AbstractClassWithImplementation
// {
//     public abstract int Method(int arg);
// }
//
// public class AbstractClassImplementation: AbstractClassWithImplementation
// {
//     public override int Method(int arg)
//     {
//         return arg + 1;
//     }
// }
//
// public abstract class AbstractClassWithoutImplementation
// {
//     public abstract int Method(int arg);
// }
//
// public interface IInterfaceWithImplementation
// {
//     public int Method(int arg);
// }
//
// public class InterfaceImplementation: IInterfaceWithImplementation
// {
//     public int Method(int arg)
//     {
//         return arg + 1;
//     }
// }
//
// public interface IInterfaceWithoutImplementation
// {
//     public int Method(int arg);
// }
//
// public struct Struct
// {
//     public int IntField;
//     public string StringField;
// }
//
// public enum Enum
// {
//     A, B, C
// }
//
// [TestSvmFixture]
// public class FuzzerGenerator<A, B, C, D>
//     where A: IInterfaceWithImplementation
//     where B: IInterfaceWithoutImplementation
//     where C: AbstractClassWithImplementation
//     where D: AbstractClassWithoutImplementation
// {
//     [TestSvm(enableFuzzer: true)]
//     public void SupportEnum(Enum arg) { }
//
//     [TestSvm(enableFuzzer: true)]
//     public void SupportNumerics(
//         // SByte arg1,
//         Int16 arg2,
//         Int32 arg3,
//         Int64 arg4,
//         // Byte arg5,
//         UInt16 arg6,
//         UInt32 arg7,
//         UInt64 arg8
//         // float arg9,
//         // double arg10
//         ) { }
//
//     [TestSvm(enableFuzzer: true)]
//     public void SupportDecimal(Decimal arg) { }
//
//     [TestSvm(enableFuzzer: true)]
//     public void SupportBoolean(bool arg) { }
//
//     [TestSvm(enableFuzzer: true)]
//     public void SupportChar(char arg) { }
//
//     [TestSvm(enableFuzzer: true)]
//     public void SupportStruct(Struct arg) {}
//
//     [TestSvm(enableFuzzer: true)]
//     public void SupportString(char arg) { }
//
//     [TestSvm(enableFuzzer: true)]
//     public void SupportInterfaceWithImplementation(IInterfaceWithImplementation arg) { }
//
//     [TestSvm(enableFuzzer: true)]
//     public void SupportInterfaceWithImplementationAsGeneric(A arg) { }
//
//     [TestSvm(enableFuzzer: true)]
//     public void SupportInterfaceWithoutImplementation(IInterfaceWithoutImplementation arg) { }
//
//     [TestSvm(enableFuzzer: true)]
//     public int SupportInterfaceWithoutImplementationAsGeneric(B arg)
//     {
//         if (arg == null)
//         {
//             return 1;
//         }
//         return 0;
//     }
//
//     [TestSvm(enableFuzzer: true)]
//     public void SupportAbstractClassWithImplementation(AbstractClassWithImplementation arg) { }
//
//     [TestSvm(enableFuzzer: true)]
//     public void SupportAbstractClassWithImplementationAsGeneric(C arg) { }
//
//     [TestSvm(enableFuzzer: true)]
//     public void SupportAbstractClassWithoutImplementation(AbstractClassWithoutImplementation arg) { }
//
//     [TestSvm(enableFuzzer: true)]
//     public int SupportAbstractClassWithoutImplementationAsGeneric(D arg)
//     {
//         if (arg == null)
//         {
//             return 1;
//         }
//         return 0;
//     }
//
//     [TestSvm(enableFuzzer: true)]
//     public int SupportByRef(ref int arg1)
//     {
//         return arg1;
//     }
//
//     [TestSvm(enableFuzzer: true)]
//     public unsafe int SupportPointer(int* arg)
//     {
//         if (arg == null)
//         {
//             return 1;
//         }
//         return 0;
//     }
// }
//
// [TestSvmFixture]
// public class FuzzerGenerics
// {
//
// }
