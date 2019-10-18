using System.IO;

namespace Rylogic.Script
{
	/// <summary>Base class for a source of script characters</summary>
	public class Preprocessor :Src
	{
		public Preprocessor(IIncludeHandler? inc = null, IMacroHandler? mac = null, EmbeddedCodeFactory? emb = null)
		{
			Stack = new SrcStack();
			Includes = inc ?? new Includes();
			Macros = mac ?? new MacroDB();
		}
		public Preprocessor(Src src, IIncludeHandler? inc = null, IMacroHandler? mac = null, EmbeddedCodeFactory? emb = null)
			:this(inc, mac, emb)
		{
			Stack.Push(src);
		}

		/// <summary>Access the include handler</summary>
		public IIncludeHandler Includes { get; }

		/// <summary>Access the macro handler</summary>
		public IMacroHandler Macros { get; }

		/// <summary>The stack of input streams. Streams are pushed/popped from the stack as files are opened, or macros are evaluated.</summary>
		private SrcStack Stack { get; }

		/// <summary></summary>
		public override Loc Location => Stack.Location;

		/// <summary></summary>
		protected override char Read()
		{
			var ch = Stack.Peek;
			if (ch != 0) Stack.Next();
			return ch;
		}
	}
}

#if PR_UNITTESTS
namespace Rylogic.UnitTests
{
	using Script;

	[TestFixture] public partial class TestScript
	{
		[Test] public static void Preprocessor()
		{}
	}
}
#endif
