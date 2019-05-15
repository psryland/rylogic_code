using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml.Linq;
using Rylogic.Common;
using Rylogic.Container;
using Rylogic.Extn;
using Rylogic.Utility;

namespace Rylogic.Gui.WinForms
{
	public class LogUI :UserControl ,IDockable
	{
		// Notes:
		// This component can be used in two ways, one is as a simple text
		// panel for displaying log messages, the second is as a view of a
		// log file.
		// 1)
		//  - Add an instance of the control
		//  - Call AddMessage()
		// 2)
		//  - Add an instance of the control
		//  - Set the log filepath

		private const int FilePollPeriodMS = 100;

		#region UI elements
		private ToolStripContainer m_tsc;
		private ImageList m_il_toolbar;
		private ToolStrip m_ts;
		private ToolStripButton m_btn_log_filepath;
		private ToolStripTextBox m_tb_log_filepath;
		private DataGridView m_view;
		private ToolStripButton m_btn_tail;
		private ToolTip m_tt;
		private ToolStripButton m_chk_line_wrap;
		private ToolStripButton m_btn_clear;
		private FileWatch m_watch;
		#endregion

		public LogUI()
			:this("Log","Log")
		{}
		public LogUI(string title, string persist_name)
		{
			InitializeComponent();
			m_watch = new FileWatch{ PollPeriod = TimeSpan.FromMilliseconds(FilePollPeriodMS) };

			Title = title;

			// Support for dock container controls
			DockControl = new DockControl(this, persist_name)
			{
				TabText             = Title,
				DefaultDockLocation = new DockContainer.DockLocation(auto_hide:EDockSite.Right),
				TabColoursActive    = new DockContainer.OptionData().TabStrip.ActiveTab,
			};

			// When docked in an auto-hide panel, pop out on new messages
			PopOutOnNewMessages = true;

			// Line wrap default
			SetLineWrap(false);

			// A buffer of the log entries.
			// This is populated by calls to AddMessage or from the log file.
			LogEntries = new BindingListEx<LogEntry>();

			// Define a line in the log
			LogEntryPattern = null;
				//new Regex(@"^(?<File>.*?)\|(?<Level>.*?)\|(?<Timestamp>.*?)\|(?<Message>.*)\n"
				//	,RegexOptions.Singleline|RegexOptions.Multiline|RegexOptions.CultureInvariant|RegexOptions.Compiled);
				//new Regex(@"^\u001b(?<lvl>\d),(?<time>.*?),(?<name>.*?),""(?<msg>.*?)"",""(?<except>.*?)"",(?<count>\d+)\s*\n"
				//	,RegexOptions.Singleline|RegexOptions.Multiline|RegexOptions.CultureInvariant|RegexOptions.Compiled);

			// Highlighting patterns
			Highlighting = new BindingListEx<HLPattern>();

			// Column fill weights
			FillWeights = new Dictionary<string, float>
			{
				{ColumnNames.Tag         , 0.3f},
				{ColumnNames.Level       , 0.3f},
				{ColumnNames.Timestamp   , 0.6f},
				{ColumnNames.Message     , 5.0f},
				{ColumnNames.File        , 2.0f},
				{ColumnNames.Line        , 0.02f},
				{ColumnNames.Occurrences , 0.02f},
			};

			// The log entry delimiter
			LineDelimiter = Log.EntryDelimiter;

			// Limit the number of log entries to display
			MaxLines = 500;
			MaxFileBytes = 2 * 1024 * 1024;

			// Hook up UI
			SetupUI();

			// Create straight away
			CreateHandle();
		}
		protected override void Dispose(bool disposing)
		{
			LogEntries = null;
			LogFilepath = null;
			DockControl = null;
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}

