using NUnit.Framework;
using VSharp.TestExtensions;
using JetBrains.Util.Util;
using JetBrains.Threading;
using JetBrains.Lifetimes;
using JetBrains.Util;
using System;
using System.Collections.Generic;
using JetBrains.Util.Internal;
using static VSharp.TestExtensions.ObjectsComparer;

namespace GeneratedNamespace
{
    [TestFixture]
    class GeneratedClass
    {
        [Test]
        public void PeekLastError()
        {
            StaticsForType<ByteBufferAsyncProcessor> obj = new Allocator<StaticsForType<ByteBufferAsyncProcessor>>{
                ["myList"] = null!
            }.Object;
            obj.PeekLast();
        }

        [Test]
        public void FromUIntTest()
        {
            object result = Cast32BitEnum<LifetimeStatus>.FromUInt(0U);
            Assert.IsTrue(CompareObjects(result, LifetimeStatus.Alive));
        }

        [Test]
        public void GetElapsedTest()
        {
            LocalStopwatch obj = new Allocator<LocalStopwatch>{
                ["myStartTimeStamp"] = 0L
            }.Object;
            object result = obj.Elapsed;
            TimeSpan obj2 = new Allocator<TimeSpan>{
                ["_ticks"] = 384667865434L
            }.Object;
            Assert.IsTrue(CompareObjects(result, obj2));
        }

        [Test]
        public void AddFirstError()
        {
            StaticsForType<ByteBufferAsyncProcessor> obj = new Allocator<StaticsForType<ByteBufferAsyncProcessor>>{
                ["myList"] = null!
            }.Object;
            obj.AddFirst(null!);
        }

        [Test]
        public void AddFirstError2()
        {
            List<ByteBufferAsyncProcessor> obj = new Allocator<List<ByteBufferAsyncProcessor>>{
                ["_items"] = null!,
                ["_size"] = 0,
                ["_version"] = 0
            }.Object;
            StaticsForType<ByteBufferAsyncProcessor> obj2 = new Allocator<StaticsForType<ByteBufferAsyncProcessor>>{
                ["myList"] = obj
            }.Object;
            obj2.AddFirst(null!);
        }

        [Test]
        public void NumberOfBitSetTest()
        {
            object result = BitHacks.NumberOfBitSet(0);
            Assert.IsTrue(CompareObjects(result, 0));
        }

        [Test]
        public void ToIntTest()
        {
            object result = Cast32BitEnum<LifetimeStatus>.ToInt(LifetimeStatus.Alive);
            Assert.IsTrue(CompareObjects(result, 0));
        }

        [Test]
        public void PeekLastError2()
        {
            List<ByteBufferAsyncProcessor> obj = new Allocator<List<ByteBufferAsyncProcessor>>{
                ["_items"] = null!,
                ["_size"] = 1,
                ["_version"] = 0
            }.Object;
            StaticsForType<ByteBufferAsyncProcessor> obj2 = new Allocator<StaticsForType<ByteBufferAsyncProcessor>>{
                ["myList"] = obj
            }.Object;
            obj2.PeekLast();
        }

        [Test]
        public void PeekFirstError()
        {
            StaticsForType<ByteBufferAsyncProcessor> obj = new Allocator<StaticsForType<ByteBufferAsyncProcessor>>{
                ["myList"] = null!
            }.Object;
            obj.PeekFirst();
        }

        [Test]
        public void GetElapsedMillisecondsTest()
        {
            LocalStopwatch obj = new Allocator<LocalStopwatch>{
                ["myStartTimeStamp"] = 0L
            }.Object;
            object result = obj.ElapsedMilliseconds;
            Assert.IsTrue(CompareObjects(result, 38466799L));
        }

        [Test]
        public void AddFirstError3()
        {
            List<ByteBufferAsyncProcessor> obj = new Allocator<List<ByteBufferAsyncProcessor>>{
                ["_items"] = new ByteBufferAsyncProcessor[]{},
                ["_size"] = -2147482624,
                ["_version"] = 0
            }.Object;
            StaticsForType<ByteBufferAsyncProcessor> obj2 = new Allocator<StaticsForType<ByteBufferAsyncProcessor>>{
                ["myList"] = obj
            }.Object;
            obj2.AddFirst(null!);
        }

        [Test]
        public void AddFirstError4()
        {
            List<ByteBufferAsyncProcessor> obj = new Allocator<List<ByteBufferAsyncProcessor>>{
                ["_items"] = new ByteBufferAsyncProcessor[]{null!, null!, null!, null!, null!, null!, null!, null!},
                ["_size"] = 1024,
                ["_version"] = 0
            }.Object;
            StaticsForType<ByteBufferAsyncProcessor> obj2 = new Allocator<StaticsForType<ByteBufferAsyncProcessor>>{
                ["myList"] = obj
            }.Object;
            obj2.AddFirst(null!);
        }

        [Test]
        public void GetElapsedTicksTest()
        {
            LocalStopwatch obj = new Allocator<LocalStopwatch>{
                ["myStartTimeStamp"] = 0L
            }.Object;
            object result = obj.ElapsedTicks;
            Assert.IsTrue(CompareObjects(result, 38466804605125L));
        }

        [Test]
        public void PeekLastError3()
        {
            List<ByteBufferAsyncProcessor> obj = new Allocator<List<ByteBufferAsyncProcessor>>{
                ["_items"] = new ByteBufferAsyncProcessor[]{},
                ["_size"] = 1073741824,
                ["_version"] = 0
            }.Object;
            StaticsForType<ByteBufferAsyncProcessor> obj2 = new Allocator<StaticsForType<ByteBufferAsyncProcessor>>{
                ["myList"] = obj
            }.Object;
            obj2.PeekLast();
        }

        [Test]
        public void ReplaceFirstError()
        {
            List<ByteBufferAsyncProcessor> obj = new Allocator<List<ByteBufferAsyncProcessor>>{
                ["_items"] = null!,
                ["_size"] = -2147483616,
                ["_version"] = 0
            }.Object;
            StaticsForType<ByteBufferAsyncProcessor> obj2 = new Allocator<StaticsForType<ByteBufferAsyncProcessor>>{
                ["myList"] = obj
            }.Object;
            obj2.ReplaceFirst(null!);
        }

        [Test]
        public void ForEachValueError()
        {
            List<ByteBufferAsyncProcessor> obj = new Allocator<List<ByteBufferAsyncProcessor>>{
                ["_items"] = null!,
                ["_size"] = 0,
                ["_version"] = 0
            }.Object;
            StaticsForType<ByteBufferAsyncProcessor> obj2 = new Allocator<StaticsForType<ByteBufferAsyncProcessor>>{
                ["myList"] = obj
            }.Object;
            obj2.ForEachValue(null!);
        }

        [Test]
        public void RentTest()
        {
            Stack<ByteBufferAsyncProcessor> obj = new Allocator<Stack<ByteBufferAsyncProcessor>>{
                ["_array"] = new ByteBufferAsyncProcessor[]{null!},
                ["_size"] = 1,
                ["_version"] = 0
            }.Object;
            SingleThreadObjectPool<ByteBufferAsyncProcessor> obj2 = new Allocator<SingleThreadObjectPool<ByteBufferAsyncProcessor>>{
                ["myMaxCapacity"] = 0,
                ["myPool"] = obj,
                ["myThread"] = null!
            }.Object;
            object result = obj2.Rent();
            Assert.IsTrue(CompareObjects(result, null!));
        }

