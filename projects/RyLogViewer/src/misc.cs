using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using pr.common;
using pr.gui;
using pr.extn;

namespace RyLogViewer
{
	public static class Constants
	{
		public const string SupportEmail            = "support@rylogic.co.nz";
		public const string FreeLicence             = "Free Edition Licence";
		public const string AppIdentifier           = "rylogviewer";
		public const string Purchase                = "purchase";
		public const string StoreLink               = "http://store.kagi.com/cgi-bin/store.cgi?storeID=6FFFY_LIVE";
		public const int MaxHistoryDefault          = 10;
		public const int PortNumberMin              = 0;
		public const int PortNumberWebProxyDefault  = 8080;
		public const int PortNumberMax              = 65535;
		public const int FileBufSizeMin             = 256 * OneKB;
		public const int FileBufSizeDefault         = 1 * OneMB;
		public const int FileBufSizeMax             = 100 * OneMB;
		public const int MaxLineLengthMin           = 1 * OneKB;
		public const int MaxLineLengthDefault       = 16 * OneKB;
		public const int MaxLineLengthMax           = 128 * OneKB;
		public const int LineCacheCountMin          = 1;
		public const int LineCacheCountDefault      = 1000;
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

		public static readonly Color[] BkColors = new[]
			{
				Color.LightGreen, Color.LightBlue, Color.LightCoral, Color.LightSalmon, Color.Violet, Color.LightSkyBlue,
				Color.Aquamarine, Color.Yellow, Color.Orchid, Color.GreenYellow, Color.PaleGreen, Color.Goldenrod, Color.MediumTurquoise
			};
	}

	public static class FreeEditionLimits
	{
		public const int MaxHighlights = 5;
		public const int MaxFilters    = 5;
		public const int MaxTransforms = 5;
		public const int MaxActions    = 5;
		public const int FeatureEnableTimeInMinutes = 15;
	}

	public static class FeatureName
	{
		public const string AggregateFiles = "AggregateFiles";
		public const string Highlighting   = "Highlighting";
		public const string Filtering      = "Filtering";
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

		/// <summary>An option that changes how the window is positioned/displayed</summary>
		WindowDisplay = 1 << 3,

		/// <summary>who knows...</summary>
		Everything = ~0,
	}

	public static class XmlTag
	{
		public const string Version        = "version";
		public const string Name           = "name";
		public const string LicenceHolder  = "licence_holder";
		public const string EmailAddr      = "email_address";
		public const string Company        = "company";
		public const string AppVersion     = "app_version";
		public const string ActivationCode = "activation_code";
		public const string Root           = "root";
		public const string Expr           = "expr";
		public const string Active         = "active";
		public const string PatnType       = "patntype";
		public const string IgnoreCase     = "ignorecase";
		public const string Invert         = "invert";
		public const string WholeLine      = "wholeline";
		public const string Binary         = "binary";
		public const string Highlight      = "highlight";
		public const string ForeColour     = "forecolour";
		public const string BackColour     = "backcolour";
		public const string Filter         = "filter";
		public const string Transform      = "transform";
		public const string ClkAction      = "clkaction";
		public const string Exclude        = "exclude";
		public const string IfMatch        = "ifmatch";
		public const string Filepath       = "filepath";
		public const string Match          = "match";
		public const string Pattern        = "pattern";
		public const string Replace        = "replace";
		public const string Subs           = "subs";
		public const string Sub            = "sub";
		public const string SubData        = "subdata";
		public const string Src            = "src";
		public const string Dst            = "dst";
		public const string Type           = "type";
		public const string Elem           = "elem";
		public const string Id             = "id";
		public const string Tag            = "tag";
		public const string CodeValues     = "codevalues";
		public const string CodeValue      = "cv";
		public const string Code           = "c";
		public const string Value          = "v";
		public const string Executable     = "executable";
		public const string Arguments      = "arguments";
		public const string WorkingDir     = "working_dir";
		public const string ADBPath        = "adb_path";
		public const string Device         = "device";
		public const string CaptureOutput  = "capture";
		public const string AppendOutput   = "append";
		public const string LogBuffers     = "log_buffers";
		public const string FilterSpecs    = "filter_specs";
		public const string LogFormat      = "log_format";
		public const string ConnType       = "conn_type";
		public const string IPAddrHistory  = "ip_history";
		public const string Port           = "port";
		public const string Priority       = "priority";
	}