		/// <summary>Provides support for the DockContainer</summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public DockControl DockControl
		{
			[DebuggerStepThrough] get { return m_impl_dock_control; }
			private set
			{
				if (m_impl_dock_control == value) return;
				if (m_impl_dock_control != null)
				{
					m_impl_dock_control.ActiveChanged -= HandleActiveChanged;
					m_impl_dock_control.DockContainerChanged -= HandleDockContainerChanged;
					Util.Dispose(ref m_impl_dock_control);
				}
				m_impl_dock_control = value;
				if (m_impl_dock_control != null)
				{
					m_impl_dock_control.DockContainerChanged += HandleDockContainerChanged;
					m_impl_dock_control.ActiveChanged += HandleActiveChanged;
				}
			}
		}
		private DockControl m_impl_dock_control;

		/// <summary>The tab name of this control</summary>
		public string Title
		{
			get { return m_title; }
			set
			{
				if (m_title == value) return;
				m_title = value;
				Name = value;
				if (DockControl != null)
					DockControl.TabText = value;
			}
		}
		private string m_title;

		/// <summary>If docked in a doc container, pop-out when new messages are added to the log</summary>
		public bool PopOutOnNewMessages { get; set; }

		/// <summary>Line wrap mode</summary>
		public bool LineWrap
		{
			get { return m_line_wrap; }
			set
			{
				if (m_line_wrap == value) return;
				SetLineWrap(value);
			}
		}
		private bool m_line_wrap;

		/// <summary>A buffer of the log entries</summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public BindingListEx<LogEntry> LogEntries
		{
			[DebuggerStepThrough] get { return m_log_entries; }
			private set
			{
				if (m_log_entries == value) return;
				if (m_log_entries != null)
				{
					m_log_entries.ListChanging -= HandleLogEntriesChanging;
				}
				m_log_entries = value;
				if (m_log_entries != null)
				{
					m_log_entries.ListChanging += HandleLogEntriesChanging;
				}
			}
		}
		private BindingListEx<LogEntry> m_log_entries;

		/// <summary>The maximum number of lines to show in the log</summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public int MaxLines { get; set; }

		/// <summary>The maximum number of bytes to read from the log file</summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public int MaxFileBytes { get; set; }

		/// <summary>Get/Set the log file to display. Setting a filepath sets the control to the 'file tail' mode</summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public string LogFilepath
		{
			get { return m_log_filepath; }
			set
			{
				if (m_log_filepath == value) return;
				if (m_log_filepath != null)
				{
					m_watch.Remove(m_log_filepath);
					m_tb_log_filepath.Text = string.Empty;
				}
				m_log_filepath = value;
				if (m_log_filepath != null)
				{
					m_tb_log_filepath.Text = value;
					m_watch.Add(m_log_filepath, HandleFileChanged);
					HandleFileChanged(m_log_filepath, null);
				}
			}
		}
		private string m_log_filepath;

		/// <summary>A regex expression for extracting lines from the log file</summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public Regex LogEntryPattern
		{
			[DebuggerStepThrough] get { return m_log_entry_pattern; }
			set
			{
				if (m_log_entry_pattern == value) return;
				m_log_entry_pattern = value;
				CreateColumns();
			}
		}
		private Regex m_log_entry_pattern;

		/// <summary>A special character used to mark the start of a log entry. Must be a 1-byte UTF8 character</summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public char LineDelimiter { get; set; }

		/// <summary></summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public bool Freeze
		{
			get { return m_freeze; }
			set
			{
				if (m_freeze == value) return;
				if (m_freeze)
				{
					m_watch.PollPeriod = TimeSpan.FromMilliseconds(FilePollPeriodMS);
				}
				m_freeze = value;
				if (m_freeze)
				{
					m_watch.PollPeriod = TimeSpan.Zero;
				}
			}
		}
		private bool m_freeze;

		/// <summary>Access to the DGV containing the log data</summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public DataGridView LogPanel
		{
			get { return m_view; }
		}

		/// <summary>Access to the DGV containing the log data</summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public BindingListEx<HLPattern> Highlighting { get; private set; }

