using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using Rylogic.Common;
using Rylogic.Gui.WPF;
using Rylogic.Utility;
using Rylogic.Windows;

namespace LDraw.Dialogs
{
	public partial class SettingsUI :Window, INotifyPropertyChanged
	{
		public SettingsUI(Window owner, SettingsData settings)
		{
			InitializeComponent();
			Owner = owner;
			Icon = owner?.Icon;
			Settings = settings;

			AvailableFonts = CollectionViewSource.GetDefaultView(Fonts.SystemFontFamilies);
			
			m_editor_cs_boilerplate.Text = Settings.EmbeddedCSharpBoilerPlate;
			m_editor_cs_boilerplate.TextArea.LostKeyboardFocus += delegate
			{
				Settings.EmbeddedCSharpBoilerPlate = m_editor_cs_boilerplate.Text;
			};

			Accept = Command.Create(this, AcceptInternal);
			AddIncludePath = Command.Create(this, AddIncludePathInternal);
			RemoveIncludePath = Command.Create(this, RemoveIncludePathInternal);
			MoveIncludePathUp = Command.Create(this, MoveIncludePathUpInternal);
			MoveIncludePathDown = Command.Create(this, MoveIncludePathDownInternal);
			ResetToDefaults = Command.Create(this, ResetToDefaultsInternal);

			DataContext = this;
		}

		/// <summary>The settings data being modified</summary>
		public SettingsData Settings { get; }

		/// <summary>Available fonts</summary>
		public ICollectionView AvailableFonts { get; }

		/// <summary>Embedded C# boiler plate code</summary>
		public string EmbeddedCSharpBoilerPlate
		{
			get => Settings.EmbeddedCSharpBoilerPlate;
			set => Settings.EmbeddedCSharpBoilerPlate = value;
		}

		/// <summary>Close the dialog</summary>
		public Command Accept { get; }
		private void AcceptInternal()
		{
			Close();
		}

		/// <summary>Add a path to the include paths collection</summary>
		public Command AddIncludePath { get; }
		private void AddIncludePathInternal()
		{
			var dlg = new OpenFolderUI { Title = "Add an Include Path" };
			if (dlg.ShowDialog(this) == true)
				Settings.IncludePaths = Settings.IncludePaths.Append(dlg.SelectedPath).ToArray();
		}

		/// <summary>Add a path to the include paths collection</summary>
		public Command RemoveIncludePath { get; }
		private void RemoveIncludePathInternal(object? parameter)
		{
			if (parameter is string path)
				Settings.IncludePaths = Settings.IncludePaths.Where(x => x != path).ToArray();
		}

		/// <summary>Reorder the include paths</summary>
		public Command MoveIncludePathUp { get; }
		private void MoveIncludePathUpInternal(object? parameter)
		{
			if (parameter is int index && index >= 1 && index < Settings.IncludePaths.Length)
			{
				Util.Swap(ref Settings.IncludePaths[index], ref Settings.IncludePaths[index - 1]);
				Settings.IncludePaths = Settings.IncludePaths.ToArray();
			}
		}

		/// <summary>Reorder the include paths</summary>
		public Command MoveIncludePathDown { get; }
		private void MoveIncludePathDownInternal(object? parameter)
		{
			if (parameter is int index && index >= 0 && index < Settings.IncludePaths.Length - 1)
			{
				Util.Swap(ref Settings.IncludePaths[index], ref Settings.IncludePaths[index + 1]);
				Settings.IncludePaths = Settings.IncludePaths.ToArray();
			}
		}

		/// <summary>Reset the settings to defaults</summary>
		public Command ResetToDefaults { get; }
		private void ResetToDefaultsInternal()
		{
			Settings.Reset();
			Settings.Save();
		}

		/// <summary>Property changed</summary>
		public event PropertyChangedEventHandler? PropertyChanged;
		public void NotifyPropertyChanged(string prop_name)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
		}
	}
}
