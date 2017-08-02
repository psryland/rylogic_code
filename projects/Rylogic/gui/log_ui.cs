using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml.Linq;
using pr.common;
using pr.container;
using pr.extn;
using pr.util;

namespace pr.gui
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

		private const int FilePollPeriod = 100;

		#region UI elements
		private ToolStripContainer m_tsc;
		private ImageList m_il_toolbar;
		private ToolStrip m_ts;
		private ToolStripButton m_btn_log_filepath;
		private ToolStripTextBox m_tb_log_filepath;
		private DataGridView m_view;
		private FileWatch m_watch;
		#endregion

		public LogUI()
			:this("Log","Log")
		{}
		public LogUI(string title, string persist_name)
		{
			InitializeComponent();
			m_watch = new FileWatch{ PollPeriod = FilePollPeriod };

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

			// The log entry delimiter
			LineDelimiter = Log.EntryDelimiter;

			// Limit the number of log entries to display
			MaxLines = 500;
			MaxFileBytes = 2 * 1024 * 1024;

			SetupUI();
			UpdateUI();
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
					m_ts.Visible = false;
				}
				m_log_filepath = value;
				if (m_log_filepath != null)
				{
					m_ts.Visible = true;
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

				// Set the number of columns based on group names in the pattern
				var names = m_log_entry_pattern?.GetGroupNames().Skip(1).ToArray();
				if (names != null && names.Length != 0)
				{
					m_view.ColumnCount = names.Length;
					for (int i = 0; i != names.Length; ++i)
					{
						m_view.Columns[i].HeaderText = names[i];
						m_view.Columns[i].FillWeight =
							names[i] == "Level"     ? 0.3f :
							names[i] == "Timestamp" ? 0.6f :
							names[i] == "Message"   ? 5f :
							1f;
					}

					m_view.ColumnHeadersVisible = true;
				}
				else
				{
					m_view.ColumnCount = 1;
					m_view.ColumnHeadersVisible = false;
				}
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
					m_watch.PollPeriod = FilePollPeriod;
				}
				m_freeze = value;
				if (m_freeze)
				{
					m_watch.PollPeriod = 0;
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
			m_ts.Layout += (s,a) =>
			{
				m_tb_log_filepath.StretchToFit(250);
			};

			// Set up the grid for displaying log entries
			m_view.AutoGenerateColumns = false;
			m_view.ColumnHeadersVisible = false;
			m_view.CellValueNeeded += HandleCellValueNeeded;
			m_view.CellFormatting += HandleCellFormatting;
			m_view.MouseDown += DataGridView_.ColumnVisibility;
			m_view.VirtualMode = true;
			m_view.ColumnCount = 1;
			m_view.RowCount = 0;
		}

		/// <summary>Update the state of UI Elements</summary>
		private void UpdateUI(object sender = null, EventArgs args = null)
		{
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
			// Use the FPos value of the last entry so that if 'AddMessage' is mixed with
			// entries read from a log file, the read position in the log file is preserved.
			var fpos = !LogEntries.Empty() ? LogEntries.Back().FPos : 0;
			LogEntries.Add(new LogEntry(fpos, text, false));
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
		}

		/// <summary>Handle cell values needed for the log view</summary>
		private void HandleCellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
		{
			// Find the log entry index
			var idx = LogEntries.Count - (m_view.RowCount - e.RowIndex);
			if (!idx.Within(0, LogEntries.Count))
			{
				e.Value = string.Empty;
				return;
			}

			Match m;
			var entry = LogEntries[idx];

			// Apply the log entry pattern to the log entry to get the column values
			if (LogEntryPattern != null && (m = LogEntryPattern.Match(entry.Text)).Success)
			{
				var grp = m_view.Columns[e.ColumnIndex].HeaderText;
				e.Value = m.Groups[grp]?.Value ?? string.Empty;
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

		/// <summary>Handle the list of log entries changing</summary>
		private void HandleLogEntriesChanging(object sender, ListChgEventArgs<LogEntry> e)
		{
			if (!e.IsPostEvent || !e.IsDataChanged)
				return;

			// Auto tail
			var auto_tail = m_view.CurrentCell?.RowIndex == m_view.RowCount - 1;

			// Update the row count to the number of log entries
			m_view.RowCount = Math.Min(MaxLines, LogEntries.Count);

			// Auto scroll to the last row
			if (auto_tail && m_view.RowCount != 0)
			{
				var displayed_rows = m_view.DisplayedRowCount(false);
				var first_row = Math.Max(0, m_view.RowCount - displayed_rows);
				m_view.TryScrollToRowIndex(first_row);
				m_view.CurrentCell = m_view[m_view.CurrentCell.ColumnIndex, m_view.RowCount - 1];
			}

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
				AddMessage("Log Error: {0}".Fmt(ex.Message));
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

		#region Component Designer generated code
		private IContainer components = null;
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
			this.m_tsc = new pr.gui.ToolStripContainer();
			this.m_view = new pr.gui.DataGridView();
			this.m_ts = new System.Windows.Forms.ToolStrip();
			this.m_btn_log_filepath = new System.Windows.Forms.ToolStripButton();
			this.m_tb_log_filepath = new System.Windows.Forms.ToolStripTextBox();
			this.m_il_toolbar = new System.Windows.Forms.ImageList(this.components);
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
			this.m_view.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
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
            this.m_tb_log_filepath});
			this.m_ts.Location = new System.Drawing.Point(0, 0);
			this.m_ts.Name = "m_ts";
			this.m_ts.Size = new System.Drawing.Size(398, 25);
			this.m_ts.Stretch = true;
			this.m_ts.TabIndex = 0;
			// 
			// m_btn_log_filepath
			// 
			this.m_btn_log_filepath.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.m_btn_log_filepath.Image = global::pr.Resources.folder;
			this.m_btn_log_filepath.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.m_btn_log_filepath.Name = "m_btn_log_filepath";
			this.m_btn_log_filepath.Size = new System.Drawing.Size(23, 22);
			this.m_btn_log_filepath.Text = "toolStripButton1";
			// 
			// m_tb_log_filepath
			// 
			this.m_tb_log_filepath.Name = "m_tb_log_filepath";
			this.m_tb_log_filepath.ReadOnly = true;
			this.m_tb_log_filepath.Size = new System.Drawing.Size(250, 25);
			// 
			// m_il_toolbar
			// 
			this.m_il_toolbar.ColorDepth = System.Windows.Forms.ColorDepth.Depth32Bit;
			this.m_il_toolbar.ImageSize = new System.Drawing.Size(24, 24);
			this.m_il_toolbar.TransparentColor = System.Drawing.Color.Transparent;
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

#if false


		/// <summary>Copy data to the 'UpdateLog' thread</summary>
		private LoggerProps LogProps
		{
			get
			{
				return new LoggerProps
				{
					Filepath     = LogFilepath,
					Pattern      = LogEntryPattern,
					LineDelim    = LineDelimiter,
					Level        = ELogLevel.Debug,
					MaxLines     = 500,
					MaxFileBytes = 2*1024*1024,
				};
			}
		}

		/// <summary>Data to copy between GUI and UpdateLog thread</summary>
		private struct LoggerProps
		{
			public string Filepath;
			public Regex Pattern;
			public char LineDelim;
			public ELogLevel Level;
			public int MaxLines;
			public int MaxFileBytes;
		}

		/// <summary>Enable/Disable the worker thread that displays the log file contents</summary>
		public bool UpdateLogViewThreadActive
		{
			get { return m_thread != null; }
			private set
			{
				if (UpdateLogViewThreadActive == value) return;
				if (m_thread != null)
				{
					m_thread_exit = true;
					lock (m_lock) Monitor.Pulse(m_lock);
					if (m_thread.IsAlive)
						m_thread.Join();
				}
				m_thread = value ? new Thread(new ThreadStart(UpdateLogViewThreadEntryPoint)) : null;
				if (m_thread != null)
				{
					m_thread_exit = false;
					m_thread.Start();
				}
			}
		}
		private void UpdateLogViewThreadEntryPoint()
		{
			var buf = new byte[8192];
			var sb = new StringBuilder();
			var no_remainder = new byte[0];
			var cells = new Sci.BackFillCellBuf();

			// Limit how often we spam the UI thread with error messages
			//var time_of_last_msg = 0;
			//var max_ui_message_rate = 2000;

			LoggerProps props;
			for (;!m_thread_exit;)
			{
				// Sleep until exit, or the cell buffer is available again, or update required
				lock (m_lock)
				{
					if (m_thread_exit) return;
					if (cells == null)       { Monitor.Wait(m_lock); continue; }
					if (!m_refresh_log_view) { Monitor.Wait(m_lock); continue; }
					props = LogProps;
					m_refresh_log_view = false;
				}

				try
				{
					// Ignore log files that don't exist
					if (!File.Exists(props.Filepath))
						continue;

					// Open the file
					using (var file = new FileStream(props.Filepath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite|FileShare.Delete))
					{
						// Get the file length (remember this is changing all the time)
						var fend = file.Seek(0, SeekOrigin.End);
						long pos = fend;
						cells.Length = 0;

						// Read from the end of the file backwards, extracting log lines
						var remainder = no_remainder;
						for (var line_count = 0; line_count < props.MaxLines && pos != 0 && fend - pos < props.MaxFileBytes;)
						{
							// If the log file format is invalid, we mightn't find any lines
							// and the "remainder" will be the entire buffer. In this case abort.
							if (remainder.Length == buf.Length)
								throw new Exception("Log file format is invalid, no log entries detected within {0} bytes of data".Fmt(remainder.Length));

							// Read a chunk from the end of the file
							var len = (int)Math.Min(pos, buf.Length - remainder.Length);
							file.Position = pos -= len;
							if (file.Read(buf, 0, len) != len)
								throw new Exception("Failed to read file");

							// Append the remainder from last time
							Array.Copy(remainder, 0, buf, len, remainder.Length);
							len += remainder.Length;
							remainder = no_remainder;

							// Scan backwards finding log lines.
							for (int e = len, s = e; e != 0; e = s, --s)
							{
								// Log lines start with the ESC character \u001b
								for (; s-- != 0 && buf[s] != props.LineDelim;) {}
								if (s == -1) // No log lines found
								{
									// If the buffer contains a partial line, save it as the remainder and read the next block
									remainder = buf.Dup(0, e);
									break;
								}

								// The first characters after the ESC character is the log level
								// Skip lines whose log level is below the threshold
								{
									var lvl = buf[s+1] - '0';
									if (lvl < (int)props.Level)
										continue;
								}

								sb.Length = 0;
								byte style = 0;

								// Add valid lines to the cell buffer
								var line = Encoding.ASCII.GetString(buf, s, e - s);
								var match = props.Pattern.Match(line);
								try
								{
									if (!match.Success || match.Groups.Count != 7)
										throw new Exception("Format mismatch");

									//var line    = match.Groups[0     ].Value;
									var lvl     = match.Groups["lvl"   ]?.Value ?? "0";
									var time    = match.Groups["time"  ]?.Value ?? string.Empty;
									var name    = match.Groups["name"  ]?.Value ?? string.Empty;
									var msg     = match.Groups["msg"   ]?.Value ?? string.Empty;
									var except  = match.Groups["except"]?.Value ?? string.Empty;
									var repeats = match.Groups["count" ]?.Value ?? string.Empty;

									sb.Append(time).Append(" | ").Append(name).Append(" | ").Append(msg);
									if (except.HasValue()) sb.Append(" - ").Append(except);
									if (repeats.HasValue() && repeats != "1") sb.Append("[").Append(repeats).Append("]");
									sb.AppendLine();

									style = (byte)int.Parse(lvl);
								}
								catch (Exception ex)
								{
									sb.Length = 0;
									sb.Append("<log error - ").Append(ex.Message).Append(">").AppendLine();

									//style = (byte)RexBionics.RexLink.LogLevel.Application;
								}

								// Fill the cells buffer
								cells.Add(sb.ToString(), style);
								++line_count;
							}
						}

						// Once done, BeginInvoke to update the view.
						// Pass the reference to 'cells' to the GUI thread. When it's done it will restore
						// the reference back to its original value and signal the condition variable.
						var c = cells; cells = null;
						this.BeginInvoke(() =>
						{
							m_text.UpdateView(c);
							lock (m_lock)
							{
								cells = c; // Restore the reference
								Monitor.Pulse(m_lock);
							}
						});
					}
				}
				catch (Exception ex)
				{
		//			// Limit how often we spam the UI thread with error messages
		//			var now = Environment.TickCount;
		//			if (now - time_of_last_msg > max_ui_message_rate)
		//			{
		//				time_of_last_msg = now;
		//				var msg = "Failed to update log view. {0}".Fmt(ex.MessageFull());
		//				this.BeginInvoke(() =>
		//					{
		//						Status.SetStatusMessage(msg:msg, fr_color:Color.Red, display_time:TimeSpan.FromSeconds(5));
		//					});
		//			}
				}
			}
		}
		private bool m_thread_exit;
		private Thread m_thread;
		private object m_lock;


#endif