		/// <summary>Column names to fill weights for those columns</summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public Dictionary<string, float> FillWeights { get; private set; }

		/// <summary>Set up UI Elements</summary>
		private void SetupUI()
		{
			// Browse for log file
			m_btn_log_filepath.Click += (s,a) =>
			{
				OpenLogFile(null);
			};

			// Resize the log file path text box
			m_tb_log_filepath.AutoSize = false;
			m_ts.CanOverflow = true;
			m_ts.Layout += (s,a) =>
			{
				m_tb_log_filepath.StretchToFit(250);
			};

			// Clear log
			m_btn_clear.ToolTip(m_tt, "Clear the log");
			m_btn_clear.Overflow = ToolStripItemOverflow.Never;
			m_btn_clear.Click += (s,a) =>
			{
				Clear();
			};

			// Jump to the bottom of the log
			m_btn_tail.ToolTip(m_tt, "Jump to the last log entry");
			m_btn_tail.Overflow = ToolStripItemOverflow.Never;
			m_btn_tail.Click += (s,a) =>
			{
				TailScroll();
			};

			// Line Wrap mode
			m_chk_line_wrap.Checked = LineWrap;
			m_chk_line_wrap.Overflow = ToolStripItemOverflow.Never;
			m_chk_line_wrap.CheckedChanged += (s,a) =>
			{
				LineWrap = m_chk_line_wrap.Checked;
			};
			SetLineWrap(LineWrap);

			// Set up the grid for displaying log entries
			m_view.AutoGenerateColumns         = false;
			m_view.ColumnHeadersVisible        = false;
			m_view.CellValueNeeded            += HandleCellValueNeeded;
			m_view.CellFormatting             += HandleCellFormatting;
			m_view.VisibleChanged             += DataGridView_.FitColumnsWithNoLineWrap;
			m_view.ColumnWidthChanged         += DataGridView_.FitColumnsWithNoLineWrap;
			m_view.RowHeadersWidthChanged     += DataGridView_.FitColumnsWithNoLineWrap;
			m_view.AutoSizeColumnsModeChanged += DataGridView_.FitColumnsWithNoLineWrap;
			m_view.SizeChanged                += DataGridView_.FitColumnsWithNoLineWrap;
			m_view.Scroll                     += DataGridView_.FitColumnsWithNoLineWrap;
			m_view.MouseDown                  += DataGridView_.ColumnVisibility;
			m_view.VirtualMode                 = true;
			m_view.ColumnCount                 = 1;
			m_view.RowCount                    = 0;
		}

		/// <summary>Invalidate this control and all children</summary>
		public void Invalidate(object sender, EventArgs e)
		{
			Invalidate(true);
		}

		/// <summary>Update the log file to display</summary>
		private void OpenLogFile(string filepath)
		{
			// Prompt for a log file if not given
			if (filepath == null)
			{
				using (var dlg = new OpenFileDialog { Title = "Select a log file to display" })
				{
					if (dlg.ShowDialog(this) != DialogResult.OK) return;
					filepath = dlg.FileName;
				}
			}

			// Set the log file to display
			LogFilepath = filepath;
		}

		/// <summary>Reset the error log to empty</summary>
		public void Clear()
		{
			LogEntries.Clear();
		}

		/// <summary>Add text to the log</summary>
		public void AddMessage(string text)
		{
			if (!IsHandleCreated) return;
			if (InvokeRequired)
			{
				BeginInvoke((Action<string>)AddMessage, text);
			}
			else
			{
				// Use the FPos value of the last entry so that if 'AddMessage' is mixed with
				// entries read from a log file, the read position in the log file is preserved.
				var fpos = !LogEntries.Empty() ? LogEntries.Back().FPos : 0;
				LogEntries.Add(new LogEntry(fpos, text, false));
			}
		}

