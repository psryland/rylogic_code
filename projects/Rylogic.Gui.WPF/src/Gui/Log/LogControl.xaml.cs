using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Linq;
using Microsoft.Win32;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Gfx;
using Rylogic.Gui.WPF.DockContainerDetail;
using Rylogic.Utility;

namespace Rylogic.Gui.WPF
{
	public partial class LogControl : UserControl, IDockable, IDisposable, INotifyPropertyChanged
	{
		// Notes:
		//   This component can be used in two ways, one is as a simple text
		//   panel for displaying log messages, the second is as a view of a
		//   log file.
		//   1)
		//    - Add an instance of the control
		//    - Call AddMessage()
		//   2)
		//    - Add an instance of the control
		//    - Set the log filepath

		private const int FilePollPeriodMS = 100;
		private readonly int m_main_thread_id;

		public LogControl()
		{
			InitializeComponent();
			m_main_thread_id = Thread.CurrentThread.ManagedThreadId;
			m_view.MouseRightButtonUp += DataGrid_.ColumnVisibility;

			// Support for dock container controls
			DockControl = new DockControl(this, "log")
			{
				TabText = "Log",
				DefaultDockLocation = new DockContainer.DockLocation(auto_hide: EDockSite.Right),
			};

			// When docked in an auto-hide panel, pop out on new messages
			PopOutOnNewMessages = true;

			// Show log filepath related UI by default
			ShowLogFilepath = true;

			// Line wrap default
			LineWrap = true;

			// Hide diagnostics by default
			FilterLevel = ELogLevel.Info;

			// A buffer of the log entries.
			// This is populated by calls to AddMessage or from the log file.
			LogEntries = new ObservableCollection<LogEntry>();
			LogEntriesView = new ListCollectionView(LogEntries) { Filter = obj => obj is LogEntry le && le.Level >= FilterLevel };

			// Define a line in the log
			LogEntryPattern = null;
			//new Regex(@"^(?<File>.*?)\|(?<Level>.*?)\|(?<Timestamp>.*?)\|(?<Message>.*)\n"
			//	,RegexOptions.Singleline|RegexOptions.Multiline|RegexOptions.CultureInvariant|RegexOptions.Compiled);
			//new Regex(@"^\u001b(?<lvl>\d),(?<time>.*?),(?<name>.*?),""(?<msg>.*?)"",""(?<except>.*?)"",(?<count>\d+)\s*\n"
			//	,RegexOptions.Singleline|RegexOptions.Multiline|RegexOptions.CultureInvariant|RegexOptions.Compiled);

			// Highlighting patterns
			Highlighting = new ObservableCollection<HLPattern>();
			Highlighting.Add(new HLPattern(Color_.From("#FCF"), Color_.From("#C0C"), EPattern.RegularExpression, ".*fatal.*") { IgnoreCase = true });
			Highlighting.Add(new HLPattern(Color_.From("#FDD"), Color_.From("#F00"), EPattern.RegularExpression, ".*error.*") { IgnoreCase = true });
			Highlighting.Add(new HLPattern(Colors.Transparent, Color_.From("#E70"), EPattern.RegularExpression, ".*warn.*") { IgnoreCase = true });
			Highlighting.Add(new HLPattern(Colors.Transparent, Color_.From("#888"), EPattern.RegularExpression, ".*debug.*") { IgnoreCase = true });

			// The log entry delimiter
			LineDelimiter = Log.EntryDelimiter;

			// Limit the number of log entries to display
			MaxLines = 500;
			MaxFileBytes = 2 * 1024 * 1024;

			// Commands
			BrowseForLogFile = Command.Create(this, BrowseForLogFileInternal);
			ClearLog = Command.Create(this, ClearLogInternal);
			ToggleTailScroll = Command.Create(this, ToggleTailScrollInternal);
			ToggleLineWrap = Command.Create(this, ToggleLineWrapInternal);
			CopySelectedRowsToClipboard = Command.Create(this, CopySelectedRowsToClipboardInternal);
			CopySelectedMessagesToClipboard = Command.Create(this, CopySelectedMessagesToClipboardInternal);

			DataContext = this;
		}
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool _)
		{
			LogEntries = null!;
			LogFilepath = null;
			Highlighting = null!;
			DockControl = null!;
		}
		protected override void OnPreviewKeyDown(KeyEventArgs e)
		{
			base.OnPreviewKeyDown(e);
			if (e.Key.ToKeyCode() == EKeyCodes.End && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
			{
				TailScroll = true;
				e.Handled = true;
			}
		}

		/// <summary>Provides support for the DockContainer</summary>
		public DockControl DockControl
		{
			get => m_dock_control;
			private set
			{
				if (m_dock_control == value) return;
				if (m_dock_control != null)
				{
					m_dock_control.ActiveChanged -= HandleActiveChanged;
					m_dock_control.DockContainerChanged -= HandleDockContainerChanged;
					m_dock_control.SavingLayout -= HandleSavingLayout;
					m_dock_control.LoadingLayout -= HandleLoadingLayout;
					Util.Dispose(ref m_dock_control!);
				}
				m_dock_control = value;
				if (m_dock_control != null)
				{
					m_dock_control.LoadingLayout += HandleLoadingLayout;
					m_dock_control.SavingLayout += HandleSavingLayout;
					m_dock_control.DockContainerChanged += HandleDockContainerChanged;
					m_dock_control.ActiveChanged += HandleActiveChanged;
				}

				// Handlers
				void HandleLoadingLayout(object sender, DockContainerLoadingLayoutEventArgs e)
				{
					LoadSettings(e.UserData);
				}
				void HandleSavingLayout(object sender, DockContainerSavingLayoutEventArgs e)
				{
					SaveSettings(e.Node);
				}
				void HandleActiveChanged(object sender, EventArgs e)
				{
					if (DockControl.IsActiveContent)
					{
						//DockControl.TabColoursActive.Text = Color.Black;
						//DockControl.InvalidateTab();
					}
				}
				void HandleDockContainerChanged(object sender, DockContainerChangedEventArgs e)
				{
					OnDockContainerChanged(e);
				}
			}
		}
		private DockControl m_dock_control = null!;
		protected virtual void OnDockContainerChanged(DockContainerChangedEventArgs args)
		{ }

		/// <summary>Get/Set the log file to display. Setting a filepath sets the control to the 'file tail' mode</summary>
		public string? LogFilepath
		{
			get => m_log_filepath;
			set
			{
				// Allow setting to the same value
				if (m_log_filepath != null)
				{
					m_watch!.Remove(m_log_filepath);
					Util.Dispose(ref m_watch);
				}
				m_log_filepath = value;
				if (m_log_filepath != null)
				{
					m_watch = new FileWatch { PollPeriod = TimeSpan.FromMilliseconds(FilePollPeriodMS) };
					m_watch.Add(m_log_filepath, HandleFileChanged);
					HandleFileChanged(m_log_filepath, null);
				}
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LogFilepath)));

