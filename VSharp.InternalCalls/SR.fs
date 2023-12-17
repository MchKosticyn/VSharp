namespace VSharp.System

open global.System
open VSharp.Core

// ------------------------------ System.SR --------------------------------

module SR =

    let internal get_Arg_OverflowException (state : state) (args : term list) : term =
        assert(List.length args = 0)
        Memory.AllocateString "Arg_OverflowException" state

    let internal get_Arg_SystemException (state : state) (args : term list) : term =
        assert(List.length args = 0)
        Memory.AllocateString "Arg_SystemException" state

    let internal get_Arg_IndexOutOfRangeException (state : state) (args : term list) : term =
        assert(List.length args = 0)
        Memory.AllocateString "Arg_IndexOutOfRangeException" state

    let internal get_Arg_NullReferenceException (state : state) (args : term list) : term =
        assert(List.length args = 0)
        Memory.AllocateString "Arg_NullReferenceException" state

    let internal get_Arg_ArrayTypeMismatchException (state : state) (args : term list) : term =
        assert(List.length args = 0)
        Memory.AllocateString "Arg_ArrayTypeMismatchException" state

    let internal get_Arg_InvalidHandle (state : state) (args : term list) : term =
        assert(List.length args = 0)
        Memory.AllocateString "Arg_InvalidHandle" state

    let internal get_Arg_InvalidOperationException (state : state) (args : term list) : term =
        assert(List.length args = 0)
        Memory.AllocateString "Arg_InvalidOperationException" state

    let internal get_ArgumentOutOfRange_Index (state : state) (args : term list) : term =
        assert(List.length args = 0)
        Memory.AllocateString "ArgumentOutOfRange_Index" state

    let internal get_Arg_PlatformNotSupported (state : state) (args : term list) : term =
        assert(List.length args = 0)
        Memory.AllocateString "Arg_PlatformNotSupported" state

    let internal get_Arg_NotGenericTypeDefinition (state : state) (args : term list) : term =
        assert(List.length args = 0)
        Memory.AllocateString "Arg_NotGenericTypeDefinition" state

    let internal get_Arg_ArgumentException (state : state) (args : term list) : term =
        assert(List.length args = 0)
        Memory.AllocateString "Arg_ArgumentException" state

    let internal get_Arg_ArgumentOutOfRangeException (state : state) (args : term list) : term =
        assert(List.length args = 0)
        Memory.AllocateString "Arg_ArgumentOutOfRangeException" state

    let internal get_Arg_ArgumentNullException (state : state) (args : term list) : term =
        assert(List.length args = 0)
        Memory.AllocateString "Arg_ArgumentNullException" state

    let internal get_ArgumentNull_Generic (state : state) (args : term list) : term =
        assert(List.length args = 0)
        Memory.AllocateString "ArgumentNull_Generic" state

    let internal get_Arg_DivideByZero (state : state) (args : term list) : term =
        assert(List.length args = 0)
        Memory.AllocateString "Arg_DivideByZero" state

    let internal get_Arg_KeyNotFoundWithKey (state : state) (args : term list) : term =
        assert(List.length args = 0)
        Memory.AllocateString "Arg_KeyNotFoundWithKey" state

    let internal get_Arg_ArithmeticException (state : state) (args : term list) : term =
        assert(List.length args = 0)
        Memory.AllocateString "Arg_ArithmeticException" state

    let internal get_InvalidOperation_EmptyStack (state : state) (args : term list) : term =
        assert(List.length args = 0)
        Memory.AllocateString "InvalidOperation_EmptyStack" state

    let internal getMessageFromNativeResources (state : state) (args : term list) : term =
        assert(List.length args = 1)
        Memory.AllocateString "getMessageFromNativeResources" state

    let internal concurrencyLevelMustBePositive (state : state) (args : term list) : term =
        assert(List.length args = 0)
        Memory.AllocateString "concurrencyLevelMustBePositive" state

    let internal concurrencyLevelMustBeNegative (state : state) (args : term list) : term =
        assert(List.length args = 0)
        Memory.AllocateString "concurrencyLevelMustBeNegative" state

    let internal get_ArgumentOutOfRange_BadYearMonthDay (state : state) (args : term list) : term =
        assert(List.length args = 0)
        Memory.AllocateString "ArgumentOutOfRange_BadYearMonthDay" state

    let internal get_ArgumentOutOfRange_Count (state : state) (args : term list) : term =
        assert(List.length args = 0)
        Memory.AllocateString "ArgumentOutOfRange_Count" state

    let internal get_ArgumentOutOfRange_StartIndex (state : state) (args : term list) : term =
        assert(List.length args = 0)
        Memory.AllocateString "get_ArgumentOutOfRange_StartIndex" state

    let internal get_ArgumentOutOfRange_SmallCapacity (state : state) (args : term list) : term =
        assert(List.length args = 0)
        Memory.AllocateString "get_ArgumentOutOfRange_SmallCapacity" state

    let internal get_Argument_HasToBeArrayClass (state : state) (args : term list) : term =
        assert(List.length args = 0)
        Memory.AllocateString "get_Argument_HasToBeArrayClass" state

    let internal get_ThreadLocal_Disposed (state : state) (args : term list) : term =
        assert(List.length args = 0)
        Memory.AllocateString "get_ThreadLocal_Disposed" state

    let internal get_Arg_NotImplementedException (state : state) (args : term list) : term =
        assert(List.length args = 0)
        Memory.AllocateString "get_Arg_NotImplementedException" state

    let internal get_Argument_InvalidTypeWithPointersNotSupported (state : state) (args : term list) : term =
        assert(List.length args = 0)
        Memory.AllocateString "get_Argument_InvalidTypeWithPointersNotSupported" state

    let internal get_InvalidOperation_HandleIsNotInitialized (state : state) (args : term list) : term =
        assert(List.length args = 0)
        Memory.AllocateString "get_InvalidOperation_HandleIsNotInitialized" state

    let internal get_Argument_InvalidPathChars (state : state) (args : term list) : term =
        assert(List.length args = 0)
        Memory.AllocateString "get_Argument_InvalidPathChars" state
