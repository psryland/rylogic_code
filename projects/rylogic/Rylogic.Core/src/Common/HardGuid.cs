using System;
using System.Reflection;

namespace Rylogic.Common
{
    public class HardGuid<T> where T : HardGuid<T>
    {
        // Notes:
        // A hard-typedef for Guid

        private readonly Guid _guid;

        public HardGuid() => _guid = Guid.Empty;
        public HardGuid(Guid id) => _guid = id;

        /// <summary>Create a new ID with a random value</summary>
        public static T NewGuid() => New(Guid.NewGuid());

        /// <summary>Implicit conversion to 'Guid'</summary>
        public static implicit operator Guid(HardGuid<T> operationID)
        {
            return operationID._guid;
        }

        // Use reflection to find the constructor that takes a Guid parameter and cache the result
        private static ConstructorInfo ctor => _ctor ??= typeof(T).GetConstructor(new Type[] { typeof(Guid) }) ?? throw new MissingMethodException("Constructor taking 'Guid' parameter expected");
        private static ConstructorInfo? _ctor;

        // Construct a new 'T'
        private static T New(Guid guid)
        {
            var id = Activator.CreateInstance<T>();
            ctor.Invoke(id, new object?[] { guid });
            return id;
        }

        /// <inheritdoc/>
        public override string ToString() => _guid.ToString();

        // These operators work even when comparing 'T's
        #region Value Equality
        public static bool operator ==(HardGuid<T> left, HardGuid<T> right) => left._guid == right._guid;
        public static bool operator !=(HardGuid<T> left, HardGuid<T> right) => left._guid != right._guid;
        public override bool Equals(object? obj) => obj is HardGuid<T> id && id._guid == _guid;
        public override int GetHashCode() => _guid.GetHashCode();
        #endregion
    }
}

#if PR_UNITTESTS
namespace Rylogic.UnitTests
{
	using Common;

    public class MyGuid1 : HardGuid<MyGuid1>
    {
        public MyGuid1() {}
        public MyGuid1(Guid id) : base(id) { }
    }
    public class MyGuid2 : HardGuid<MyGuid2>
    {
        public MyGuid2() {}
        public MyGuid2(Guid id) : base(id) { }
    }

    [TestFixture]
    public class TestHardGuid
    {
        [Test]
        public void Equality()
        {
            var lhs = MyGuid1.NewGuid();
            var rhs = MyGuid2.NewGuid();

            var g0 = (Guid)lhs;
            var g1 = (Guid)rhs;
            Assert.False(g0 == g1);
            Assert.True(g0 == lhs);
            Assert.True(g1 == rhs);

            var RHS = rhs;
            if (rhs == RHS)
            {
                Assert.True(rhs == RHS);
                RHS = MyGuid2.NewGuid();
                Assert.True(rhs != RHS);
            }

        }
    }
}
#endif