	public static class CmdLineOption
	{
		public const string ShowHelp     = "-h";
		public const string ShowHelp2    = "/?";
		public const string SettingsPath = "-s";
		public const string LogFilePath  = "-l";
		public const string Portable     = "-p";
		public const string HighlightSet = "-hl";
		public const string FilterSet    = "-ft";
		public const string TransformSet = "-tx";
		public const string Export       = "-e";
		public const string RDelim       = "-rdelim";
		public const string CDelim       = "-cdelim";
		public const string NoGUI        = "-nogui";
	}

	public static class Misc
	{
		/// <summary>Returns true if this is the main thread</summary>
		public static bool IsMainThread { get { return Thread.CurrentThread.ManagedThreadId == m_main_thread_id; } }
		private static readonly int m_main_thread_id = Thread.CurrentThread.ManagedThreadId;

		/// <summary>Return a background colour appropriate for a validity state</summary>
		public static Color FieldBkColor(bool is_valid) { return is_valid ? FieldValid : FieldInvalid; }
		public static Color FieldValid   = Color.LightGreen;
		public static Color FieldInvalid = Color.Salmon;

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

		/// <summary>Checks for the existence of a file without blocking the UI</summary>
		public static bool FileExists(Form parent, string filepath)
		{
			// Check that the file exists, this can take ages if 'filepath' is a network file
			bool file_exists = false;
			var dlg = new ProgressForm("Open File", "Opening file...", null, ProgressBarStyle.Marquee, (s,a,cb) => file_exists = Path_.FileExists(filepath));
			dlg.ShowDialog(parent, 500);
			return file_exists;
		}

		/// <summary>Helper for populating a combo box from an array of items</summary>
		public static void Load<T>(this ComboBox combo, IList<T> items)
		{
			foreach (var s in items) combo.Items.Add(s);
			if (items.Count != 0) combo.SelectedIndex = 0;
		}

		/// <summary>A wrapper around showing message boxes for exceptions</summary>
		public static void ShowMessage(Control owner, string caption, string title, MessageBoxIcon icon, Exception exception = null)
		{
			// Only show one error dialog at a time
			if (Interlocked.CompareExchange(ref m_dialog_visible, 1, 0) != 0)
				return;

			var msg = new StringBuilder(caption);
			if (exception != null)
			{
				msg.AppendLine();
				msg.AppendLine("Details:");
				msg.AppendLine(exception.Message);
				for (var ex = exception.InnerException; ex != null; ex = ex.InnerException)
					msg.AppendLine(ex.Message);
			}

			MsgBox.Show(owner, msg.ToString(), title, MessageBoxButtons.OK, icon);

			Interlocked.Exchange(ref m_dialog_visible, 0);
		}
		private static int m_dialog_visible = 0;

		/// <summary>A wrapper around showing a hint balloon</summary>
		public static void ShowHint(Control owner, string caption, Point offset, int duration = 5000)
		{
			m_balloon.Show(owner, offset, caption, duration);
		}
		public static void ShowHint(Control owner, string caption, int duration = 5000)
		{
			m_balloon.Show(owner, owner != null ? owner.ClientRectangle.Centre() : Point.Empty, caption, duration);
		}
		public static void ShowHint(ToolStripItem item, string caption, Point offset, int duration = 5000)
		{
			var parent = item.GetCurrentParent();
			if (parent == null) return;
			ShowHint(parent, caption, offset, duration);
		}
		public static void ShowHint(ToolStripItem item, string caption, int duration = 5000)
		{
			var parent = item.GetCurrentParent();
			if (parent == null) return;
			ShowHint(parent, caption, item.Bounds.Centre(), duration);
		}
		private static readonly HintBalloon m_balloon = new HintBalloon();

		/// <summary>Helper for passing an action directly to BeginInvoke</summary>
		public static void BeginInvoke<TForm>(this TForm form, Action action) where TForm:Form
		{
			form.BeginInvoke(action);
		}

		/// <summary>
		/// Returns the index in 'buf' of one past the next delimiter, starting from 'start'.
		/// If not found, returns -1 when searching backwards, or length when searching forwards</summary>
		public static int FindNextDelim(byte[] buf, int start, int length, byte[] delim, bool backward)
		{
			Debug.Assert(start >= -1 && start <= length);
			int i = start, di = backward ? -1 : 1;
			for (; i >= 0 && i < length; i += di)
			{
				// Quick test using the first byte of the delimiter
				if (buf[i] != delim[0]) continue;

				// Test the remaining bytes of the delimiter
				bool is_match = (i + delim.Length) <= length;
				for (int j = 1; is_match && j != delim.Length; ++j) is_match = buf[i + j] == delim[j];
				if (!is_match) continue;

				// 'i' now points to the start of the delimiter,
				// shift it forward to one past the delimiter.
				i += delim.Length;
				break;
			}
			return i;
		}
	}
}