        [Test]
        public void ReplaceFirstError2()
        {
            List<ByteBufferAsyncProcessor> obj = new Allocator<List<ByteBufferAsyncProcessor>>{
                ["_items"] = new ByteBufferAsyncProcessor[]{},
                ["_size"] = -2147483616,
                ["_version"] = 0
            }.Object;
            StaticsForType<ByteBufferAsyncProcessor> obj2 = new Allocator<StaticsForType<ByteBufferAsyncProcessor>>{
                ["myList"] = obj
            }.Object;
            obj2.ReplaceFirst(null!);
        }

        [Test]
        public void ReplaceFirstError3()
        {
            List<ByteBufferAsyncProcessor> obj = new Allocator<List<ByteBufferAsyncProcessor>>{
                ["_items"] = new ByteBufferAsyncProcessor[]{},
                ["_size"] = 1073741825,
                ["_version"] = 0
            }.Object;
            StaticsForType<ByteBufferAsyncProcessor> obj2 = new Allocator<StaticsForType<ByteBufferAsyncProcessor>>{
                ["myList"] = obj
            }.Object;
            obj2.ReplaceFirst(null!);
        }

        [Test]
        public void ToNullableTest()
        {
            SpinWaitLock obj = new Allocator<SpinWaitLock>{
                ["_lockCount"] = 0,
                ["_ownerThreadId"] = 0
            }.Object;
            object result = NullableEx.ToNullable(obj);
            SpinWaitLock obj2 = new Allocator<SpinWaitLock>{
                ["_lockCount"] = 0,
                ["_ownerThreadId"] = 0
            }.Object;
            Assert.IsTrue(CompareObjects(result, obj2));
        }

        [Test]
        public void BoolTest()
        {
            object result = BitSlice.Bool(null!);
            BoolBitSlice obj = new Allocator<BoolBitSlice>{
                ["BitCount"] = 1,
                ["LoBit"] = 0,
                ["Mask"] = 1,
                ["BitCount"] = 1,
                ["LoBit"] = 0,
                ["Mask"] = 1,
                ["BitCount"] = 1,
                ["LoBit"] = 0,
                ["Mask"] = 1
            }.Object;
            Assert.IsTrue(CompareObjects(result, obj));
        }

        [Test]
        public void AddLastError()
        {
            List<ByteBufferAsyncProcessor> obj = new Allocator<List<ByteBufferAsyncProcessor>>{
                ["_items"] = new ByteBufferAsyncProcessor[]{},
                ["_size"] = -2147483645,
                ["_version"] = 0
            }.Object;
            StaticsForType<ByteBufferAsyncProcessor> obj2 = new Allocator<StaticsForType<ByteBufferAsyncProcessor>>{
                ["myList"] = obj
            }.Object;
            obj2.AddLast(null!);
        }

        [Test]
        public void AddLastError2()
        {
            List<ByteBufferAsyncProcessor> obj = new Allocator<List<ByteBufferAsyncProcessor>>{
                ["_items"] = new ByteBufferAsyncProcessor[]{},
                ["_size"] = 3,
                ["_version"] = 0
            }.Object;
            StaticsForType<ByteBufferAsyncProcessor> obj2 = new Allocator<StaticsForType<ByteBufferAsyncProcessor>>{
                ["myList"] = obj
            }.Object;
            obj2.AddLast(null!);
        }

        [Test]
        public void EnumTest()
        {
            BitSlice obj = new Allocator<BitSlice>{
                ["BitCount"] = 0,
                ["LoBit"] = 0,
                ["Mask"] = 0
            }.Object;
            object result = BitSlice.Enum(obj);
            Enum32BitSlice<LifetimeStatus> obj2 = new Allocator<Enum32BitSlice<LifetimeStatus>>{
                ["BitCount"] = 2,
                ["LoBit"] = 0,
                ["Mask"] = 3,
                ["BitCount"] = 2,
                ["LoBit"] = 0,
                ["Mask"] = 3,
                ["BitCount"] = 2,
                ["LoBit"] = 0,
                ["Mask"] = 3
            }.Object;
            Assert.IsTrue(CompareObjects(result, obj2));
        }

        [Test]
        public void ReplaceFirstError4()
        {
            List<ByteBufferAsyncProcessor> obj = new Allocator<List<ByteBufferAsyncProcessor>>{
                ["_items"] = null!,
                ["_size"] = 32,
                ["_version"] = 0
            }.Object;
            StaticsForType<ByteBufferAsyncProcessor> obj2 = new Allocator<StaticsForType<ByteBufferAsyncProcessor>>{
                ["myList"] = obj
            }.Object;
            obj2.ReplaceFirst(null!);
        }

        [Test]
        public void RemoveLastReferenceEqualTest()
        {
            List<ByteBufferAsyncProcessor> obj = new Allocator<List<ByteBufferAsyncProcessor>>{
                ["_items"] = new ByteBufferAsyncProcessor[]{null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!},
                ["_size"] = 80,
                ["_version"] = 0
            }.Object;
            StaticsForType<ByteBufferAsyncProcessor> obj2 = new Allocator<StaticsForType<ByteBufferAsyncProcessor>>{
                ["myList"] = obj
            }.Object;
            object result = obj2.RemoveLastReferenceEqual(null!, false);
            Assert.IsTrue(CompareObjects(result, true));
        }

        [Test]
        public void RemoveLastReferenceEqualTest2()
        {
            List<ByteBufferAsyncProcessor> obj = new Allocator<List<ByteBufferAsyncProcessor>>{
                ["_items"] = new ByteBufferAsyncProcessor[]{null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!},
                ["_size"] = 82,
                ["_version"] = 0
            }.Object;
            StaticsForType<ByteBufferAsyncProcessor> obj2 = new Allocator<StaticsForType<ByteBufferAsyncProcessor>>{
                ["myList"] = obj
            }.Object;
            ByteBufferAsyncProcessor obj3 = new Allocator<ByteBufferAsyncProcessor>{
                ["AcknowledgedSeqN"] = 0L,
                ["Id"] = null!,
                ["State"] = ByteBufferAsyncProcessor.StateKind.Initialized,
                ["ChunkSize"] = 0,
                ["ShrinkIntervalMs"] = 0,
                ["myAllDataProcessed"] = false,
                ["myAsyncProcessingThread"] = null!,
                ["myChunkToFill"] = null!,
                ["myChunkToProcess"] = null!,
                ["myLastShrinkOrGrowTimeMs"] = 0,
                ["myLock"] = null!,
                ["myLog"] = null!,
                ["myPauseReasons"] = null!,
                ["myProcessing"] = false
            }.Object;
            object result = obj2.RemoveLastReferenceEqual(obj3, false);
            Assert.IsTrue(CompareObjects(result, true));
        }

        [Test]
        public void Log2FloorTest()
        {
            object result = BitHacks.Log2Floor(0);
            Assert.IsTrue(CompareObjects(result, 0));
        }

        [Test]
        public void Log2FloorTest2()
        {
            object result = BitHacks.Log2Floor(256);
            Assert.IsTrue(CompareObjects(result, 8));
        }