		/// <summary>Use the log entry pattern to create columns</summary>
		private void CreateColumns()
		{
			// Set the number of columns based on group names in the pattern.
			// Group names should match the values in 'ColumnNames' to support non-string values
			var names = m_log_entry_pattern?.GetGroupNames().Skip(1).ToArray();
			if (names != null && names.Length != 0)
			{
				m_view.ColumnCount = names.Length;
				for (int i = 0; i != names.Length; ++i)
				{
					m_view.Columns[i].Name = names[i];
					m_view.Columns[i].HeaderText = names[i];
					m_view.Columns[i].DefaultCellStyle.WrapMode = DataGridViewTriState.False;
					m_view.Columns[i].FillWeight = FillWeights.TryGetValue(names[i], out float fw) ? fw : 1f;
				}
				m_view.ColumnHeadersVisible = true;
			}
			else
			{
				m_view.ColumnCount = 1;
				m_view.Columns[0].Name = ColumnNames.Message;
				m_view.Columns[0].HeaderText = ColumnNames.Message;
				m_view.Columns[0].DefaultCellStyle.WrapMode = DataGridViewTriState.False;
				m_view.ColumnHeadersVisible = false;
			}

			SetLineWrap(LineWrap);
		}

		/// <summary>Scroll to make the last row visible and select it (i.e. enter tail scroll mode)</summary>
		private void TailScroll()
		{
			if (m_view.RowCount == 0)
				return;

			var displayed_rows = m_view.DisplayedRowCount(false);
			var first_row = Math.Max(0, m_view.RowCount - displayed_rows);
			m_view.TryScrollToRowIndex(first_row);
			m_view.CurrentCell = m_view[m_view.CurrentCell.ColumnIndex, m_view.RowCount - 1];
		}

		/// <summary>Enable/Disable line wrap</summary>
		private void SetLineWrap(bool line_wrap)
		{
			foreach (var col in m_view.Columns.Cast<DataGridViewColumn>())
			{
				col.DefaultCellStyle.WrapMode = line_wrap && (col.Name == ColumnNames.Message || m_view.ColumnCount == 1)
					? DataGridViewTriState.True
					: DataGridViewTriState.NotSet;
			}
			m_view.AutoSizeColumnsMode = line_wrap ? DataGridViewAutoSizeColumnsMode.Fill : DataGridViewAutoSizeColumnsMode.None;
			m_chk_line_wrap.Checked = line_wrap;
			m_line_wrap = line_wrap;
		}

		/// <summary>Return the log entry for the given grid row index</summary>
		private LogEntry LEFromRowIndex(int row_index)
		{
			// Find the log entry index
			var idx = LogEntries.Count - (m_view.RowCount - row_index);
			return idx.Within(0, LogEntries.Count) ? LogEntries[idx] : null;
		}

		/// <summary>Handle cell values needed for the log view</summary>
		private void HandleCellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
		{
			// Find the log entry
			var entry = LEFromRowIndex(e.RowIndex);
			if (entry == null)
			{
				e.Value = string.Empty;
				return;
			}

			Match m;

			// Apply the log entry pattern to the log entry to get the column values
			if (LogEntryPattern != null && (m = LogEntryPattern.Match(entry.Text)).Success)
			{
				var grp = m_view.Columns[e.ColumnIndex].Name;
				var value = m.Groups[grp]?.Value ?? string.Empty;
				try
				{
					switch (grp)
					{
					default:                      e.Value = value; break;
					case ColumnNames.Tag:         e.Value = value; break;
					case ColumnNames.Level:       e.Value = Enum<ELogLevel>.Parse(value); break;
					case ColumnNames.Timestamp:   e.Value = TimeSpan.Parse(value); break;
					case ColumnNames.Message:     e.Value = value; break;
					case ColumnNames.File:        e.Value = value; break;
					case ColumnNames.Line:        e.Value = int.Parse(value); break;
					case ColumnNames.Occurrences: e.Value = int.Parse(value); break;
					}
				}
				catch (Exception)
				{
					e.Value = value;
				}
			}
			else
			{
				e.Value = e.ColumnIndex == 0 ? entry.Text : string.Empty;
			}
		}

