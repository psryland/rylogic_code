using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using Rylogic.Common;
using Rylogic.Gui.WPF;
using RyLogViewer.Options;

namespace RyLogViewer
{
	public partial class MainWindow : Window, INotifyPropertyChanged
	{
		public MainWindow(Main main, Settings settings, IReport report)
		{
			InitializeComponent();

			Main = main;
			Settings = settings;
			Report = report;
			m_grid.Settings = settings;

			// Initialise from settings
			m_recent_files.Import(Settings.RecentFiles);
			m_recent_files.RecentFileSelected += fp =>
			{
				m_recent_files.Add(fp);
				try
				{
					Main.LogDataSource = new SingleFileSource(fp, Settings);
				}
				catch
				{
					Main.LogDataSource = null;
				}
			};

			// Commands
			OpenSingleLogFile = Command.Create(this, OpenSingleLogFileInternal);
			ShowOptionsUI = Command.Create(this, ShowOptionsUIInternal);
			Shutdown = Command.Create(this, ShutdownInternal);

			Loaded += (s, a) =>
			{
				// Assign data contexts after properties have been set
				DataContext = this;
				m_panel.DataContext = this;
				m_grid.DataContext = this;
			};
		}

		/// <summary>Notify property changes</summary>
		public event PropertyChangedEventHandler? PropertyChanged;

		/// <summary>Application logic</summary>
		private Main Main
		{
			get => m_main;
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
				void HandlePropertyChanged(object? sender, PropertyChangedEventArgs e)
				{
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(WindowTitle)));
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StatusLogDataSource)));
				}
			}
		}
		private Main m_main = null!;

		/// <summary>Settings</summary>
		private Settings Settings { get; }

		/// <summary></summary>
		private IReport Report { get; }

		/// <summary>Open a single log file</summary>
		public ICommand OpenSingleLogFile { get; }
		private void OpenSingleLogFileInternal()
		{
			// Prompt for a log file
			var fd = new OpenFileDialog
			{
				Title = "Open a Log File",
				Filter = Constants.LogFileFilter,
				Multiselect = false,
				CheckFileExists = true,
			};
			if (fd.ShowDialog(this) != true) return;
			var filepath = fd.FileName;

			try
			{
				// Validate
				if (!Path_.FileExists(filepath))
					throw new FileNotFoundException($"File '{filepath}' does not exist");

				// Add the file to the recent files
				AddToRecentFiles(filepath);

				// Create a log data source from the log file
				Main.LogDataSource = new SingleFileSource(filepath, Settings);
			}
			catch (Exception ex)
			{
				Report.ErrorPopup($"Failed to open file {filepath} due to an error.", ex);
				Main.LogDataSource = null;
			}
		}

		/// <summary>Open the Option UI</summary>
		public ICommand ShowOptionsUI { get; }
		private void ShowOptionsUIInternal(object? value)
		{
			if (m_options_ui == null)
			{
				m_options_ui = new OptionsUI(Main, Settings, Report) { Owner = this };
				m_options_ui.SelectedPage = value is EOptionsPage page ? page : EOptionsPage.General;
				m_options_ui.Closed += delegate { m_options_ui = null; };
				m_options_ui.Show();
			}
			m_options_ui.Focus();
		}
		private OptionsUI? m_options_ui;

		/// <summary>Shutdown the application</summary>
		public ICommand Shutdown { get; }
		private void ShutdownInternal()
		{
			Application.Current.Shutdown(0);
		}

		/// <summary>The data to display in the grid</summary>
		public IReadOnlyCollection<ILine> LogData => Main;

		/// <summary>Application title</summary>
		public string WindowTitle => Main.LogDataSource != null ? $"RyLogViewer - {Main.LogDataSource.Path}" : $"RyLogViewer";

		/// <summary>Status for the current log data source</summary>
		public string StatusLogDataSource => Main.LogDataSource == null ? "No Log Data Source" : string.Empty;

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
				var sel_range = RangeI.Zero;// m_grid.SelectedRowIndexRange();
				var rg = (r == -1) ? RangeI.Zero//todo SelectedRowByteRange;
					: new RangeI(Main[sel_range.Begi].FileByteRange.Beg, Main[sel_range.Endi].FileByteRange.End);
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
			Settings.General.LastLoadedFile = filepath;
			Settings.RecentFiles = m_recent_files.Export();
		}
	}
}