        [Test]
        public void RemoveLastReferenceEqualTest3()
        {
            List<ByteBufferAsyncProcessor> obj = new Allocator<List<ByteBufferAsyncProcessor>>{
                ["_items"] = new ByteBufferAsyncProcessor[]{null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!},
                ["_size"] = 81,
                ["_version"] = 0
            }.Object;
            StaticsForType<ByteBufferAsyncProcessor> obj2 = new Allocator<StaticsForType<ByteBufferAsyncProcessor>>{
                ["myList"] = obj
            }.Object;
            object result = obj2.RemoveLastReferenceEqual(null!, false);
            Assert.IsTrue(CompareObjects(result, true));
        }

        [Test]
        public void RemoveLastReferenceEqualTest4()
        {
            List<ByteBufferAsyncProcessor> obj = new Allocator<List<ByteBufferAsyncProcessor>>{
                ["_items"] = new ByteBufferAsyncProcessor[]{null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!},
                ["_size"] = 80,
                ["_version"] = 0
            }.Object;
            StaticsForType<ByteBufferAsyncProcessor> obj2 = new Allocator<StaticsForType<ByteBufferAsyncProcessor>>{
                ["myList"] = obj
            }.Object;
            ByteBufferAsyncProcessor obj3 = new Allocator<ByteBufferAsyncProcessor>{
                ["AcknowledgedSeqN"] = 0L,
                ["Id"] = null!,
                ["State"] = ByteBufferAsyncProcessor.StateKind.Initialized,
                ["ChunkSize"] = 0,
                ["ShrinkIntervalMs"] = 0,
                ["myAllDataProcessed"] = false,
                ["myAsyncProcessingThread"] = null!,
                ["myChunkToFill"] = null!,
                ["myChunkToProcess"] = null!,
                ["myLastShrinkOrGrowTimeMs"] = 0,
                ["myLock"] = null!,
                ["myLog"] = null!,
                ["myPauseReasons"] = null!,
                ["myProcessing"] = false
            }.Object;
            object result = obj2.RemoveLastReferenceEqual(obj3, false);
            Assert.IsTrue(CompareObjects(result, true));
        }

        [Test]
        public void RemoveLastReferenceEqualTest5()
        {
            List<ByteBufferAsyncProcessor> obj = new Allocator<List<ByteBufferAsyncProcessor>>{
                ["_items"] = new ByteBufferAsyncProcessor[]{null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!},
                ["_size"] = 89,
                ["_version"] = 0
            }.Object;
            StaticsForType<ByteBufferAsyncProcessor> obj2 = new Allocator<StaticsForType<ByteBufferAsyncProcessor>>{
                ["myList"] = obj
            }.Object;
            ByteBufferAsyncProcessor obj3 = new Allocator<ByteBufferAsyncProcessor>{
                ["AcknowledgedSeqN"] = 0L,
                ["Id"] = null!,
                ["State"] = ByteBufferAsyncProcessor.StateKind.Initialized,
                ["ChunkSize"] = 0,
                ["ShrinkIntervalMs"] = 0,
                ["myAllDataProcessed"] = false,
                ["myAsyncProcessingThread"] = null!,
                ["myChunkToFill"] = null!,
                ["myChunkToProcess"] = null!,
                ["myLastShrinkOrGrowTimeMs"] = 0,
                ["myLock"] = null!,
                ["myLog"] = null!,
                ["myPauseReasons"] = null!,
                ["myProcessing"] = false
            }.Object;
            object result = obj2.RemoveLastReferenceEqual(obj3, false);
            Assert.IsTrue(CompareObjects(result, true));
        }

        [Test]
        public void RemoveLastReferenceEqualTest6()
        {
            List<ByteBufferAsyncProcessor> obj = new Allocator<List<ByteBufferAsyncProcessor>>{
                ["_items"] = new ByteBufferAsyncProcessor[]{null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!},
                ["_size"] = 83,
                ["_version"] = 0
            }.Object;
            StaticsForType<ByteBufferAsyncProcessor> obj2 = new Allocator<StaticsForType<ByteBufferAsyncProcessor>>{
                ["myList"] = obj
            }.Object;
            object result = obj2.RemoveLastReferenceEqual(null!, false);
            Assert.IsTrue(CompareObjects(result, true));
        }

        [Test]
        public void RemoveLastReferenceEqualTest7()
        {
            List<ByteBufferAsyncProcessor> obj = new Allocator<List<ByteBufferAsyncProcessor>>{
                ["_items"] = new ByteBufferAsyncProcessor[]{null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!},
                ["_size"] = 80,
                ["_version"] = 0
            }.Object;
            StaticsForType<ByteBufferAsyncProcessor> obj2 = new Allocator<StaticsForType<ByteBufferAsyncProcessor>>{
                ["myList"] = obj
            }.Object;
            ByteBufferAsyncProcessor obj3 = new Allocator<ByteBufferAsyncProcessor>{
                ["AcknowledgedSeqN"] = 0L,
                ["Id"] = null!,
                ["State"] = ByteBufferAsyncProcessor.StateKind.Initialized,
                ["ChunkSize"] = 0,
                ["ShrinkIntervalMs"] = 0,
                ["myAllDataProcessed"] = false,
                ["myAsyncProcessingThread"] = null!,
                ["myChunkToFill"] = null!,
                ["myChunkToProcess"] = null!,
                ["myLastShrinkOrGrowTimeMs"] = 0,
                ["myLock"] = null!,
                ["myLog"] = null!,
                ["myPauseReasons"] = null!,
                ["myProcessing"] = false
            }.Object;
            object result = obj2.RemoveLastReferenceEqual(obj3, false);
            Assert.IsTrue(CompareObjects(result, true));
        }

        [Test]
        public void GetInstanceTest()
        {
            object result = EmptyArray.GetInstance();
            Assert.IsTrue(CompareObjects(result, new ByteBufferAsyncProcessor[]{}));
        }

        [Test]
        public void RemoveLastReferenceEqualTest8()
        {
            List<ByteBufferAsyncProcessor> obj = new Allocator<List<ByteBufferAsyncProcessor>>{
                ["_items"] = new ByteBufferAsyncProcessor[]{null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!},
                ["_size"] = 80,
                ["_version"] = 0
            }.Object;
            StaticsForType<ByteBufferAsyncProcessor> obj2 = new Allocator<StaticsForType<ByteBufferAsyncProcessor>>{
                ["myList"] = obj
            }.Object;
            ByteBufferAsyncProcessor obj3 = new Allocator<ByteBufferAsyncProcessor>{
                ["AcknowledgedSeqN"] = 0L,
                ["Id"] = null!,
                ["State"] = ByteBufferAsyncProcessor.StateKind.Initialized,
                ["ChunkSize"] = 0,
                ["ShrinkIntervalMs"] = 0,
                ["myAllDataProcessed"] = false,
                ["myAsyncProcessingThread"] = null!,
                ["myChunkToFill"] = null!,
                ["myChunkToProcess"] = null!,
                ["myLastShrinkOrGrowTimeMs"] = 0,
                ["myLock"] = null!,
                ["myLog"] = null!,
                ["myPauseReasons"] = null!,
                ["myProcessing"] = false
            }.Object;
            object result = obj2.RemoveLastReferenceEqual(obj3, false);
            Assert.IsTrue(CompareObjects(result, true));
        }

