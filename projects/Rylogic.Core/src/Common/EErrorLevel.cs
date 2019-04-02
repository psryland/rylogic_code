using Rylogic.Attrib;
using Rylogic.Gfx;

namespace Rylogic.Common
{
	/// <summary>Generic error levels</summary>
	public enum EErrorLevel
	{
		// Note: foreground and background colours given
		// You probably don't want to display both at the same time though.
		[Assoc("fg", 0xFFAAAAAAU), Assoc("bg", 0xFFFFFFFFU)] Diag = -1,
		[Assoc("fg", 0xFF000000U), Assoc("bg", 0xFFFFFFFFU)] Default = 0,
		[Assoc("fg", 0xFFD7AB69U), Assoc("bg", 0xFFD7AB69U)] Warning = 1,
		[Assoc("fg", 0xFFFF0000U), Assoc("bg", 0xFFFF0000U)] Error = 2,
		[Assoc("fg", 0xFF865FC5U), Assoc("bg", 0xFF865FC5U)] Fatal = 3,
	}

	public static class EErrorLevel_
	{
		/// <summary>Get the foreground colour associated with this error level</summary>
		public static Colour32 Foreground(this EErrorLevel lvl)
		{
			return lvl.Assoc<uint>("fg");
		}

		/// <summary>Get the background colour associated with this error level</summary>
		public static Colour32 Background(this EErrorLevel lvl)
		{
			return lvl.Assoc<uint>("bg");
		}
	}
}