		/// <summary>Colour cells based on log level</summary>
		private void HandleCellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
		{
			// Find the log entry index
			var idx = LogEntries.Count - (m_view.RowCount - e.RowIndex);
			if (!idx.Within(0, LogEntries.Count))
				return;

			// Format the log entry
			Format(LogEntries[idx], e);
		}

		/// <summary>Format the cell background on the log view</summary>
		public virtual void Format(LogEntry entry, DataGridViewCellFormattingEventArgs e)
		{
			foreach (var hl in Highlighting)
			{
				if (!hl.IsMatch(entry.Text))
					continue;

				e.CellStyle.BackColor = hl.BackColour;
				e.CellStyle.ForeColor = hl.ForeColour;
				break;
			}
			e.CellStyle.SelectionBackColor = e.CellStyle.BackColor.Lerp(Color.Gray, 0.5f);
			e.CellStyle.SelectionForeColor = e.CellStyle.ForeColor;

			// Raise an event to allow formatting
			if (m_view.Within(e.ColumnIndex, e.RowIndex, out DataGridViewColumn col, out DataGridViewCell cell))
			{
				var args = new FormattingEventArgs(entry, col.Name, e.Value, e.DesiredType, e.FormattingApplied, e.CellStyle);
				Formatting?.Invoke(this, args);
				e.Value = args.Value;
				e.FormattingApplied = args.FormattingApplied;
			}
		}
		public event EventHandler<FormattingEventArgs> Formatting;

		/// <summary>Handle the list of log entries changing</summary>
		private void HandleLogEntriesChanging(object sender, ListChgEventArgs<LogEntry> e)
		{
			if (!e.IsPostEvent || !e.IsDataChanged)
				return;

			// Auto tail
			var auto_tail = m_view.CurrentCell?.RowIndex == m_view.RowCount - 1 || m_view.RowCount == 0;

			// Update the row count to the number of log entries
			m_view.RowCount = Math.Min(MaxLines, LogEntries.Count);

			// Auto scroll to the last row
			if (auto_tail)
				TailScroll();

			// If a log entry was added, pop-out.
			if (e.ChangeType == ListChg.ItemAdded && DockControl?.DockContainer != null)
			{
				if (PopOutOnNewMessages)
				{
					DockControl.DockContainer.FindAndShow(this);
				}
				else
				{
					DockControl.TabColoursActive.Text = Color.Red;
					DockControl.InvalidateTab();
				}
			}

			// Prevent the LogEntries collection getting too big
			if (e.ChangeType == ListChg.ItemAdded && LogEntries.Count > 2*MaxLines)
				LogEntries.RemoveRange(0, LogEntries.Count - MaxLines);
		}

		/// <summary>Raised when the dock container is assigned/changed</summary>
		protected virtual void OnDockContainerChanged(DockContainerChangedEventArgs args)
		{}
		private void HandleDockContainerChanged(object sender, DockContainerChangedEventArgs e)
		{
			OnDockContainerChanged(e);
		}

		/// <summary>Handle this window becoming active</summary>
		private void HandleActiveChanged(object sender, EventArgs e)
		{
			if (DockControl.IsActiveContent)
			{
				DockControl.TabColoursActive.Text = Color.Black;
				DockControl.InvalidateTab();
			}
		}