        [Test]
        public void RemoveLastReferenceEqualTest9()
        {
            List<ByteBufferAsyncProcessor> obj = new Allocator<List<ByteBufferAsyncProcessor>>{
                ["_items"] = new ByteBufferAsyncProcessor[]{null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!},
                ["_size"] = 84,
                ["_version"] = 0
            }.Object;
            StaticsForType<ByteBufferAsyncProcessor> obj2 = new Allocator<StaticsForType<ByteBufferAsyncProcessor>>{
                ["myList"] = obj
            }.Object;
            ByteBufferAsyncProcessor obj3 = new Allocator<ByteBufferAsyncProcessor>{
                ["AcknowledgedSeqN"] = 0L,
                ["Id"] = null!,
                ["State"] = ByteBufferAsyncProcessor.StateKind.Initialized,
                ["ChunkSize"] = 0,
                ["ShrinkIntervalMs"] = 0,
                ["myAllDataProcessed"] = false,
                ["myAsyncProcessingThread"] = null!,
                ["myChunkToFill"] = null!,
                ["myChunkToProcess"] = null!,
                ["myLastShrinkOrGrowTimeMs"] = 0,
                ["myLock"] = null!,
                ["myLog"] = null!,
                ["myPauseReasons"] = null!,
                ["myProcessing"] = false
            }.Object;
            object result = obj2.RemoveLastReferenceEqual(obj3, false);
            Assert.IsTrue(CompareObjects(result, true));
        }

        [Test]
        public void RemoveLastReferenceEqualTest10()
        {
            List<ByteBufferAsyncProcessor> obj = new Allocator<List<ByteBufferAsyncProcessor>>{
                ["_items"] = new ByteBufferAsyncProcessor[]{null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!},
                ["_size"] = 90,
                ["_version"] = 0
            }.Object;
            StaticsForType<ByteBufferAsyncProcessor> obj2 = new Allocator<StaticsForType<ByteBufferAsyncProcessor>>{
                ["myList"] = obj
            }.Object;
            ByteBufferAsyncProcessor obj3 = new Allocator<ByteBufferAsyncProcessor>{
                ["AcknowledgedSeqN"] = 0L,
                ["Id"] = null!,
                ["State"] = ByteBufferAsyncProcessor.StateKind.Initialized,
                ["ChunkSize"] = 0,
                ["ShrinkIntervalMs"] = 0,
                ["myAllDataProcessed"] = false,
                ["myAsyncProcessingThread"] = null!,
                ["myChunkToFill"] = null!,
                ["myChunkToProcess"] = null!,
                ["myLastShrinkOrGrowTimeMs"] = 0,
                ["myLock"] = null!,
                ["myLog"] = null!,
                ["myPauseReasons"] = null!,
                ["myProcessing"] = false
            }.Object;
            object result = obj2.RemoveLastReferenceEqual(obj3, false);
            Assert.IsTrue(CompareObjects(result, true));
        }

        [Test]
        public void RemoveLastReferenceEqualTest11()
        {
            List<ByteBufferAsyncProcessor> obj = new Allocator<List<ByteBufferAsyncProcessor>>{
                ["_items"] = new ByteBufferAsyncProcessor[]{null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!},
                ["_size"] = 88,
                ["_version"] = 0
            }.Object;
            StaticsForType<ByteBufferAsyncProcessor> obj2 = new Allocator<StaticsForType<ByteBufferAsyncProcessor>>{
                ["myList"] = obj
            }.Object;
            ByteBufferAsyncProcessor obj3 = new Allocator<ByteBufferAsyncProcessor>{
                ["AcknowledgedSeqN"] = 0L,
                ["Id"] = null!,
                ["State"] = ByteBufferAsyncProcessor.StateKind.Initialized,
                ["ChunkSize"] = 0,
                ["ShrinkIntervalMs"] = 0,
                ["myAllDataProcessed"] = false,
                ["myAsyncProcessingThread"] = null!,
                ["myChunkToFill"] = null!,
                ["myChunkToProcess"] = null!,
                ["myLastShrinkOrGrowTimeMs"] = 0,
                ["myLock"] = null!,
                ["myLog"] = null!,
                ["myPauseReasons"] = null!,
                ["myProcessing"] = false
            }.Object;
            object result = obj2.RemoveLastReferenceEqual(obj3, false);
            Assert.IsTrue(CompareObjects(result, true));
        }

        [Test]
        public void RemoveLastReferenceEqualTest12()
        {
            List<ByteBufferAsyncProcessor> obj = new Allocator<List<ByteBufferAsyncProcessor>>{
                ["_items"] = new ByteBufferAsyncProcessor[]{null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!},
                ["_size"] = 80,
                ["_version"] = 0
            }.Object;
            StaticsForType<ByteBufferAsyncProcessor> obj2 = new Allocator<StaticsForType<ByteBufferAsyncProcessor>>{
                ["myList"] = obj
            }.Object;
            ByteBufferAsyncProcessor obj3 = new Allocator<ByteBufferAsyncProcessor>{
                ["AcknowledgedSeqN"] = 0L,
                ["Id"] = null!,
                ["State"] = ByteBufferAsyncProcessor.StateKind.Initialized,
                ["ChunkSize"] = 0,
                ["ShrinkIntervalMs"] = 0,
                ["myAllDataProcessed"] = false,
                ["myAsyncProcessingThread"] = null!,
                ["myChunkToFill"] = null!,
                ["myChunkToProcess"] = null!,
                ["myLastShrinkOrGrowTimeMs"] = 0,
                ["myLock"] = null!,
                ["myLog"] = null!,
                ["myPauseReasons"] = null!,
                ["myProcessing"] = false
            }.Object;
            object result = obj2.RemoveLastReferenceEqual(obj3, false);
            Assert.IsTrue(CompareObjects(result, true));
        }

        [Test]
        public void PeekLastTest()
        {
            List<ByteBufferAsyncProcessor> obj = new Allocator<List<ByteBufferAsyncProcessor>>{
                ["_items"] = new ByteBufferAsyncProcessor[]{null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!},
                ["_size"] = 32,
                ["_version"] = 0
            }.Object;
            StaticsForType<ByteBufferAsyncProcessor> obj2 = new Allocator<StaticsForType<ByteBufferAsyncProcessor>>{
                ["myList"] = obj
            }.Object;
            object result = obj2.PeekLast();
            Assert.IsTrue(CompareObjects(result, null!));
        }

        [Test]
        public void Log2FloorTest3()
        {
            object result = BitHacks.Log2Floor(65536);
            Assert.IsTrue(CompareObjects(result, 16));
        }

        [Test]
        public void Log2FloorTest4()
        {
            object result = BitHacks.Log2Floor(16777216);
            Assert.IsTrue(CompareObjects(result, 24));
        }

        [Test]
        public void PeekLastTest2()
        {
            List<ByteBufferAsyncProcessor> obj = new Allocator<List<ByteBufferAsyncProcessor>>{
                ["_items"] = null!,
                ["_size"] = 0,
                ["_version"] = 0
            }.Object;
            StaticsForType<ByteBufferAsyncProcessor> obj2 = new Allocator<StaticsForType<ByteBufferAsyncProcessor>>{
                ["myList"] = obj
            }.Object;
            object result = obj2.PeekLast();
            Assert.IsTrue(CompareObjects(result, null!));
        }