				// Handlers
				bool HandleFileChanged(string filepath, object? ctx)
				{
					try
					{
						// Open the file
						using var file = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);

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
								for (; s != len && m_buf[s] != LineDelimiter; ++s) { }
								fpos = fstart + s;

								// No log lines found
								if (s == len)
									break;

								// Find the start of the next log entry
								for (e = s + 1; e != len && m_buf[e] != LineDelimiter; ++e) { }

								// [s,e) is a log entry. If 'm_buf[e]' is not the delimiter then it may be a partial entry
								var entry = new LogEntry(this, fstart + s, Encoding.UTF8.GetString(m_buf, s + 1, e - s - 1), true);

								// Add to the log entries collection. Replace any partial log entry
								int i = LogEntries.Count;
								for (; i-- != 0 && (!LogEntries[i].FromFile || LogEntries[i].FPos > fpos);) { }
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
					catch (Exception ex)
					{
						AddMessage($"Log Error: {ex.Message}");
					}
					return true;
				}
			}
		}
		private string? m_log_filepath;
		private byte[] m_buf = new byte[8192];
		private FileWatch? m_watch;

		/// <summary>If docked in a doc container, pop-out when new messages are added to the log</summary>
		public bool PopOutOnNewMessages
		{
			get { return (bool)GetValue(PopOutOnNewMessagesProperty); }
			set { SetValue(PopOutOnNewMessagesProperty, value); }
		}
		public static readonly DependencyProperty PopOutOnNewMessagesProperty = Gui_.DPRegister<LogControl>(nameof(PopOutOnNewMessages));

		/// <summary>Show UI parts related to log files</summary>
		public bool ShowLogFilepath
		{
			get { return (bool)GetValue(ShowLogFilepathProperty); }
			set { SetValue(ShowLogFilepathProperty, value); }
		}
		public static readonly DependencyProperty ShowLogFilepathProperty = Gui_.DPRegister<LogControl>(nameof(ShowLogFilepath));

		/// <summary>Line wrap mode</summary>
		public bool LineWrap
		{
			get { return (bool)GetValue(LineWrapProperty); }
			set { SetValue(LineWrapProperty, value); }
		}
		private void LineWrap_Changed(bool new_value)
		{
			CreateColumns();
		}
		public static readonly DependencyProperty LineWrapProperty = Gui_.DPRegister<LogControl>(nameof(LineWrap));

		/// <summary>Scroll the view to the last entry</summary>
		public bool TailScroll
		{
			get { return (bool)GetValue(TailScrollProperty); }
			set { SetValue(TailScrollProperty, value); }
		}
		private void TailScroll_Changed(bool new_value)
		{
			if (new_value)
				ScrollToEnd();
		}
		public static readonly DependencyProperty TailScrollProperty = Gui_.DPRegister<LogControl>(nameof(TailScroll));

		/// <summary>The minimum log level to display</summary>
		public ELogLevel FilterLevel
			{
				get { return (ELogLevel)GetValue(FilterLevelProperty); }
				set { SetValue(FilterLevelProperty, value); }
			}
		private void FilterLevel_Changed()
		{
			LogEntriesView?.Refresh();
		}
		public static readonly DependencyProperty FilterLevelProperty = Gui_.DPRegister<LogControl>(nameof(FilterLevel));

		/// <summary>A regex expression for extracting lines from the log file</summary>
		public Regex? LogEntryPattern
		{
			get => m_log_entry_pattern;
			set
			{
				if (m_log_entry_pattern == value) return;
				m_log_entry_pattern = value;
				CreateColumns();
			}
		}
		private Regex? m_log_entry_pattern;

		/// <summary>A special character used to mark the start of a log entry. Must be a 1-byte UTF8 character</summary>
		public char LineDelimiter { get; set; }

		/// <summary>The maximum number of lines to show in the log</summary>
		public int MaxLines { get; set; }

		/// <summary>The maximum number of bytes to read from the log file</summary>
		public int MaxFileBytes { get; set; }
		
		/// <summary>Get/Set watching of the log file for changes</summary>
		public bool Freeze
		{
			get => m_freeze;
			set
			{
				if (m_freeze == value) return;
				if (m_freeze)
				{
					if (m_watch != null)
						m_watch.PollPeriod = TimeSpan.FromMilliseconds(FilePollPeriodMS);
				}
				m_freeze = value;
				if (m_freeze)
				{
					if (m_watch != null)
						m_watch.PollPeriod = TimeSpan.Zero;
				}
			}
		}
		private bool m_freeze;

		/// <summary>A buffer of the log entries</summary>
		public ObservableCollection<LogEntry> LogEntries
		{
			get => m_log_entries;
			private set
			{
				if (m_log_entries == value) return;
				if (m_log_entries != null)
				{
					m_log_entries.CollectionChanged -= HandleLogEntriesChanged;
				}
				m_log_entries = value;
				if (m_log_entries != null)
				{
					m_log_entries.CollectionChanged += HandleLogEntriesChanged;
				}

				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LogEntries)));
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LogEntriesView)));

				// Handlers
				void HandleLogEntriesChanged(object sender, NotifyCollectionChangedEventArgs e)
				{
					if (e.Action != NotifyCollectionChangedAction.Add)
						return;

					// If a log entry was added, pop-out.
					if (DockControl?.DockContainer != null)
					{
						if (PopOutOnNewMessages)
							DockControl.DockContainer.FindAndShow(this);
						else if (DockControl.TabState != ETabState.Active)
							DockControl.TabState = ETabState.Flashing;
					}

					// Prevent the LogEntries collection getting too big.
					// Defer because we can't edit the collection in this handler.
					if (LogEntries.Count > MaxLines)
						Dispatcher.BeginInvoke(() =>
						{
							if (LogEntries == null) return;
							LogEntries.RemoveRange(0, LogEntries.Count - MaxLines);
						});

					// Auto scroll to the last row
					// Have to do this outside of the event handler or we get an exception
					// about the "ItemsSource being inconsistent with the ItemsControl"
					if (TailScroll)
						Dispatcher.BeginInvoke(ScrollToEnd);
				}
			}
		}
		private ObservableCollection<LogEntry> m_log_entries = null!;
		public ICollectionView LogEntriesView { get; }

		// Trigger a refresh
		private void SignalRefresh()
		{
			if (m_log_entry_view_refresh_pending) return;
			m_log_entry_view_refresh_pending = true;
			Dispatcher.BeginInvoke(() =>
			{
				LogEntriesView?.Refresh();
				m_log_entry_view_refresh_pending = false;
			});
		}
		private bool m_log_entry_view_refresh_pending;

		/// <summary>Highlighting patterns (in priority order)</summary>
		public ObservableCollection<HLPattern> Highlighting
		{
			get => m_highlighting;
			set
			{
				if (m_highlighting == value) return;
				if (m_highlighting != null)
				{
					m_highlighting.CollectionChanged -= HandleCollectionChanged;
				}
				m_highlighting = value;
				if (m_highlighting != null)
				{
					m_highlighting.CollectionChanged += HandleCollectionChanged;
				}

				// Handler
				void HandleCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
				{
					++m_highlighting_issue;
					SignalRefresh();
				}
			}
		}
		public ObservableCollection<HLPattern> m_highlighting = null!;
		private int m_highlighting_issue;

		/// <summary>Access to the columns of the log grid</summary>
		public IEnumerable<DataGridColumn> Columns => m_view.Columns;

		/// <summary>Use the log entry pattern to create columns</summary>
		private void CreateColumns()
		{
			// Element style for the rows of the log grid
			var element_style = (Style)FindResource("LogEntryStyle");

			// Set the number of columns based on group names in the pattern.
			// Group names should match the values in 'ColumnNames' to support non-string values
			var names = LogEntryPattern?.GetGroupNames().Skip(1).ToArray();
			if (names != null && names.Length != 0)
			{
				// Fill weights per column type
				var fill_weights = new Dictionary<string, DataGridLength>
				{
					{ColumnNames.Tag        , new DataGridLength(1.00, DataGridLengthUnitType.SizeToCells)},
					{ColumnNames.Level      , new DataGridLength(1.00, DataGridLengthUnitType.SizeToCells)},
					{ColumnNames.Timestamp  , new DataGridLength(1.00, DataGridLengthUnitType.SizeToCells)},
					{ColumnNames.Message    , new DataGridLength(3.00, LineWrap ? DataGridLengthUnitType.Star : DataGridLengthUnitType.Auto)},
					{ColumnNames.File       , new DataGridLength(2.00, LineWrap ? DataGridLengthUnitType.Star : DataGridLengthUnitType.Auto)},
					{ColumnNames.Line       , new DataGridLength(1.00, DataGridLengthUnitType.SizeToCells)},
					{ColumnNames.Occurrences, new DataGridLength(1.00, DataGridLengthUnitType.SizeToCells)},
				};

				// Create a column for each capture group in the pattern
				m_view.Columns.Clear();
				m_view.HeadersVisibility = DataGridHeadersVisibility.Column;
				m_view.HorizontalScrollBarVisibility = LineWrap ? ScrollBarVisibility.Disabled : ScrollBarVisibility.Auto;
				for (int i = 0; i != names.Length; ++i)
				{
					m_view.Columns.Add2(new DataGridTextColumn
					{
						Header = names[i],
						Binding = new Binding(names[i]) { Mode = BindingMode.OneWay },
						ElementStyle = element_style,
						Width = fill_weights.TryGetValue(names[i], out var fw) ? fw : new DataGridLength()
					});
				}
			}
			else
			{
				m_view.Columns.Clear();
				m_view.HeadersVisibility = DataGridHeadersVisibility.None;
				m_view.Columns.Add2(new DataGridTextColumn
				{
					Header = ColumnNames.Message,
					Binding = new Binding(nameof(LogEntry.Text)) { Mode = BindingMode.OneWay },
					ElementStyle = element_style,
					Width = LineWrap
						? new DataGridLength(1.0, DataGridLengthUnitType.Star)
						: new DataGridLength(1.0, DataGridLengthUnitType.Auto),
				});
			}
		}

		/// <summary>Update the log file to display</summary>
		private void OpenLogFile(string? filepath)
		{
			// Prompt for a log file if not given
			if (filepath == null)
			{
				var dlg = new OpenFileDialog { Title = "Select a log file to display" };
				if (dlg.ShowDialog(Window.GetWindow(this)) != true)
					return;

				filepath = dlg.FileName;
			}

			// Set the log file to display
			LogFilepath = filepath;
		}

		/// <summary>Reset the error log to empty</summary>
		public void Clear()
		{
			LogEntries.Clear();
		}

		/// <summary>Add text to the log. Can be called from any thread</summary>
		public void AddMessage(string text)
		{
			if (Thread.CurrentThread.ManagedThreadId != m_main_thread_id)
			{
				Debug.Assert(Dispatcher.Thread.ManagedThreadId == m_main_thread_id);
				Dispatcher.BeginInvoke(() =>
				{
					AddMessage(text);
				});
			}
			else
			{
				// Use the FPos value of the last entry so that if 'AddMessage' is mixed with
				// entries read from a log file, the read position in the log file is preserved.
				var fpos = !LogEntries.Empty() ? LogEntries.Back().FPos : 0;
				LogEntries.Add(new LogEntry(this, fpos, text, false));
			}
		}

		/// <summary>Make the last log entry the selected one</summary>
		public void ScrollToEnd()
		{
			LogEntriesView.MoveCurrentToLast();

			var last = LogEntriesView.CurrentItem;
			if (last != null)
				m_view.ScrollIntoView(last);
		}

		/// <summary>Load/Save settings</summary>
		public void LoadSettings(XElement? node)
		{
			// Allow null to make first-run scenarios easier
			if (node == null)
				return;

			FilterLevel = node.Element(nameof(FilterLevel)).As(FilterLevel);

			// Build a settings map from the column settings
			var column_settings = node.Elements("Columns", "Column")
				.Select(x => x.As<string>().Split(','))
				.Where(x => x.Length == 4)
				.Select(x => new
				{
					Header = x[0],
					Idx = int.Parse(x[1]),
					Width = DataGrid_.ParseDataGridLength(x[2]),
					Visibility = Enum<Visibility>.Parse(x[3]),
				})
				.ToList();

			// Apply the settings to the columns
			foreach (var col in Columns)
			{
				if (!(col.Header is string header)) continue;
				var settings = column_settings.FirstOrDefault(x => x.Header == header);
				if (settings == null) continue;
				col.DisplayIndex = settings.Idx;
				col.Width = settings.Width;
				col.Visibility = settings.Visibility;
			}
		}
		public XElement SaveSettings(XElement? node = null)
		{
			node ??= new XElement("Log");
			node.Add2(nameof(FilterLevel), FilterLevel, false);

			// Only save column info if the layout is valid
			var columns_node = node.Add2(new XElement("Columns"));
			if (IsArrangeValid)
			{
				foreach (var col in Columns)
				{
					if (!(col.Header is string header)) continue;
					columns_node.Add2("Column", $"{header},{col.DisplayIndex},{col.Width},{col.Visibility}", false);
				}
			}
			return node;
		}

		/// <summary>Browse for a log file to display</summary>
		public Command BrowseForLogFile { get; }
		private void BrowseForLogFileInternal()
		{
			OpenLogFile(null);
		}

		/// <summary>Clear the error log display (not the file)</summary>
		public Command ClearLog { get; }
		private void ClearLogInternal()
		{
			Clear();
		}

		/// <summary>Toggle scrolling to the last log entry</summary>
		public Command ToggleTailScroll { get; }
		private void ToggleTailScrollInternal()
		{
			TailScroll = !TailScroll;
		}

		/// <summary>Toggle line wrapping mode</summary>
		public Command ToggleLineWrap { get; }
		private void ToggleLineWrapInternal()
		{
			LineWrap = !LineWrap;
		}

		/// <summary>Copy the selected rows to the clipboard</summary>
		public Command CopySelectedRowsToClipboard { get; }
		private void CopySelectedRowsToClipboardInternal()
		{
			var sb = new StringBuilder();
			foreach (var le in m_view.SelectedItems.Cast<LogEntry>()) sb.Append(le.Text);
			try { Clipboard.SetData(DataFormats.Text, sb.ToString()); } catch { }
		}

		/// <summary></summary>
		public Command CopySelectedMessagesToClipboard { get; }
		private void CopySelectedMessagesToClipboardInternal()
		{
			var sb = new StringBuilder();
			foreach (var le in m_view.SelectedItems.Cast<LogEntry>()) sb.Append(le.Message);
			try { Clipboard.SetData(DataFormats.Text, sb.ToString()); } catch { }
		}

		/// <summary>Switch out of tail scroll mode when a row other than the last is selected</summary>
		private void HandleSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (TailScroll && LogEntriesView.CurrentPosition != LogEntriesView.Count() - 1)
				TailScroll = false;
		}

		/// <summary></summary>
		public event PropertyChangedEventHandler? PropertyChanged;

		/// <summary>A single log entry</summary>
		[DebuggerDisplay("[{FPos}] {Text}")]
		public class LogEntry
		{
			private readonly LogControl m_this;
			public LogEntry(LogControl @this, long fpos, string text, bool from_file)
			{
				m_this = @this;
				FPos = fpos;
				Text = text;
				FromFile = from_file;
			}

			/// <summary>Log file offset (byte index of the log entry delimiter)</summary>
			public long FPos { get; }

			/// <summary>The text for the log entry</summary>
			public string Text { get; }

			/// <summary>True if this log entry was read from a log file</summary>
			public bool FromFile { get; }

			/// <summary>Map fields to matches in 'Text'</summary>
			public string Tag => Read(nameof(Tag), x => x);
			public ELogLevel Level => Read(nameof(Level), x => Enum<ELogLevel>.Parse(x));
			public TimeSpan Timestamp => Read(nameof(Timestamp), x => TimeSpan.Parse(x));
			public string Message => Read(nameof(Message), x => x);
			public string File => Read(nameof(File), x => x);
			public int Line => Read(nameof(Line), x => int.Parse(x));
			public int Occurrences => Read(nameof(Occurrences), x => int.Parse(x));

			/// <summary>Lazy regex pattern match</summary>
			private T Read<T>(string grp, Func<string, T> parse)
			{
				m_match ??= m_this.LogEntryPattern?.Match(Text);
				if (m_match != null && m_match.Success)
				{
					try
					{
						var value = m_match.Groups[grp]?.Value ?? string.Empty;
						return parse(value);
					}
					catch { }
				}
				return default!;
			}
			private Match? m_match;

			/// <summary>The lighting pattern suitable for this log entry (or null)</summary>
			public HLPattern Highlight
			{
				get
				{
					if (m_highlight_issue != m_this.m_highlighting_issue)
					{
						m_highlight = m_this.Highlighting.FirstOrDefault(x => x.IsMatch(Text));
						m_highlight_issue = m_this.m_highlighting_issue;
					}
					return m_highlight ?? new HLPattern(Colors.Transparent, Colors.Black);
				}
			}
			private HLPattern? m_highlight;
			private int m_highlight_issue;

			/// <summary>Line wrapping</summary>
			public bool LineWrap => m_this.LineWrap;
		}

		/// <summary>Patterns for highlighting rows in the log</summary>
		[DebuggerDisplay("{Expr}")]
		public class HLPattern : Pattern
		{
			public HLPattern(Color bk, Color fr)
				: base()
			{
				BackColour = bk;
				ForeColour = fr;
			}
			public HLPattern(Color bk, Color fr, EPattern patn_type, string expr)
				: base(patn_type, expr)
			{
				BackColour = bk;
				ForeColour = fr;
			}
			public HLPattern(HLPattern rhs)
				: base(rhs)
			{
				BackColour = rhs.BackColour;
				ForeColour = rhs.ForeColour;
			}
			public HLPattern(XElement node)
				: base(node)
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
			public const string Tag = nameof(Logger.LogEvent.Tag);
			public const string Level = nameof(Logger.LogEvent.Level);
			public const string Timestamp = nameof(Logger.LogEvent.Timestamp);
			public const string Message = nameof(Logger.LogEvent.Message);
			public const string File = nameof(Logger.LogEvent.File);
			public const string Line = nameof(Logger.LogEvent.Line);
			public const string Occurrences = nameof(Logger.LogEvent.Occurrences);
		}
	}
}
