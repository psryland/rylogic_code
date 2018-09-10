using System;
using System.IO;
using System.Windows.Input;
using Microsoft.Win32;
using Rylogic.Common;
using RyLogViewer.Options;

namespace RyLogViewer
{
	public class OpenSingleLogFileCommand : ICommand
	{
		private readonly Main m_main;
		private readonly MainWindow m_ui;
		private readonly Settings m_settings;
		private readonly IReport m_report;

		public OpenSingleLogFileCommand(MainWindow ui, Main main, Settings settings, IReport report)
		{
			m_ui = ui;
			m_main = main;
			m_settings = settings;
			m_report = report;
		}
		public void Execute(object _)
		{
			// Prompt for a log file
			var fd = new OpenFileDialog
			{
				Title = "Open a Log File",
				Filter = Constants.LogFileFilter,
				Multiselect = false,
				CheckFileExists = true,
			};
			if (fd.ShowDialog(m_ui) != true) return;
			var filepath = fd.FileName;

			try
			{
				// Validate
				if (!Path_.FileExists(filepath))
					throw new FileNotFoundException($"File '{filepath}' does not exist");

				// Add the file to the recent files
				m_ui.AddToRecentFiles(filepath);

				// Create a log data source from the log file
				m_main.LogDataSource = new SingleFileSource(filepath, m_settings);
			}
			catch (Exception ex)
			{
				m_report.ErrorPopup($"Failed to open file {filepath} due to an error.", ex);
				m_main.LogDataSource = null;
			}
		}
		public bool CanExecute(object _) => true;
		public event EventHandler CanExecuteChanged { add { } remove { } }
	}
}
