using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Rylogic.Script
{
	[DebuggerDisplay("{Description,nq}")]
	public sealed class SrcStack :IDisposable
	{
		// Notes:
		//  - SrcStack is not a 'Src' subclass because the stack must not buffer.
		//  - 'Buffer' and 'ReadAhead' is not defined because that gives the
		//    impression this class is buffering, which it isn't.

		public SrcStack()
			: this(null)
		{ }
		public SrcStack(Src? src)
		{
			Stack = new Stack<Src>();
			if (src != null)
				Push(src);
		}
		public void Dispose()
		{
			for (; !Empty;)
				Pop();
		}

		/// <summary>The stack of input streams</summary>
		private Stack<Src> Stack { get; }

		/// <summary>The 'file position' within the source</summary>
		public Loc Location => Top.Location;

		/// <summary>True if there are no sources on the stack</summary>
		public bool Empty => Stack.Count == 0;

		/// <summary>The top script source on the stack</summary>
		public Src Top => Stack.Count != 0 ? Stack.Peek() : new NullSrc();

		/// <summary>Push a script source onto the stack</summary>
		public void Push(Src src)
		{
			// The stack takes ownership of 'src' and calls dispose on it when popped.
			if (src == null)
				throw new ArgumentNullException("src", "Null character source provided");

			Stack.Push(src);
		}

		/// <summary>Pop a source from the stack</summary>
		public void Pop()
		{
			var popped = Stack.Pop();
			popped.Dispose();
		}

		/// <summary>The next character</summary>
		public char Peek
		{
			get
			{
				// Careful, make sure Peek is readonly. Don't want the debugger to change it's state when viewed in watch windows.
				var ch = Top.Peek;
				if (ch == 0 && Stack.Count != 0) throw new Exception("Stack.Pop has been missed");
				return ch;
			}
		}

		/// <summary>Increment to the next character</summary>
		public void Next(int n = 1)
		{
			for (; n > 0; --n)
			{
				Top.Next();
				for (; Stack.Count != 0 && Top.Peek == 0;)
					Pop();
			}
		}

		/// <summary>Pointer-like interface</summary>
		public static implicit operator char(SrcStack src) { return src.Peek; }
		public static SrcStack operator ++(SrcStack src) { src.Next(); return src; }
		public static SrcStack operator +(SrcStack src, int n) { src.Next(n); return src; }

		/// <summary></summary>
		public string Description => Top.Description;
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
				Assert.Equal(str1[i], stack.Peek);

			stack.Push(src2);

			for (int i = 0; i != 3; ++i, stack.Next())
				Assert.Equal(str2[i], stack.Peek);

			for (int i = 2; i != 3; ++i, stack.Next())
				Assert.Equal(str1[i], stack.Peek);

			Assert.Equal((char)0, stack.Peek);
		}
	}
}
#endif
