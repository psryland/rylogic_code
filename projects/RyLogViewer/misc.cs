using System;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Text;

namespace RyLogViewer
{
	public static class Constants
	{
		public const int FileReadChunkSize = 4096;
		public const int FilePollingRate   = 100;
	}

	public enum EPattern
	{
		Substring,
		Wildcard,
		RegularExpression
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

	public enum NetworkTransport
	{
	}

	[DataContract]
	public class LaunchApp :ICloneable
	{
		[DataMember] public string Executable       = "";
		[DataMember] public string Arguments        = "";
		[DataMember] public string WorkingDirectory = "";
		[DataMember] public string OutputFilepath   = "";
		[DataMember] public bool   ShowWindow       = false;
		[DataMember] public bool   AppendOutputFile = true;
		[DataMember] public StandardStreams Streams = StandardStreams.Stdout|StandardStreams.Stderr;
		
		public bool CaptureStdout { get { return (Streams & StandardStreams.Stdout) != 0; } }
		public bool CaptureStderr { get { return (Streams & StandardStreams.Stderr) != 0; } }

		public LaunchApp() {}
		public LaunchApp(LaunchApp rhs)
		{
			Executable       = rhs.Executable;
			Arguments        = rhs.Arguments;
			WorkingDirectory = rhs.WorkingDirectory;
			OutputFilepath   = rhs.OutputFilepath;
			ShowWindow       = rhs.ShowWindow;
			AppendOutputFile = rhs.AppendOutputFile;
			Streams          = rhs.Streams;
		}
		public override string ToString()
		{
			return Executable;
		}
		public object Clone()
		{
			return new LaunchApp(this);
		}
	}

	[DataContract]
	public class NetConn :ICloneable
	{
		[DataMember] public string       Hostname     = "";
		[DataMember] public ushort       Port         = 5555;
		[DataMember] public ProtocolType ProtocolType = ProtocolType.IPv4;
		[DataMember] public bool         UseProxy         = false;
		[DataMember] public string       ProxyHostname    = "";
		[DataMember] public ushort       ProxyPort        = 5555;
		[DataMember] public string       OutputFilepath   = "";
		[DataMember] public bool         AppendOutputFile = true;
		
		public NetConn() {}
		public NetConn(NetConn rhs)
		{
			Hostname         = rhs.Hostname         ;
			Port             = rhs.Port             ;
			ProtocolType     = rhs.ProtocolType     ;
			UseProxy         = rhs.UseProxy         ;
			ProxyHostname    = rhs.ProxyHostname    ;
			ProxyPort        = rhs.ProxyPort        ;
			OutputFilepath   = rhs.OutputFilepath   ;
			AppendOutputFile = rhs.AppendOutputFile ;
		}
		public override string ToString()
		{
			return Hostname;
		}
		public object Clone()
		{
			return new NetConn(this);
		}
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