		/// <summary>Handle the log file changing</summary>
		private bool HandleFileChanged(string filepath, object ctx)
		{
			try
			{
				// Open the file
				using (var file = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite|FileShare.Delete))
				{
					// Get the file length (remember this is potentially changing all the time)
					var fend = file.Seek(0, SeekOrigin.End);

					// Determine the file offset to start reading from
					var fpos = Math.Max(0, fend - MaxFileBytes);
					var le = LogEntries.LastOrDefault(x => x.FromFile);
					if (le != null) fpos = Math.Min(le.FPos, fend);

					// Read forwards from 'pos' to the end of the file
					for (; fpos != fend;)
					{
						// Read a chunk from the file
						var len = (int)Math.Min(fend - fpos, m_buf.Length);
						file.Seek(fpos, SeekOrigin.Begin);
						if (file.Read(m_buf, 0, len) != len)
							throw new Exception("Failed to read file");

						// Extract log entries from the file data
						var fstart = fpos;
						for (int s = 0, e = 0; fpos != fend; s = e)
						{
							// Log lines start with the ESC character '\u001b'
							for (; s != len && m_buf[s] != LineDelimiter; ++s) {}
							fpos = fstart + s;

							// No log lines found
							if (s == len)
								break;

							// Find the start of the next log entry
							for (e = s+1; e != len && m_buf[e] != LineDelimiter; ++e) {}

							// [s,e) is a log entry. If 'm_buf[e]' is not the delimiter then it may be a partial entry
							var entry = new LogEntry(fstart + s, Encoding.UTF8.GetString(m_buf, s+1, e-s-1), true);

							// Add to the log entries collection. Replace any partial log entry
							int i = LogEntries.Count;
							for (; i-- != 0 && (!LogEntries[i].FromFile || LogEntries[i].FPos > fpos);) {}
							if (i >= 0 && LogEntries[i].FPos == entry.FPos)
								LogEntries[i] = entry;
							else
								LogEntries.Add(entry);

							// If we haven't read to the end of the file, re-read from 's'.
							// If we have read to the end of the file, exit the loop.
							if (e == len && len != m_buf.Length)
								fpos = fend;
						}
					}
				}
			}
			catch (Exception ex)
			{
				AddMessage($"Log Error: {ex.Message}");
			}
			return true;
		}
		private byte[] m_buf = new byte[8192];

		/// <summary>A single log entry</summary>
		[DebuggerDisplay("[{FPos}] {Text}")]
		public class LogEntry
		{
			public LogEntry(long fpos, string text, bool from_file)
			{
				FPos = fpos;
				Text = text;
				FromFile = from_file;
			}

			/// <summary>Log file offset (byte index of the log entry delimiter)</summary>
			public long FPos;

			/// <summary>The text for the log entry</summary>
			public string Text;

			/// <summary>True if this log entry was read from a log file</summary>
			public bool FromFile;
		}

		/// <summary>Patterns for highlighting rows in the log</summary>
		[DebuggerDisplay("{Expr}")]
		public class HLPattern :Pattern
		{
			public HLPattern(Color bk, Color fr)
				:base()
			{
				BackColour = bk;
				ForeColour = fr;
			}
			public HLPattern(Color bk, Color fr, EPattern patn_type, string expr)
				:base(patn_type, expr)
			{
				BackColour = bk;
				ForeColour = fr;
			}
			public HLPattern(HLPattern rhs)
				:base(rhs)
		{
				BackColour = rhs.BackColour;
				ForeColour = rhs.ForeColour;
			}
			public HLPattern(XElement node)
				:base(node)
			{
				BackColour = node.Element(nameof(BackColour)).As<Color>();
				ForeColour = node.Element(nameof(ForeColour)).As<Color>();
			}
			public override XElement ToXml(XElement node)
			{
				base.ToXml(node);
				node.Add2(nameof(BackColour), BackColour, false);
				node.Add2(nameof(ForeColour), ForeColour, false);
				return node;
			}

			/// <summary>Background colour when the pattern is a match</summary>
			public Color BackColour { get; set; }

			/// <summary>Foreground colour when the pattern is a match</summary>
			public Color ForeColour { get; set; }
		}

