using System;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using Rylogic.Gui.WPF;
using Rylogic.Utility;

namespace CoinFlip.UI
{
	public sealed partial class LogView : Grid, IDockable, IDisposable
	{
		public LogView()
		{
			InitializeComponent();

			DockControl.Owner = this;
			m_log.LogEntryPattern = LogEntryPatternRegex;
			m_log.PopOutOnNewMessages = false;
			m_log.LogFilepath = Model.Log.LogCB is LogToFile l2f ? l2f.Filepath : null;
			m_log.FilterLevel = ELogLevel.Debug;
		}
		public void Dispose()
		{
			m_log.Dispose();
		}

		/// <summary></summary>
		public DockControl DockControl => m_log.DockControl;

		/// <summary>Log line pattern</summary>
		private static readonly Regex LogEntryPatternRegex = new(
			@"^(?<Tag>.*?)\|(?<Level>.*?)\|(?<Timestamp>.*?)\|(?<Message>.*)",
			RegexOptions.Singleline | RegexOptions.Multiline | RegexOptions.CultureInvariant | RegexOptions.Compiled);

		/// <summary>Single entry highlights</summary>
		//public static readonly HLPattern[] LogHighlighting = new[]
		//{
		//	new HLPattern(Color_.FromArgb(0xffb5ffd2), Color.Black, EPattern.Substring, "Fishing started"),
		//	new HLPattern(Color_.FromArgb(0xffb5ffd2), Color.Black, EPattern.Substring, "Fishing stopped"),
		//	new HLPattern(Color_.FromArgb(0xffe7a5ff), Color.Black, EPattern.Substring, "!Profit!"),
		//	new HLPattern(Color_.FromArgb(0xfffcffae), Color.Black, EPattern.Substring, "filled"),
		//	new HLPattern(Color_.FromArgb(0xffff7e39), Color.Black, EPattern.Substring, "ignored"),
		//};
	}
}
