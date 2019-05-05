using System;
using System.Windows;
using System.Windows.Controls;
using Rylogic.Common;
using Rylogic.Gui.WPF;

namespace EDTradeAdvisor.UI.Dialogs
{
	public partial class SettingsUI : Window
	{
		public SettingsUI(Window owner)
		{
			InitializeComponent();
			Owner = owner;
			DataContext = Settings.Instance;
		}

		/// <summary></summary>
		public Func<string, ValidationResult> DataPath_Validate = path =>
		{
			return !Path_.IsValidDirectory(path, true)
				? new ValidationResult(false, "Must be a valid directory path")
				: ValidationResult.ValidResult;
		};

		/// <summary></summary>
		public Func<string, ValidationResult> JournalFilesPath_Validate = path =>
		{
			return !string.IsNullOrEmpty(path) && !Path_.DirExists(path)
				? new ValidationResult(false, "Must be empty or a valid directory path")
				: ValidationResult.ValidResult;
		};

		/// <summary></summary>
		private void ResetJournalPathToDefault(object sender, RoutedEventArgs e)
		{
			Settings.Instance.JournalFilesDir = Settings.Default.JournalFilesDir;
		}
		private void HandleClose(object sender, RoutedEventArgs e)
		{
			Close();
		}
	}
}