		/// <summary>Typical column names</summary>
		public static class ColumnNames
		{
			public const string Tag         = nameof(Logger.LogEvent.Tag);
			public const string Level       = nameof(Logger.LogEvent.Level);
			public const string Timestamp   = nameof(Logger.LogEvent.Timestamp);
			public const string Message     = nameof(Logger.LogEvent.Message);
			public const string File        = nameof(Logger.LogEvent.File);
			public const string Line        = nameof(Logger.LogEvent.Line);
			public const string Occurrences = nameof(Logger.LogEvent.Occurrences);
		}

		/// <summary>Formatting event args</summary>
		public class FormattingEventArgs :EventArgs
		{
			public FormattingEventArgs(LogUI.LogEntry entry, string column_name, object value, Type desired_type, bool formatting_applied, DataGridViewCellStyle cell_style)
			{
				LogEntry          = entry;
				ColumnName        = column_name;
				DesiredType       = desired_type;
				Value             = value;
				FormattingApplied = formatting_applied;
				CellStyle         = cell_style;
			}

			/// <summary>The log entry being displayed</summary>
			public LogUI.LogEntry LogEntry { get; private set; }

			/// <summary>The name of the column that the formatting is for</summary>
			public string ColumnName { get; private set; }

			/// <summary>The Type that the grid is expecting for this cell</summary>
			public Type DesiredType { get; private set; }

			/// <summary>Get/Set the value to be displayed</summary>
			public object Value { get; set; }

			/// <summary>True if 'Value' has been converted to a string already</summary>
			public bool FormattingApplied { get; set; }

			/// <summary>Get/Set the cell style</summary>
			public DataGridViewCellStyle CellStyle { get; private set; }
		}