        [Test]
        public void RemoveLastReferenceEqualTest13()
        {
            List<ByteBufferAsyncProcessor> obj = new Allocator<List<ByteBufferAsyncProcessor>>{
                ["_items"] = new ByteBufferAsyncProcessor[]{null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!},
                ["_size"] = 87,
                ["_version"] = 0
            }.Object;
            StaticsForType<ByteBufferAsyncProcessor> obj2 = new Allocator<StaticsForType<ByteBufferAsyncProcessor>>{
                ["myList"] = obj
            }.Object;
            ByteBufferAsyncProcessor obj3 = new Allocator<ByteBufferAsyncProcessor>{
                ["AcknowledgedSeqN"] = 0L,
                ["Id"] = null!,
                ["State"] = ByteBufferAsyncProcessor.StateKind.Initialized,
                ["ChunkSize"] = 0,
                ["ShrinkIntervalMs"] = 0,
                ["myAllDataProcessed"] = false,
                ["myAsyncProcessingThread"] = null!,
                ["myChunkToFill"] = null!,
                ["myChunkToProcess"] = null!,
                ["myLastShrinkOrGrowTimeMs"] = 0,
                ["myLock"] = null!,
                ["myLog"] = null!,
                ["myPauseReasons"] = null!,
                ["myProcessing"] = false
            }.Object;
            object result = obj2.RemoveLastReferenceEqual(obj3, false);
            Assert.IsTrue(CompareObjects(result, true));
        }

        [Test]
        public void RemoveLastReferenceEqualTest14()
        {
            List<ByteBufferAsyncProcessor> obj = new Allocator<List<ByteBufferAsyncProcessor>>{
                ["_items"] = new ByteBufferAsyncProcessor[]{null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!},
                ["_size"] = 85,
                ["_version"] = 0
            }.Object;
            StaticsForType<ByteBufferAsyncProcessor> obj2 = new Allocator<StaticsForType<ByteBufferAsyncProcessor>>{
                ["myList"] = obj
            }.Object;
            ByteBufferAsyncProcessor obj3 = new Allocator<ByteBufferAsyncProcessor>{
                ["AcknowledgedSeqN"] = 0L,
                ["Id"] = null!,
                ["State"] = ByteBufferAsyncProcessor.StateKind.Initialized,
                ["ChunkSize"] = 0,
                ["ShrinkIntervalMs"] = 0,
                ["myAllDataProcessed"] = false,
                ["myAsyncProcessingThread"] = null!,
                ["myChunkToFill"] = null!,
                ["myChunkToProcess"] = null!,
                ["myLastShrinkOrGrowTimeMs"] = 0,
                ["myLock"] = null!,
                ["myLog"] = null!,
                ["myPauseReasons"] = null!,
                ["myProcessing"] = false
            }.Object;
            object result = obj2.RemoveLastReferenceEqual(obj3, false);
            Assert.IsTrue(CompareObjects(result, true));
        }

        [Test]
        public void Log2CeilTest()
        {
            object result = BitHacks.Log2Ceil(2147483647L);
            Assert.IsTrue(CompareObjects(result, 63));
        }

        [Test]
        public void Log2FloorTest5()
        {
            Assert.Throws<ArgumentException>(() => BitHacks.Log2Floor(-2147483648));
        }

        [Test]
        public void RemoveLastReferenceEqualTest15()
        {
            List<ByteBufferAsyncProcessor> obj = new Allocator<List<ByteBufferAsyncProcessor>>{
                ["_items"] = new ByteBufferAsyncProcessor[]{null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!},
                ["_size"] = 86,
                ["_version"] = 0
            }.Object;
            StaticsForType<ByteBufferAsyncProcessor> obj2 = new Allocator<StaticsForType<ByteBufferAsyncProcessor>>{
                ["myList"] = obj
            }.Object;
            ByteBufferAsyncProcessor obj3 = new Allocator<ByteBufferAsyncProcessor>{
                ["AcknowledgedSeqN"] = 0L,
                ["Id"] = null!,
                ["State"] = ByteBufferAsyncProcessor.StateKind.Initialized,
                ["ChunkSize"] = 0,
                ["ShrinkIntervalMs"] = 0,
                ["myAllDataProcessed"] = false,
                ["myAsyncProcessingThread"] = null!,
                ["myChunkToFill"] = null!,
                ["myChunkToProcess"] = null!,
                ["myLastShrinkOrGrowTimeMs"] = 0,
                ["myLock"] = null!,
                ["myLog"] = null!,
                ["myPauseReasons"] = null!,
                ["myProcessing"] = false
            }.Object;
            object result = obj2.RemoveLastReferenceEqual(obj3, false);
            Assert.IsTrue(CompareObjects(result, true));
        }

        [Test]
        public void RemoveLastReferenceEqualTest16()
        {
            List<ByteBufferAsyncProcessor> obj = new Allocator<List<ByteBufferAsyncProcessor>>{
                ["_items"] = new ByteBufferAsyncProcessor[]{null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!},
                ["_size"] = 128,
                ["_version"] = 0
            }.Object;
            StaticsForType<ByteBufferAsyncProcessor> obj2 = new Allocator<StaticsForType<ByteBufferAsyncProcessor>>{
                ["myList"] = obj
            }.Object;
            ByteBufferAsyncProcessor obj3 = new Allocator<ByteBufferAsyncProcessor>{
                ["AcknowledgedSeqN"] = 0L,
                ["Id"] = null!,
                ["State"] = ByteBufferAsyncProcessor.StateKind.Initialized,
                ["ChunkSize"] = 0,
                ["ShrinkIntervalMs"] = 0,
                ["myAllDataProcessed"] = false,
                ["myAsyncProcessingThread"] = null!,
                ["myChunkToFill"] = null!,
                ["myChunkToProcess"] = null!,
                ["myLastShrinkOrGrowTimeMs"] = 0,
                ["myLock"] = null!,
                ["myLog"] = null!,
                ["myPauseReasons"] = null!,
                ["myProcessing"] = false
            }.Object;
            object result = obj2.RemoveLastReferenceEqual(obj3, false);
            Assert.IsTrue(CompareObjects(result, false));
        }

        [Test]
        public void PeekFirstTest()
        {
            List<ByteBufferAsyncProcessor> obj = new Allocator<List<ByteBufferAsyncProcessor>>{
                ["_items"] = null!,
                ["_size"] = 0,
                ["_version"] = 0
            }.Object;
            StaticsForType<ByteBufferAsyncProcessor> obj2 = new Allocator<StaticsForType<ByteBufferAsyncProcessor>>{
                ["myList"] = obj
            }.Object;
            object result = obj2.PeekFirst();
            Assert.IsTrue(CompareObjects(result, null!));
        }

        [Test]
        public void PeekFirstTest2()
        {
            List<ByteBufferAsyncProcessor> obj = new Allocator<List<ByteBufferAsyncProcessor>>{
                ["_items"] = new ByteBufferAsyncProcessor[]{null!},
                ["_size"] = 1,
                ["_version"] = 0
            }.Object;
            StaticsForType<ByteBufferAsyncProcessor> obj2 = new Allocator<StaticsForType<ByteBufferAsyncProcessor>>{
                ["myList"] = obj
            }.Object;
            object result = obj2.PeekFirst();
            Assert.IsTrue(CompareObjects(result, null!));
        }

