using System;
using System.Text;

namespace RyLogViewer
{
	public static class Constants
	{
		public const int FileReadChunkSize = 4096;
	}

	public enum EPattern
	{
		Substring,
		Wildcard,
		RegularExpression
	}

	[Flags] public enum EWhatsChanged
	{
		Nothing = 0,
		
		/// <summary>Options that only effect the program on startup</summary>
		StartupOptions = 1 << 0,
		
		/// <summary>Options that affect files as they are opened</summary>
		FileOpenOptions = 1 << 1,
		
		/// <summary>An option that affects how a file is parsed</summary>
		FileParsing = 1 << 1,
		
		/// <summary>An option that changes how the log view is rendered</summary>
		Rendering = 1 << 2,
		
		/// <summary>who knows...</summary>
		Everything = ~0,
	}

	public class XmlTag
	{
		public const string Root       = "root";
		public const string Expr       = "expr";
		public const string Active     = "active";
		public const string PatnType   = "patntype";
		public const string IgnoreCase = "ignorecase";
		public const string Invert     = "invert";
		public const string Binary     = "binary";
		public const string Highlight  = "highlight";
		public const string ForeColour = "forecolour";
		public const string BackColour = "backcolour";
		public const string Filter     = "filter";
		public const string Exclude    = "exclude";
		public const string Name       = "name";
		public const string Filepath   = "filepath";
	}

	public enum SubRangeScrollRange
	{
		FileRange,
		DisplayedRange,
		SelectedRange,
	}

	public static class Misc
	{
		/// <summary>Watch window helper for converting byte buffers to strings</summary>
		public static string BufToStr(byte[] buf, int start, int len)
		{
			return Encoding.UTF8.GetString(buf, start, len);
		}
	}
}