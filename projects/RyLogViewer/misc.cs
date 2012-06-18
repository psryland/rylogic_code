using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Text;

namespace RyLogViewer
{
	public static class Constants
	{
		public const int FileReadChunkSize                = 4096;
		public const int FilePollingRate                  = 100;
		public const int MaxProgramHistoryLength          = 10;
		public const int MaxNetConnHistoryLength          = 10;
		public const int MaxSerialConnHistoryLength       = 1;
		public const int MaxOutputFileHistoryLength       = 10;
	}

	public enum EPattern
	{
		Substring,
		Wildcard,
		RegularExpression
	}

	public enum ELineEnding
	{
		Detect,
		CR,
		CRLF,
		LF,
		Custom
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

	[DataContract]
	public class SerialConn :ICloneable
	{
		// Notes: It is usually recommended to set DTR and RTS true.
		// If the connected device uses these signals, it will not transmit before
		// the signals are set

		[DataMember] public string       CommPort         = "";
		[DataMember] public int          BaudRate         = 9600;
		[DataMember] public int          DataBits         = 8;
		[DataMember] public Parity       Parity           = Parity.None;
		[DataMember] public StopBits     StopBits         = StopBits.One;
		[DataMember] public Handshake    FlowControl      = Handshake.None;
		[DataMember] public bool         DtrEnable        = true;
		[DataMember] public bool         RtsEnable        = true;
		[DataMember] public string       OutputFilepath   = "";
		[DataMember] public bool         AppendOutputFile = true;
		
		public SerialConn() {}
		public SerialConn(SerialConn rhs)
		{
			CommPort         = rhs.CommPort;
			BaudRate         = rhs.BaudRate;
			DataBits         = rhs.DataBits;
			Parity           = rhs.Parity;
			StopBits         = rhs.StopBits;
			FlowControl      = rhs.FlowControl;
			DtrEnable        = rhs.DtrEnable;
			RtsEnable        = rhs.RtsEnable;
			OutputFilepath   = rhs.OutputFilepath;
			AppendOutputFile = rhs.AppendOutputFile;
		}
		public override string ToString()
		{
			return CommPort;
		}
		public object Clone()
		{
			return new SerialConn(this);
		}
	}

	[DataContract]
	public class PipeConn :ICloneable
	{
		// Notes: It is usually recommended to set DTR and RTS true.
		// If the connected device uses these signals, it will not transmit before
		// the signals are set

		[DataMember] public string       ServerName       = "";
		[DataMember] public string       PipeName         = "";
		[DataMember] public string       OutputFilepath   = "";
		[DataMember] public bool         AppendOutputFile = true;
		
		public string PipeAddr
		{
			get { return string.Format(@"\\{0}\pipe\{1}",ServerName,PipeName); }
			set
			{
				var parts = value.Replace(@"\\", string.Empty).Split('\\');
				if (parts.Length != 3 || parts[1] != "pipe") throw new ArgumentException("Invalid pipe name");
				ServerName = parts[0];
				PipeName = parts[2];
			}
		}

		public PipeConn() {}
		public PipeConn(PipeConn rhs)
		{
			ServerName       = rhs.ServerName;
			PipeName         = rhs.PipeName;
			OutputFilepath   = rhs.OutputFilepath;
			AppendOutputFile = rhs.AppendOutputFile;
		}
		public override string ToString()
		{
			return PipeAddr;
		}
		public object Clone()
		{
			return new PipeConn(this);
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

		/// <summary>Add 'item' to a history list of items</summary>
		public static void AddToHistoryList<T>(List<T> list, T item, bool ignore_case, int max_history_length)
		{
			string item_name = item.ToString();
			if (item_name.Length == 0) return;
			StringComparison cmp = ignore_case ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
			list.RemoveAll(i => String.Compare(i.ToString(), item_name, cmp) == 0);
			list.Insert(0, item);
			
			if (list.Count > max_history_length)
				list.RemoveRange(max_history_length, list.Count - max_history_length);
		}
	}
}