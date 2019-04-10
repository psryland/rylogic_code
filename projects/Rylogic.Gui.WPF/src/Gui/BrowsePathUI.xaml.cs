using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Win32;
using Rylogic.Core.Windows;
using Rylogic.Gui.WPF.Validators;
using Rylogic.Utility;

namespace Rylogic.Gui.WPF
{
	public partial class BrowsePathUI : UserControl
	{
		static BrowsePathUI()
		{
			PathTypeProperty = Gui_.DPRegister<BrowsePathUI>(nameof(PathType));
			SelectedPathProperty = Gui_.DPRegister<BrowsePathUI>(nameof(SelectedPath));
			PathValidationProperty = Gui_.DPRegister<BrowsePathUI>(nameof(PathValidation), flags: FrameworkPropertyMetadataOptions.None);
			MaxHistoryLengthProperty = Gui_.DPRegister<BrowsePathUI>(nameof(MaxHistoryLength));
			DialogTitleProperty = Gui_.DPRegister<BrowsePathUI>(nameof(DialogTitle));
			FileFilterProperty = Gui_.DPRegister<BrowsePathUI>(nameof(FileFilter));
		}
		public BrowsePathUI()
		{
			InitializeComponent();

			PathType = EType.OpenFile;
			History = new ObservableCollection<string>();
			HistoryView = CollectionViewSource.GetDefaultView(History);
			MaxHistoryLength = 10;

			// Commands
			BrowsePath = Command.Create(this, BrowsePathInternal);

			// Don't set DataContext, we need to inherit the parent's context
		}

		/// <summary>The type of path to browse for</summary>
		public EType PathType
		{
			get { return (EType)GetValue(PathTypeProperty); }
			set { SetValue(PathTypeProperty, value); }
		}
		public static readonly DependencyProperty PathTypeProperty;

		/// <summary>The selected path</summary>
		public string SelectedPath
		{
			get { return (string)GetValue(SelectedPathProperty); }
			set { SetValue(SelectedPathProperty, value); }
		}
		private void SelectedPath_Changed(string new_value)
		{
			if (string.IsNullOrWhiteSpace(new_value)) return;
			Util.AddToHistoryList(History, new_value, true, MaxHistoryLength);
			SelectedPathChanged?.Invoke(this, EventArgs.Empty);
		}
		public static readonly DependencyProperty SelectedPathProperty;
		public event EventHandler SelectedPathChanged;

		/// <summary>Callback function for validating a selected path</summary>
		public Func<string, ValidationResult> PathValidation
		{
			get { return (Func<string, ValidationResult>)GetValue(PathValidationProperty); }
			set { SetValue(PathValidationProperty, value); }
		}
		private void PathValidation_Changed(Func<string, ValidationResult> new_value)
		{
			// Update the set of validation rules
			var binding = BindingOperations.GetBinding(m_cb_path, ComboBox.TextProperty);
			binding.ValidationRules.Clear();
			binding.ValidationRules.Add(new PredicateValidator<string>(new_value));
		}
		public static readonly DependencyProperty PathValidationProperty;

		/// <summary>The history of paths</summary>
		public ObservableCollection<string> History { get; }
		public ICollectionView HistoryView { get; }

		/// <summary>The maximum length of the stored history</summary>
		public int MaxHistoryLength
		{
			get { return (int)GetValue(MaxHistoryLengthProperty); }
			set { SetValue(MaxHistoryLengthProperty, value); }
		}
		public static readonly DependencyProperty MaxHistoryLengthProperty;

		/// <summary>The title to display on the path browser dialog</summary>
		public string DialogTitle
		{
			get { return (string)GetValue(DialogTitleProperty) ?? $"Select a {(PathType == EType.SelectDirectory ? "Directory" : "File")}"; }
			set { SetValue(DialogTitleProperty, value); }
		}
		public static readonly DependencyProperty DialogTitleProperty;

		/// <summary>The filter to apply in open/save file dialogs. Example: "Image files (*.bmp, *.jpg)|*.bmp;*.jpg|All files (*.*)|*.*"</summary>
		public string FileFilter
		{
			get { return (string)GetValue(FileFilterProperty); }
			set { SetValue(FileFilterProperty, value); }
		}
		public static readonly DependencyProperty FileFilterProperty;

		/// <summary>Open the path browser</summary>
		public Command BrowsePath { get; }
		private void BrowsePathInternal()
		{
			switch (PathType)
			{
			default:
				{
					throw new Exception($"Unknown path type: {PathType}");
				}
			case EType.OpenFile:
				{
					var dlg = new OpenFileDialog { Title = DialogTitle, Filter = FileFilter, FileName = SelectedPath };
					if (dlg.ShowDialog(this) != true) break;
					SelectedPath = dlg.FileName;
					break;
				}
			case EType.SaveFile:
				{
					var dlg = new SaveFileDialog { Title = DialogTitle, Filter = FileFilter, FileName = SelectedPath };
					if (dlg.ShowDialog(this) != true) break;
					SelectedPath = dlg.FileName;
					break;
				}
			case EType.SelectDirectory:
				{
					var dlg = new OpenFolderUI { Title = DialogTitle, SelectedPath = SelectedPath };
					if (dlg.ShowDialog(this) != true) break;
					SelectedPath = dlg.SelectedPath;
					break;
				}
			}
		}

		/// <summary>The type of path to browse for</summary>
		public enum EType
		{
			OpenFile,
			SaveFile,
			SelectDirectory,
		}
	}
}
