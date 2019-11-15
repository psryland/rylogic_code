using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;
using Rylogic.Gui.WPF;
using Rylogic.Utility;

namespace LDraw
{
	public sealed partial class LogUI :Grid, IDockable, IDisposable
	{
		public LogUI(Model model)
		{
			InitializeComponent();
			DockControl.Owner = this;
			Model = model;
			m_log.LogEntryPattern = LogEntryPatternRegex;
			m_log.PopOutOnNewMessages = false;
			m_log.LogFilepath = Log.Filepath;
			m_log.FilterLevel = ELogLevel.Debug;

			// Hide all columns except the Message column initially
			foreach (var col in m_log.Columns)
				col.Visibility = (col.Header is string header && header == LogControl.ColumnNames.Message) ? Visibility.Visible : Visibility.Collapsed;
		}
		public void Dispose()
		{
			Model = null!;
			m_log.Dispose();
		}

		/// <summary></summary>
		public DockControl DockControl => m_log.DockControl;

		/// <summary>App logic</summary>
		public Model Model
		{
			get => m_model;
			private set
			{
				if (m_model == value) return;
				if (m_model != null)
				{
				}
				m_model = value;
				if (m_model != null)
				{
				}
			}
		}
		private Model m_model = null!;

		/// <summary>Log line pattern</summary>
		private static readonly Regex LogEntryPatternRegex = new Regex(
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
