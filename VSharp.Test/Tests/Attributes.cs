using System;
using System.Diagnostics.CodeAnalysis;
using NUnit.Framework;
using VSharp.Test;

namespace IntegrationTests
{
    [TestSvmFixture]
    public class Attributes
    {
        [TestSvm]
        public int DisallowNullTest1([DisallowNull] object obj)
        {
            if (obj == null)
            {
                throw new NullReferenceException();
            }
            return 1;
        }

        [TestSvm]
        public int DisallowNullTest2([DisallowNull] object obj)
        {
            if (obj != null)
            {
                return 1;
            }
            return 0;
        }

        [TestSvm]
        public int DisallowNullTest3([DisallowNull] Typecast.Piece piece, int n)
        {
            if (n == 42)
            {
                return piece.GetRate();
            }
            if (n == 43)
            {
                return piece.GetRate() + n;
            }
            return 1;
        }

        public interface INetwork
        {
            int Read();
        }


        [TestSvm(100, guidedMode:false, strat:SearchStrategy.ShortestDistance)]
        public int ReadAll(byte[] buffer, INetwork network)
        {
            int next;
            int count = 0;
            while ((next = network.Read()) >= 0)
            {
                buffer[count++] = (byte)next;
            }

            return count;
        }

        [TestSvm]
        public int NotNullTest1([NotNull] object obj)
        {
            if (obj == null)
            {
                throw new NullReferenceException();
            }
            return 1;
        }

        [TestSvm]
        public int NotNullTest2([NotNull] object obj)
        {
            if (obj != null)
            {
                return 1;
            }
            return 0;
        }

        [TestSvm]
        public int NotNullTest3([NotNull] object obj)
        {
            return 1;
        }

        [TestSvm]
        [return : NotNull]
        public object NotNullTest4([DisallowNull] object obj)
        {
            return obj;
        }

        [TestSvm]
        [return : NotNull]
        public object NotNullTest5(object obj)
        {
            return obj;
        }

        [TestSvm]
        public int NotNullCallsDisallowNullTest1([NotNull] object obj)
        {
            return DisallowNullTest1(obj);
        }

        [TestSvm]
        public int NotNullCallsDisallowNullTest2([NotNull] object obj)
        {
            if (obj == null)
            {
                return DisallowNullTest1(obj);
            }
            return 1;
        }

        [TestSvm]
        public int NotNullCallsDisallowNullTest3([NotNull] object obj, int n)
        {
            if (n > 3)
            {
                return NotNullCallsDisallowNullTest3(obj, 3);
            }
            if (n > 0)
            {
                return NotNullCallsDisallowNullTest3(obj, n - 1);
            }
            if (obj == null)
            {
                return DisallowNullTest1(obj);
            }
            return 1;
        }

        [TestSvm]
        public int DisallowNullCallsNotNullTest([DisallowNull] object obj)
        {
            return NotNullTest1(obj);
        }
    }

    [TestSvmFixture]
    public struct AttributesStruct
    {
        [TestSvm]
        public int DisallowNullTest([DisallowNull] object obj)
        {
            if (obj == null)
            {
                throw new NullReferenceException();
            }
            return 1;
        }

        [TestSvm]
        public int NotNullCallsDisallowNullTest([NotNull] object obj, int n)
        {
            if (n > 3)
            {
                return NotNullCallsDisallowNullTest(obj, 3);
            }
            if (n > 0)
            {
                return NotNullCallsDisallowNullTest(obj, n - 1);
            }
            if (obj == null)
            {
                return DisallowNullTest(obj);
            }
            return 1;
        }
    }
}
