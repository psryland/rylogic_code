using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Rylogic.Common;
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
			m_log.LogEntries = Log.Entries;
			m_log.LogEntryPattern = Log.PatternRegex;
			m_log.PopOutOnNewMessages = false;
			m_log.FilterLevel = ELogLevel.Debug;
			m_log.LogEntryDoubleClick += HandleLogEntryDoubleClick;

			// Hide all columns except the Message column initially
			foreach (var col in m_log.Columns)
				col.Visibility = (col.Header is string header && header == LogControl.ColumnNames.Message) ? Visibility.Visible : Visibility.Collapsed;
		}
		public void Dispose()
		{
			Model = null!;
			m_log.LogEntryDoubleClick -= HandleLogEntryDoubleClick;
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

		/// <summary>Handle a log entry being double clicked in the log view</summary>
		private void HandleLogEntryDoubleClick(object? sender, LogControl.LogEntryDoubleClickEventArgs e)
		{
			// Find the script associated with the file, bring it into view, and scroll to the associated line
			var script = Model.Scripts.FirstOrDefault(x => Path_.Compare(e.Entry.File, x.Filepath) == 0);
			if (script == null)
				return;

			script.DockControl.IsActiveContent = true;
			script.ScrollTo(e.Entry.Line, 0, true);
		}
	}
}