        [Test]
        public void ReplaceFirstTest()
        {
            List<ByteBufferAsyncProcessor> obj = new Allocator<List<ByteBufferAsyncProcessor>>{
                ["_items"] = null!,
                ["_size"] = 0,
                ["_version"] = 0
            }.Object;
            StaticsForType<ByteBufferAsyncProcessor> obj2 = new Allocator<StaticsForType<ByteBufferAsyncProcessor>>{
                ["myList"] = obj
            }.Object;
            Assert.Throws<ArgumentOutOfRangeException>(() => obj2.ReplaceFirst(null!));
        }

        [Test]
        public void AddFirstTest()
        {
            List<ByteBufferAsyncProcessor> obj = new Allocator<List<ByteBufferAsyncProcessor>>{
                ["_items"] = new ByteBufferAsyncProcessor[]{},
                ["_size"] = 0,
                ["_version"] = 0
            }.Object;
            StaticsForType<ByteBufferAsyncProcessor> obj2 = new Allocator<StaticsForType<ByteBufferAsyncProcessor>>{
                ["myList"] = obj
            }.Object;
            obj2.AddFirst(null!);
        }

        [Test]
        public void ReplaceFirstTest2()
        {
            List<ByteBufferAsyncProcessor> obj = new Allocator<List<ByteBufferAsyncProcessor>>{
                ["_items"] = new ByteBufferAsyncProcessor[]{null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!},
                ["_size"] = 1,
                ["_version"] = 0
            }.Object;
            StaticsForType<ByteBufferAsyncProcessor> obj2 = new Allocator<StaticsForType<ByteBufferAsyncProcessor>>{
                ["myList"] = obj
            }.Object;
            obj2.ReplaceFirst(null!);
        }

        [Test]
        public void Log2CeilTest2()
        {
            object result = BitHacks.Log2Ceil(1073741823L);
            Assert.IsTrue(CompareObjects(result, 30));
        }

        [Test]
        public void Log2CeilTest3()
        {
            Assert.Throws<ArgumentException>(() => BitHacks.Log2Ceil(-9223372036854775808L));
        }

        [Test]
        public void ReplaceFirstTest3()
        {
            List<ByteBufferAsyncProcessor> obj = new Allocator<List<ByteBufferAsyncProcessor>>{
                ["_items"] = new ByteBufferAsyncProcessor[]{null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!},
                ["_size"] = 32,
                ["_version"] = 0
            }.Object;
            StaticsForType<ByteBufferAsyncProcessor> obj2 = new Allocator<StaticsForType<ByteBufferAsyncProcessor>>{
                ["myList"] = obj
            }.Object;
            obj2.ReplaceFirst(null!);
        }

        [Test]
        public void AddFirstTest2()
        {
            List<ByteBufferAsyncProcessor> obj = new Allocator<List<ByteBufferAsyncProcessor>>{
                ["_items"] = new ByteBufferAsyncProcessor[]{null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!},
                ["_size"] = 32,
                ["_version"] = 0
            }.Object;
            StaticsForType<ByteBufferAsyncProcessor> obj2 = new Allocator<StaticsForType<ByteBufferAsyncProcessor>>{
                ["myList"] = obj
            }.Object;
            obj2.AddFirst(null!);
        }

        [Test]
        public void AddLastTest()
        {
            List<ByteBufferAsyncProcessor> obj = new Allocator<List<ByteBufferAsyncProcessor>>{
                ["_items"] = new ByteBufferAsyncProcessor[]{},
                ["_size"] = 0,
                ["_version"] = 0
            }.Object;
            StaticsForType<ByteBufferAsyncProcessor> obj2 = new Allocator<StaticsForType<ByteBufferAsyncProcessor>>{
                ["myList"] = obj
            }.Object;
            obj2.AddLast(null!);
        }

        [Test]
        public void RemoveLastReferenceEqualTest17()
        {
            List<ByteBufferAsyncProcessor> obj = new Allocator<List<ByteBufferAsyncProcessor>>{
                ["_items"] = new ByteBufferAsyncProcessor[]{null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!},
                ["_size"] = 93,
                ["_version"] = 0
            }.Object;
            StaticsForType<ByteBufferAsyncProcessor> obj2 = new Allocator<StaticsForType<ByteBufferAsyncProcessor>>{
                ["myList"] = obj
            }.Object;
            ByteBufferAsyncProcessor obj3 = new Allocator<ByteBufferAsyncProcessor>{
                ["AcknowledgedSeqN"] = 0L,
                ["Id"] = null!,
                ["State"] = ByteBufferAsyncProcessor.StateKind.Initialized,
                ["ChunkSize"] = 0,
                ["ShrinkIntervalMs"] = 0,
                ["myAllDataProcessed"] = false,
                ["myAsyncProcessingThread"] = null!,
                ["myChunkToFill"] = null!,
                ["myChunkToProcess"] = null!,
                ["myLastShrinkOrGrowTimeMs"] = 0,
                ["myLock"] = null!,
                ["myLog"] = null!,
                ["myPauseReasons"] = null!,
                ["myProcessing"] = false
            }.Object;
            object result = obj2.RemoveLastReferenceEqual(obj3, false);
            Assert.IsTrue(CompareObjects(result, true));
        }

        [Test]
        public void RemoveLastReferenceEqualTest18()
        {
            List<ByteBufferAsyncProcessor> obj = new Allocator<List<ByteBufferAsyncProcessor>>{
                ["_items"] = new ByteBufferAsyncProcessor[]{null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!},
                ["_size"] = 80,
                ["_version"] = 0
            }.Object;
            StaticsForType<ByteBufferAsyncProcessor> obj2 = new Allocator<StaticsForType<ByteBufferAsyncProcessor>>{
                ["myList"] = obj
            }.Object;
            ByteBufferAsyncProcessor obj3 = new Allocator<ByteBufferAsyncProcessor>{
                ["AcknowledgedSeqN"] = 0L,
                ["Id"] = null!,
                ["State"] = ByteBufferAsyncProcessor.StateKind.Initialized,
                ["ChunkSize"] = 0,
                ["ShrinkIntervalMs"] = 0,
                ["myAllDataProcessed"] = false,
                ["myAsyncProcessingThread"] = null!,
                ["myChunkToFill"] = null!,
                ["myChunkToProcess"] = null!,
                ["myLastShrinkOrGrowTimeMs"] = 0,
                ["myLock"] = null!,
                ["myLog"] = null!,
                ["myPauseReasons"] = null!,
                ["myProcessing"] = false
            }.Object;
            object result = obj2.RemoveLastReferenceEqual(obj3, false);
            Assert.IsTrue(CompareObjects(result, true));
        }

