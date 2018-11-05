using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using Rylogic.Common;
using RyLogViewer.Options;

namespace RyLogViewer
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window, INotifyPropertyChanged
	{
		private readonly Settings m_settings;
		private readonly IReport m_report;

		public MainWindow(Main main, Settings settings, IReport report)
		{
			InitializeComponent();

			Main = main;
			m_settings = settings;
			m_report = report;
			m_grid.Settings = settings;

			// Initialise from settings
			m_recent_files.Import(m_settings.RecentFiles);
			m_recent_files.RecentFileSelected += fp =>
			{
				m_recent_files.Add(fp);
				Main.LogDataSource = new SingleFileSource(fp, m_settings);
			};

			// Create commands once loaded
			Loaded += (s, a) =>
			{
				OpenSingleLogFile = new OpenSingleLogFileCommand(this, Main, m_settings, m_report);
				ShowOptionsUI = new ShowOptionsUI(this, Main, m_settings, m_report);
				Shutdown = new ShutdownCommand();

				// Assign data contexts after properties have been set
				DataContext = this;
				m_panel.DataContext = this;
				m_grid.DataContext = this;
			};
		}

		/// <summary>Notify property changes</summary>
		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>Application logic</summary>
		private Main Main
		{
			get { return m_main; }
			set
			{
				if (m_main == value) return;
				if (m_main != null)
				{
					m_main.PropertyChanged -= HandlePropertyChanged;
				}
				m_main = value;
				if (m_main != null)
				{
					m_main.PropertyChanged += HandlePropertyChanged;
				}

				// Handlers
				void HandlePropertyChanged(object sender, PropertyChangedEventArgs e)
				{
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(WindowTitle)));
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StatusLogDataSource)));
				}
			}
		}
		private Main m_main;

		/// <summary>Open a single log file</summary>
		public ICommand OpenSingleLogFile { get; private set; }

		/// <summary>Open the Option UI</summary>
		public ICommand ShowOptionsUI { get; private set; }

		/// <summary>Shutdown the application</summary>
		public ICommand Shutdown { get; private set; }

		/// <summary>The data to display in the grid</summary>
		public IReadOnlyCollection<ILine> LogData => Main;

		/// <summary>Application title</summary>
		public string WindowTitle
		{
			get { return Main.LogDataSource != null ? $"RyLogViewer - {Main.LogDataSource.Path}" : $"RyLogViewer"; }
		}

		/// <summary>Status for the current log data source</summary>
		public string StatusLogDataSource
		{
			get { return Main.LogDataSource == null ? "No Log Data Source" : string.Empty; }
		}

		/// <summary>Status for the current file position</summary>
		public string StatusFilePosition
		{
			get
			{
				// Get current file position
				var r = -1;//todo selected row index
				var pos = (r != -1) ? Main[r].FileByteRange.Beg : 0;
				var end = Main.FileByteRange.End;
				return $"Position: {pos:N0} / {end:N0} bytes";
			}
		}

		/// <summary>Status for the current selection</summary>
		public string StatusSelection
		{
			get
			{
				var r = -1; //todo selected row index
				var sel_range = Range.Zero;// m_grid.SelectedRowIndexRange();
				var rg = (r == -1) ? Range.Zero//todo SelectedRowByteRange;
					: new Range(Main[sel_range.Begi].FileByteRange.Beg, Main[sel_range.Endi].FileByteRange.End);
				return $"Selection: [{rg.Beg:N0} - {rg.End:N0}] ({rg.Size} bytes)";
			}
		}

		/// <summary>Status for the current encoding</summary>
		public string StatusEncoding
		{
			get
			{
				var s = Main.EncodingName;
				return string.IsNullOrEmpty(s) ? "Encoding" : s;
			}
		}

		/// <summary>Status for the current line ending mode</summary>
		public string StatusLineEnding
		{
			get
			{
				var s = Main.LineEnding;
				return string.IsNullOrEmpty(s) ? "Line Ending" : s;
			}
		}

		/// <summary>Add a file to the recent file paths</summary>
		public void AddToRecentFiles(string filepath)
		{
			m_recent_files.Add(filepath);

			// Update the settings
			m_settings.General.LastLoadedFile = filepath;
			m_settings.RecentFiles = m_recent_files.Export();
		}
	}
}
