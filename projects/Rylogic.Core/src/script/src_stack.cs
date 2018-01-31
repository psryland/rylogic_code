using System;
using System.Collections.Generic;

namespace Rylogic.Script
{
	/// <summary>A stack of script character sources</summary>
	public sealed class SrcStack :Src
	{
		private readonly Stack<Buffer> m_stack;

		public SrcStack() :this(null) {}
		public SrcStack(Src src)
		{
			m_stack = new Stack<Buffer>();
			if (src != null) Push(src);
		}
		public override void Dispose()
		{
			while (m_stack.Count != 0)
				m_stack.Pop().Dispose();
		}

		/// <summary>The type of source this is</summary>
		public override SrcType SrcType { get { return Empty ? SrcType.Unknown : Top.SrcType; } }

		/// <summary>The 'file position' within the source</summary>
		public override Loc Location { get { return Empty ? null : Top.Location; } }

		/// <summary>True if there are no sources on the stack</summary>
		public bool Empty { get { return m_stack.Count == 0; } }

		/// <summary>The top script source on the stack</summary>
		public Buffer Top { get { return Empty ? null : m_stack.Peek(); } }

		/// <summary>Push a script source onto the stack</summary>
		public void Push(Src src)
		{
			if (src == null) throw new ArgumentNullException("src", "Null character source provided");
			m_stack.Push(src as Buffer ?? new Buffer(src));
			Advance(0);
		}

		/// <summary>Pop a source from the stack</summary>
		public void Pop()
		{
			m_stack.Pop().Dispose();
			Advance(0);
		}

		/// <summary>Returns the character at the current source position or 0 when the source is exhausted</summary>
		protected override char PeekInternal()
		{
			return Empty ? '\0' : Top.Peek;
		}

		/// <summary>
		/// Advances the internal position a minimum of 'n' positions.
		/// If the character at the new position is not a valid character to be return keep
		/// advancing to the next valid character</summary>
		protected override void Advance(int n)
		{
			for (;;)
			{
				for (; !Empty && Top.Peek == 0; Pop()) {}
				if (!Empty && n-- != 0) m_stack.Peek().Next();
				else break;
			}
		}

		public override string ToString() { return Empty ? "<empty>" : Top.ToString(); }
	}
}

#if PR_UNITTESTS
namespace Rylogic.UnitTests
{
	using Script;

	[TestFixture] public partial class TestScript
	{
		[Test] public void SrcStack()
		{
			const string str1 = "one";
			const string str2 = "two";
			var src1 = new StringSrc(str1);
			var src2 = new StringSrc(str2);
			var stack = new SrcStack(src1);

			for (int i = 0; i != 2; ++i, stack.Next())
				Assert.AreEqual(str1[i], stack.Peek);

			stack.Push(src2);

			for (int i = 0; i != 3; ++i, stack.Next())
				Assert.AreEqual(str2[i], stack.Peek);

			for (int i = 2; i != 3; ++i, stack.Next())
				Assert.AreEqual(str1[i], stack.Peek);

			Assert.AreEqual((char)0, stack.Peek);
		}
	}
}
#endif