		#region Component Designer generated code
		private IContainer components = null;
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
			this.m_tsc = new Rylogic.Gui.WinForms.ToolStripContainer();
			this.m_view = new Rylogic.Gui.WinForms.DataGridView();
			this.m_ts = new System.Windows.Forms.ToolStrip();
			this.m_btn_log_filepath = new System.Windows.Forms.ToolStripButton();
			this.m_tb_log_filepath = new System.Windows.Forms.ToolStripTextBox();
			this.m_btn_tail = new System.Windows.Forms.ToolStripButton();
			this.m_chk_line_wrap = new System.Windows.Forms.ToolStripButton();
			this.m_il_toolbar = new System.Windows.Forms.ImageList(this.components);
			this.m_tt = new System.Windows.Forms.ToolTip(this.components);
			this.m_btn_clear = new System.Windows.Forms.ToolStripButton();
			this.m_tsc.ContentPanel.SuspendLayout();
			this.m_tsc.TopToolStripPanel.SuspendLayout();
			this.m_tsc.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_view)).BeginInit();
			this.m_ts.SuspendLayout();
			this.SuspendLayout();
			// 
			// m_tsc
			// 
			// 
			// m_tsc.ContentPanel
			// 
			this.m_tsc.ContentPanel.Controls.Add(this.m_view);
			this.m_tsc.ContentPanel.Size = new System.Drawing.Size(398, 479);
			this.m_tsc.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_tsc.Location = new System.Drawing.Point(0, 0);
			this.m_tsc.Name = "m_tsc";
			this.m_tsc.Size = new System.Drawing.Size(398, 504);
			this.m_tsc.TabIndex = 0;
			this.m_tsc.Text = "tsc";
			// 
			// m_tsc.TopToolStripPanel
			// 
			this.m_tsc.TopToolStripPanel.Controls.Add(this.m_ts);
			// 
			// m_view
			// 
			this.m_view.AllowUserToAddRows = false;
			this.m_view.AllowUserToDeleteRows = false;
			this.m_view.AllowUserToOrderColumns = true;
			this.m_view.AllowUserToResizeRows = false;
			this.m_view.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
			this.m_view.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.None;
			this.m_view.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.TopLeft;
			dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Window;
			dataGridViewCellStyle1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.ControlText;
			dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
			this.m_view.DefaultCellStyle = dataGridViewCellStyle1;
			this.m_view.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_view.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
			this.m_view.Location = new System.Drawing.Point(0, 0);
			this.m_view.Name = "m_view";
			this.m_view.RowHeadersVisible = false;
			this.m_view.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.m_view.Size = new System.Drawing.Size(398, 479);
			this.m_view.TabIndex = 0;
			this.m_view.VirtualMode = true;
			// 
			// m_ts
			// 
			this.m_ts.Dock = System.Windows.Forms.DockStyle.None;
			this.m_ts.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_btn_log_filepath,
            this.m_tb_log_filepath,
            this.m_btn_clear,
            this.m_btn_tail,
            this.m_chk_line_wrap});
			this.m_ts.Location = new System.Drawing.Point(0, 0);
			this.m_ts.Name = "m_ts";
			this.m_ts.Size = new System.Drawing.Size(398, 25);
			this.m_ts.Stretch = true;
			this.m_ts.TabIndex = 0;
			// 
			// m_btn_log_filepath
			// 
			this.m_btn_log_filepath.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.m_btn_log_filepath.Image = global::Rylogic.Gui.WinForms.Resources.folder;
			this.m_btn_log_filepath.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.m_btn_log_filepath.Name = "m_btn_log_filepath";
			this.m_btn_log_filepath.Size = new System.Drawing.Size(23, 22);
			this.m_btn_log_filepath.Text = "Browse";
			this.m_btn_log_filepath.ToolTipText = "Browse for the log file to display";
			// 
			// m_tb_log_filepath
			// 
			this.m_tb_log_filepath.Name = "m_tb_log_filepath";
			this.m_tb_log_filepath.ReadOnly = true;
			this.m_tb_log_filepath.Size = new System.Drawing.Size(250, 25);
			// 
			// m_btn_tail
			// 
			this.m_btn_tail.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.m_btn_tail.Image = Resources.bottom;
			this.m_btn_tail.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.m_btn_tail.Name = "m_btn_tail";
			this.m_btn_tail.Size = new System.Drawing.Size(23, 22);
			this.m_btn_tail.Text = "Tail";
			// 
			// m_chk_line_wrap
			// 
			this.m_chk_line_wrap.CheckOnClick = true;
			this.m_chk_line_wrap.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.m_chk_line_wrap.Image = Resources.line_wrap;
			this.m_chk_line_wrap.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.m_chk_line_wrap.Name = "m_chk_line_wrap";
			this.m_chk_line_wrap.Size = new System.Drawing.Size(23, 22);
			this.m_chk_line_wrap.Text = "Line Wrap";
			// 
			// m_il_toolbar
			// 
			this.m_il_toolbar.ColorDepth = System.Windows.Forms.ColorDepth.Depth32Bit;
			this.m_il_toolbar.ImageSize = new System.Drawing.Size(24, 24);
			this.m_il_toolbar.TransparentColor = System.Drawing.Color.Transparent;
			// 
			// m_btn_clear
			// 
			this.m_btn_clear.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.m_btn_clear.Image = Resources.check_reject;
			this.m_btn_clear.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.m_btn_clear.Name = "m_btn_clear";
			this.m_btn_clear.Size = new System.Drawing.Size(23, 22);
			this.m_btn_clear.Text = "Clear";
			this.m_btn_clear.ToolTipText = "Clear Log";
			// 
			// LogUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.m_tsc);
			this.Name = "LogUI";
			this.Size = new System.Drawing.Size(398, 504);
			this.m_tsc.ContentPanel.ResumeLayout(false);
			this.m_tsc.TopToolStripPanel.ResumeLayout(false);
			this.m_tsc.TopToolStripPanel.PerformLayout();
			this.m_tsc.ResumeLayout(false);
			this.m_tsc.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_view)).EndInit();
			this.m_ts.ResumeLayout(false);
			this.m_ts.PerformLayout();
			this.ResumeLayout(false);

		}
		#endregion
	}
}