        [Test]
        public void RemoveLastReferenceEqualTest19()
        {
            List<ByteBufferAsyncProcessor> obj = new Allocator<List<ByteBufferAsyncProcessor>>{
                ["_items"] = new ByteBufferAsyncProcessor[]{null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!},
                ["_size"] = 80,
                ["_version"] = 0
            }.Object;
            StaticsForType<ByteBufferAsyncProcessor> obj2 = new Allocator<StaticsForType<ByteBufferAsyncProcessor>>{
                ["myList"] = obj
            }.Object;
            ByteBufferAsyncProcessor obj3 = new Allocator<ByteBufferAsyncProcessor>{
                ["AcknowledgedSeqN"] = 0L,
                ["Id"] = null!,
                ["State"] = ByteBufferAsyncProcessor.StateKind.Initialized,
                ["ChunkSize"] = 0,
                ["ShrinkIntervalMs"] = 0,
                ["myAllDataProcessed"] = false,
                ["myAsyncProcessingThread"] = null!,
                ["myChunkToFill"] = null!,
                ["myChunkToProcess"] = null!,
                ["myLastShrinkOrGrowTimeMs"] = 0,
                ["myLock"] = null!,
                ["myLog"] = null!,
                ["myPauseReasons"] = null!,
                ["myProcessing"] = false
            }.Object;
            object result = obj2.RemoveLastReferenceEqual(obj3, false);
            Assert.IsTrue(CompareObjects(result, true));
        }

        [Test]
        public void RemoveLastReferenceEqualTest20()
        {
            List<ByteBufferAsyncProcessor> obj = new Allocator<List<ByteBufferAsyncProcessor>>{
                ["_items"] = new ByteBufferAsyncProcessor[]{null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!},
                ["_size"] = 94,
                ["_version"] = 0
            }.Object;
            StaticsForType<ByteBufferAsyncProcessor> obj2 = new Allocator<StaticsForType<ByteBufferAsyncProcessor>>{
                ["myList"] = obj
            }.Object;
            ByteBufferAsyncProcessor obj3 = new Allocator<ByteBufferAsyncProcessor>{
                ["AcknowledgedSeqN"] = 0L,
                ["Id"] = null!,
                ["State"] = ByteBufferAsyncProcessor.StateKind.Initialized,
                ["ChunkSize"] = 0,
                ["ShrinkIntervalMs"] = 0,
                ["myAllDataProcessed"] = false,
                ["myAsyncProcessingThread"] = null!,
                ["myChunkToFill"] = null!,
                ["myChunkToProcess"] = null!,
                ["myLastShrinkOrGrowTimeMs"] = 0,
                ["myLock"] = null!,
                ["myLog"] = null!,
                ["myPauseReasons"] = null!,
                ["myProcessing"] = false
            }.Object;
            object result = obj2.RemoveLastReferenceEqual(obj3, false);
            Assert.IsTrue(CompareObjects(result, true));
        }

        [Test]
        public void ToUIntTest()
        {
            object result = Cast32BitEnum<LifetimeStatus>.ToUInt(LifetimeStatus.Alive);
            Assert.IsTrue(CompareObjects(result, 0U));
        }

        [Test]
        public void RemoveLastReferenceEqualTest21()
        {
            List<ByteBufferAsyncProcessor> obj = new Allocator<List<ByteBufferAsyncProcessor>>{
                ["_items"] = new ByteBufferAsyncProcessor[]{null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!},
                ["_size"] = 92,
                ["_version"] = 0
            }.Object;
            StaticsForType<ByteBufferAsyncProcessor> obj2 = new Allocator<StaticsForType<ByteBufferAsyncProcessor>>{
                ["myList"] = obj
            }.Object;
            object result = obj2.RemoveLastReferenceEqual(null!, false);
            Assert.IsTrue(CompareObjects(result, true));
        }

        [Test]
        public void AddFirstTest3()
        {
            List<ByteBufferAsyncProcessor> obj = new Allocator<List<ByteBufferAsyncProcessor>>{
                ["_items"] = new ByteBufferAsyncProcessor[]{null!},
                ["_size"] = 0,
                ["_version"] = 0
            }.Object;
            StaticsForType<ByteBufferAsyncProcessor> obj2 = new Allocator<StaticsForType<ByteBufferAsyncProcessor>>{
                ["myList"] = obj
            }.Object;
            obj2.AddFirst(null!);
        }

        [Test]
        public void AddFirstTest4()
        {
            List<ByteBufferAsyncProcessor> obj = new Allocator<List<ByteBufferAsyncProcessor>>{
                ["_items"] = new ByteBufferAsyncProcessor[]{null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!},
                ["_size"] = 32,
                ["_version"] = 0
            }.Object;
            StaticsForType<ByteBufferAsyncProcessor> obj2 = new Allocator<StaticsForType<ByteBufferAsyncProcessor>>{
                ["myList"] = obj
            }.Object;
            obj2.AddFirst(null!);
        }

        [Test]
        public void RemoveLastReferenceEqualTest22()
        {
            List<ByteBufferAsyncProcessor> obj = new Allocator<List<ByteBufferAsyncProcessor>>{
                ["_items"] = new ByteBufferAsyncProcessor[]{null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!},
                ["_size"] = 91,
                ["_version"] = 0
            }.Object;
            StaticsForType<ByteBufferAsyncProcessor> obj2 = new Allocator<StaticsForType<ByteBufferAsyncProcessor>>{
                ["myList"] = obj
            }.Object;
            ByteBufferAsyncProcessor obj3 = new Allocator<ByteBufferAsyncProcessor>{
                ["AcknowledgedSeqN"] = 0L,
                ["Id"] = null!,
                ["State"] = ByteBufferAsyncProcessor.StateKind.Initialized,
                ["ChunkSize"] = 0,
                ["ShrinkIntervalMs"] = 0,
                ["myAllDataProcessed"] = false,
                ["myAsyncProcessingThread"] = null!,
                ["myChunkToFill"] = null!,
                ["myChunkToProcess"] = null!,
                ["myLastShrinkOrGrowTimeMs"] = 0,
                ["myLock"] = null!,
                ["myLog"] = null!,
                ["myPauseReasons"] = null!,
                ["myProcessing"] = false
            }.Object;
            object result = obj2.RemoveLastReferenceEqual(obj3, false);
            Assert.IsTrue(CompareObjects(result, true));
        }

        [Test]
        public void ReturnTest()
        {
            Stack<ByteBufferAsyncProcessor> obj = new Allocator<Stack<ByteBufferAsyncProcessor>>{
                ["_array"] = null!,
                ["_size"] = 0,
                ["_version"] = 0
            }.Object;
            SingleThreadObjectPool<ByteBufferAsyncProcessor> obj2 = new Allocator<SingleThreadObjectPool<ByteBufferAsyncProcessor>>{
                ["myMaxCapacity"] = 0,
                ["myPool"] = obj,
                ["myThread"] = null!
            }.Object;
            obj2.Return(null!);
        }

        [Test]
        public void EnumTest2()
        {
            object result = BitSlice.Enum(null!);
            Enum32BitSlice<LifetimeStatus> obj = new Allocator<Enum32BitSlice<LifetimeStatus>>{
                ["BitCount"] = 2,
                ["LoBit"] = 0,
                ["Mask"] = 3,
                ["BitCount"] = 2,
                ["LoBit"] = 0,
                ["Mask"] = 3,
                ["BitCount"] = 2,
                ["LoBit"] = 0,
                ["Mask"] = 3
            }.Object;
            Assert.IsTrue(CompareObjects(result, obj));
        }

        [Test]
        public void GetHiBitTest()
        {
            BitSlice obj = new Allocator<BitSlice>{
                ["BitCount"] = 0,
                ["LoBit"] = 0,
                ["Mask"] = 0
            }.Object;
            object result = obj.HiBit;
            Assert.IsTrue(CompareObjects(result, -1));
        }

