using System;
using System.Text;
using pr.maths;

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

	public enum ProgramOutputAction
	{
		LaunchApplication,
		AttachToProcess,
	}

	[Flags] public enum StandardStreams
	{
		Stdout = 1 << 0,
		Stderr = 1 << 1,
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

		/// <summary>Replace the \r,\n,\t characters with '&lt;CR&gt;', '&lt;LF&gt;', and '&lt;TAB&gt;'</summary>
		public static string Humanise(string str)
		{
			return str.Replace("\r","<CR>").Replace("\n","<LF>").Replace("\t","<TAB>");
		}

		/// <summary>Replace the '&lt;CR&gt;', '&lt;LF&gt;', and '&lt;TAB&gt;' strings with \r,\n,\t characters</summary>
		public static string Robitise(string str)
		{
			return str.Replace("<CR>","\r").Replace("<LF>","\n").Replace("<TAB>","\t");
		}
	}
}