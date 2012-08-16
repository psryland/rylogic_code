using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml.Linq;
using pr.inet;
using pr.extn;

namespace RyLogViewer
{
	public static class Constants
	{
		public const string UnregistedUserName      = "UNREGISTERED VERSION";
		public const string AppIdentifier           = "rylogviewer.x86";
		public const string UpdateUrl               = "http://www.rylogic.co.nz:80/versions/rylogviewer.xml";
		public const int PortNumberMin              = 0;
		public const int PortNumberWebProxyDefault  = 8080;
		public const int PortNumberMax              = 65535;
		public const int FileBufSizeMin             = 1 * OneMB;
		public const int FileBufSizeDefault         = 10 * OneMB;
		public const int FileBufSizeMax             = 100 * OneMB;
		public const int MaxLineLengthMin           = 1 * OneKB;
		public const int MaxLineLengthDefault       = 16 * OneKB;
		public const int MaxLineLengthMax           = 128 * OneKB;
		public const int LineCacheCountMin          = 1;
		public const int LineCacheCountDefault      = 10000;
		public const int LineCacheCountMax          = 99999999;
		public const int ColumnCountMin             = 1;
		public const int ColumnCountDefault         = 1;
		public const int ColumnCountMax             = 256;
		public const int FileScrollMinWidth         = 16;
		public const int FileScrollMaxWidth         = 200;
		public const int FileScrollWidthDefault     = 20;
		public const int RowHeightMinHeight         = 1;
		public const int RowHeightMaxHeight         = 200;
		public const int RowHeightDefault           = 18;
		public const int AutoScrollAtBoundaryLimit  = 10;
		public const int FilePollingRate            = 100;
		public const int MaxProgramHistoryLength    = 10;
		public const int MaxNetConnHistoryLength    = 10;
		public const int MaxSerialConnHistoryLength = 1;
		public const int MaxOutputFileHistoryLength = 10;
		public const int MaxFindHistory             = 10;
		public const int OneKB                      = 1024;
		public const int OneMB                      = 1024*1024;
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

	public static class XmlTag
	{
		public const string UserName   = "user_name";
		public const string Company    = "company";
		public const string EmailAddr  = "email";
		public const string SerialNo   = "serial";
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
		public const string Transform  = "transform";
		public const string ClkAction  = "clkaction";
		public const string Exclude    = "exclude";
		public const string IfMatch    = "ifmatch";
		public const string Name       = "name";
		public const string Filepath   = "filepath";
		public const string Match      = "match";
		public const string Pattern    = "pattern";
		public const string Replace    = "replace";
		public const string Subs       = "subs";
		public const string Sub        = "sub";
		public const string SubData    = "subdata";
		public const string Src        = "src";
		public const string Dst        = "dst";
		public const string Type       = "type";
		public const string Elem       = "elem";
		public const string Id         = "id";
		public const string CodeValues = "codevalues";
		public const string CodeValue  = "cv";
		public const string Code       = "c";
		public const string Value      = "v";
		public const string Executable = "executable";
		public const string Arguments  = "arguments";
		public const string WorkingDir = "working_dir";
	}

	public static class CmdLineOption
	{
		public const string ShowHelp     = "-h";
		public const string SettingsPath = "-s";
		public const string Portable     = "-p";
		public const string HighlightSet = "-hl";
		public const string FilterSet    = "-ft";
		public const string TransformSet = "-tx";
		public const string Export       = "-e";
		public const string RDelim       = "-rdelim";
		public const string CDelim       = "-cdelim";
		public const string NoGUI        = "-nogui";
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
		[DataMember] public string       Hostname         = "";
		[DataMember] public ushort       Port             = 5555;
		[DataMember] public ProtocolType ProtocolType     = ProtocolType.Tcp;
		[DataMember] public Proxy.EType  ProxyType        = Proxy.EType.None;
		[DataMember] public string       ProxyHostname    = "";
		[DataMember] public ushort       ProxyPort        = 5555;
		[DataMember] public string       ProxyUserName    = "";
		             public string       ProxyPassword    = ""; // don't store passwords
		[DataMember] public string       OutputFilepath   = "";
		[DataMember] public bool         AppendOutputFile = true;

		public NetConn() {}
		public NetConn(NetConn rhs)
		{
			Hostname         = rhs.Hostname         ;
			Port             = rhs.Port             ;
			ProtocolType     = rhs.ProtocolType     ;
			ProxyType        = rhs.ProxyType        ;
			ProxyHostname    = rhs.ProxyHostname    ;
			ProxyPort        = rhs.ProxyPort        ;
			ProxyUserName    = rhs.ProxyUserName    ;
			ProxyPassword    = rhs.ProxyPassword    ;
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
		/// <summary>Read a text file embedded resource returning it as a string</summary>
		public static string TextResource(string resource_name)
		{
			Assembly ass = Assembly.GetExecutingAssembly();
			Stream stream = ass.GetManifestResourceStream(resource_name);
			if (stream == null) return null;
			using (var src = new StreamReader(stream))
				return src.ReadToEnd();
		}

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
			str = Regex.Replace(str, Regex.Escape("<CR>" ), "\r", RegexOptions.IgnoreCase);
			str = Regex.Replace(str, Regex.Escape("<LF>" ), "\n", RegexOptions.IgnoreCase);
			str = Regex.Replace(str, Regex.Escape("<TAB>"), "\t", RegexOptions.IgnoreCase);
			return str;
		}

		/// <summary>Add 'item' to a history list of items.</summary>
		public static void AddToHistoryList<T>(IList<T> list, T item, bool ignore_case, int max_history_length)
		{
			string item_name = item.ToString();
			StringComparison cmp = ignore_case ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
			list.RemoveIf(i => String.Compare(i.ToString(), item_name, cmp) == 0);
			list.Insert(0, item);
			
			while (list.Count > max_history_length)
				list.RemoveAt(list.Count - 1);
		}
		
		/// <summary>A wrapper around showing message boxes for exceptions</summary>
		public static void ShowErrorMessage(IWin32Window owner, Exception exception, string caption, string title)
		{
			string msg = exception.Message;
			for (var ex = exception.InnerException; ex != null; ex = ex.InnerException) msg += Environment.NewLine + ex.Message;
			MessageBox.Show(owner, string.Format("{0}\r\nError Details:\r\n{1}", caption, msg), title, MessageBoxButtons.OK, MessageBoxIcon.Error);
		}
	}
}