        [Test]
        public void IntTest()
        {
            BitSlice obj = new Allocator<BitSlice>{
                ["BitCount"] = 0,
                ["LoBit"] = 0,
                ["Mask"] = 0
            }.Object;
            object result = BitSlice.Int(0, obj);
            IntBitSlice obj2 = new Allocator<IntBitSlice>{
                ["BitCount"] = 0,
                ["LoBit"] = 0,
                ["Mask"] = 0,
                ["BitCount"] = 0,
                ["LoBit"] = 0,
                ["Mask"] = 0,
                ["BitCount"] = 0,
                ["LoBit"] = 0,
                ["Mask"] = 0
            }.Object;
            Assert.IsTrue(CompareObjects(result, obj2));
        }

        [Test]
        public void RemoveLastReferenceEqualError()
        {
            List<ByteBufferAsyncProcessor> obj = new Allocator<List<ByteBufferAsyncProcessor>>{
                ["_items"] = new ByteBufferAsyncProcessor[]{},
                ["_size"] = 32,
                ["_version"] = 0
            }.Object;
            StaticsForType<ByteBufferAsyncProcessor> obj2 = new Allocator<StaticsForType<ByteBufferAsyncProcessor>>{
                ["myList"] = obj
            }.Object;
            obj2.RemoveLastReferenceEqual(null!, false);
        }

        [Test]
        public void BoolTest2()
        {
            BitSlice obj = new Allocator<BitSlice>{
                ["BitCount"] = 0,
                ["LoBit"] = 0,
                ["Mask"] = 0
            }.Object;
            object result = BitSlice.Bool(obj);
            BoolBitSlice obj2 = new Allocator<BoolBitSlice>{
                ["BitCount"] = 1,
                ["LoBit"] = 0,
                ["Mask"] = 1,
                ["BitCount"] = 1,
                ["LoBit"] = 0,
                ["Mask"] = 1,
                ["BitCount"] = 1,
                ["LoBit"] = 0,
                ["Mask"] = 1
            }.Object;
            Assert.IsTrue(CompareObjects(result, obj2));
        }

        [Test]
        public void UpdatedTest()
        {
            IntBitSlice obj = new Allocator<IntBitSlice>{
                ["BitCount"] = 0,
                ["LoBit"] = 0,
                ["Mask"] = 0,
                ["BitCount"] = 0,
                ["LoBit"] = 0,
                ["Mask"] = 0,
                ["BitCount"] = 0,
                ["LoBit"] = 0,
                ["Mask"] = 0
            }.Object;
            object result = obj.Updated(0, 0);
            Assert.IsTrue(CompareObjects(result, 0));
        }

        [Test]
        public void GetItemTest()
        {
            IntBitSlice obj = new Allocator<IntBitSlice>{
                ["BitCount"] = 0,
                ["LoBit"] = 0,
                ["Mask"] = 0,
                ["BitCount"] = 0,
                ["LoBit"] = 0,
                ["Mask"] = 0,
                ["BitCount"] = 0,
                ["LoBit"] = 0,
                ["Mask"] = 0
            }.Object;
            object result = obj.Item;
            Assert.IsTrue(CompareObjects(result, 0));
        }

        [Test]
        public void IntTest2()
        {
            object result = BitSlice.Int(0, null!);
            IntBitSlice obj = new Allocator<IntBitSlice>{
                ["BitCount"] = 0,
                ["LoBit"] = 0,
                ["Mask"] = 0,
                ["BitCount"] = 0,
                ["LoBit"] = 0,
                ["Mask"] = 0,
                ["BitCount"] = 0,
                ["LoBit"] = 0,
                ["Mask"] = 0
            }.Object;
            Assert.IsTrue(CompareObjects(result, obj));
        }

        [Test]
        public void ReplaceFirstError5()
        {
            StaticsForType<ByteBufferAsyncProcessor> obj = new Allocator<StaticsForType<ByteBufferAsyncProcessor>>{
                ["myList"] = null!
            }.Object;
            obj.ReplaceFirst(null!);
        }

        [Test]
        public void BarrierTest()
        {
            Memory.Barrier();
        }

        [Test]
        public void UpdatedTest2()
        {
            BoolBitSlice obj = new Allocator<BoolBitSlice>{
                ["BitCount"] = 0,
                ["LoBit"] = 0,
                ["Mask"] = 0,
                ["BitCount"] = 0,
                ["LoBit"] = 0,
                ["Mask"] = 0,
                ["BitCount"] = 0,
                ["LoBit"] = 0,
                ["Mask"] = 0
            }.Object;
            object result = obj.Updated(0, false);
            Assert.IsTrue(CompareObjects(result, 0));
        }

        [Test]
        public void PeekFirstError2()
        {
            List<ByteBufferAsyncProcessor> obj = new Allocator<List<ByteBufferAsyncProcessor>>{
                ["_items"] = new ByteBufferAsyncProcessor[]{},
                ["_size"] = 1,
                ["_version"] = 0
            }.Object;
            StaticsForType<ByteBufferAsyncProcessor> obj2 = new Allocator<StaticsForType<ByteBufferAsyncProcessor>>{
                ["myList"] = obj
            }.Object;
            obj2.PeekFirst();
        }

        [Test]
        public void PeekFirstError3()
        {
            List<ByteBufferAsyncProcessor> obj = new Allocator<List<ByteBufferAsyncProcessor>>{
                ["_items"] = null!,
                ["_size"] = 1,
                ["_version"] = 0
            }.Object;
            StaticsForType<ByteBufferAsyncProcessor> obj2 = new Allocator<StaticsForType<ByteBufferAsyncProcessor>>{
                ["myList"] = obj
            }.Object;
            obj2.PeekFirst();
        }

        [Test]
        public void FromIntTest()
        {
            object result = Cast32BitEnum<LifetimeStatus>.FromInt(0);
            Assert.IsTrue(CompareObjects(result, LifetimeStatus.Alive));
        }

        [Test]
        public void VolatileReadTest()
        {
            ByteBufferAsyncProcessor obj = null!;
            object result = Memory.VolatileRead(ref obj);
            Assert.IsTrue(CompareObjects(result, null!));
        }

        [Test]
        public void UpdatedTest3()
        {
            BoolBitSlice obj = new Allocator<BoolBitSlice>{
                ["BitCount"] = 0,
                ["LoBit"] = 0,
                ["Mask"] = 0,
                ["BitCount"] = 0,
                ["LoBit"] = 0,
                ["Mask"] = 0,
                ["BitCount"] = 0,
                ["LoBit"] = 0,
                ["Mask"] = 0
            }.Object;
            object result = obj.Updated(0, true);
            Assert.IsTrue(CompareObjects(result, 1));
        }

        [Test]
        public void CopyMemoryTest()
        {
            Memory.CopyMemory(null!, null!, 0);
        }

        [Test]
        public void GetItemTest2()
        {
            BoolBitSlice obj = new Allocator<BoolBitSlice>{
                ["BitCount"] = 0,
                ["LoBit"] = 0,
                ["Mask"] = 0,
                ["BitCount"] = 0,
                ["LoBit"] = 0,
                ["Mask"] = 0,
                ["BitCount"] = 0,
                ["LoBit"] = 0,
                ["Mask"] = 0
            }.Object;
            object result = obj.Item;
            Assert.IsTrue(CompareObjects(result, false));
        }
    }
}
