using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Microsoft.Win32;
using Rylogic.Common;
using Rylogic.Core.Windows;
using Rylogic.Gui.WPF.Validators;
using Rylogic.Utility;

namespace Rylogic.Gui.WPF
{
	public partial class BrowsePathUI :UserControl
	{
		public BrowsePathUI()
		{
			InitializeComponent();

			PathType = EType.OpenFile;
			PathMustExist = false;
			RequireRoot = false;
			History = CollectionViewSource.GetDefaultView(new ObservableCollection<string>());
			MaxHistoryLength = 10;

			// Commands
			BrowsePath = Command.Create(this, BrowsePathInternal);
			Commit = Command.Create(this, CommitInternal);

			// Set up validation
			var binding = BindingOperations.GetBinding(m_cb_path, ComboBox.TextProperty);
			binding.ValidationRules.Add(new PredicateValidator(DoValidation));

			// Don't set DataContext, we need to inherit the parent's context
		}

		/// <summary>Raised when a path is selected (through focus lost, or enter key)</summary>
		public event EventHandler? SelectedPathChanged;

		/// <summary>The type of path to browse for</summary>
		public EType PathType
		{
			get => (EType)GetValue(PathTypeProperty);
			set => SetValue(PathTypeProperty, value);
		}
		public static readonly DependencyProperty PathTypeProperty = Gui_.DPRegister<BrowsePathUI>(nameof(PathType));

		/// <summary>The selected path</summary>
		public string SelectedPath
		{
			get => (string)GetValue(SelectedPathProperty);
			set => SetValue(SelectedPathProperty, value);
		}
		private void SelectedPath_Changed()
		{
			if (string.IsNullOrWhiteSpace(SelectedPath)) return;
			if (History.SourceCollection is IList<string> history)
			{
				Util.AddToHistoryList(history, SelectedPath, true, MaxHistoryLength);
				History.MoveCurrentTo(SelectedPath);
			}
			SelectedPathChanged?.Invoke(this, EventArgs.Empty);
		}
		public static readonly DependencyProperty SelectedPathProperty = Gui_.DPRegister<BrowsePathUI>(nameof(SelectedPath));

		/// <summary>The allow missing filepaths</summary>
		public bool PathMustExist
		{
			get => (bool)GetValue(PathMustExistProperty);
			set => SetValue(PathMustExistProperty, value);
		}
		public static readonly DependencyProperty PathMustExistProperty = Gui_.DPRegister<BrowsePathUI>(nameof(PathMustExist));

		/// <summary>The path must contain the root drive</summary>
		public bool RequireRoot
		{
			get => (bool)GetValue(RequireRootProperty);
			set => SetValue(RequireRootProperty, value);
		}
		public static readonly DependencyProperty RequireRootProperty = Gui_.DPRegister<BrowsePathUI>(nameof(RequireRoot));

		/// <summary>Callback function for validating a selected path</summary>
		public Func<string, ValidationResult>? PathValidation
		{
			get => (Func<object, ValidationResult>)GetValue(PathValidationProperty);
			set => SetValue(PathValidationProperty, value);
		}
		public static readonly DependencyProperty PathValidationProperty = Gui_.DPRegister<BrowsePathUI>(nameof(PathValidation), flags: FrameworkPropertyMetadataOptions.None);

		/// <summary>The history of paths</summary>
		public ICollectionView History
		{
			get => (ICollectionView)GetValue(HistoryProperty);
			set => SetValue(HistoryProperty, value);
		}
		public static readonly DependencyProperty HistoryProperty = Gui_.DPRegister<BrowsePathUI>(nameof(History), flags: FrameworkPropertyMetadataOptions.None);

		/// <summary>The maximum length of the stored history</summary>
		public int MaxHistoryLength
		{
			get => (int)GetValue(MaxHistoryLengthProperty);
			set => SetValue(MaxHistoryLengthProperty, value);
		}
		public static readonly DependencyProperty MaxHistoryLengthProperty = Gui_.DPRegister<BrowsePathUI>(nameof(MaxHistoryLength));

		/// <summary>The title to display on the path browser dialog</summary>
		public string DialogTitle
		{
			get { return (string)GetValue(DialogTitleProperty) ?? $"Select a {(PathType == EType.SelectDirectory ? "Directory" : "File")}"; }
			set { SetValue(DialogTitleProperty, value); }
		}
		public static readonly DependencyProperty DialogTitleProperty = Gui_.DPRegister<BrowsePathUI>(nameof(DialogTitle));

		/// <summary>The filter to apply in open/save file dialogs. Example: "Image files (*.bmp, *.jpg)|*.bmp;*.jpg|All files (*.*)|*.*"</summary>
		public string FileFilter
		{
			get { return (string)GetValue(FileFilterProperty); }
			set { SetValue(FileFilterProperty, value); }
		}
		public static readonly DependencyProperty FileFilterProperty = Gui_.DPRegister<BrowsePathUI>(nameof(FileFilter));

		/// <summary>Open the path browser</summary>
		public Command BrowsePath { get; }
		private void BrowsePathInternal()
		{
			switch (PathType)
			{
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
				default:
				{
					throw new Exception($"Unknown path type: {PathType}");
				}
			}
		}

		/// <summary>Trigger the SelectedPathChanged event</summary>
		public Command Commit { get; }
		private void CommitInternal()
		{
			// Trigger an update of the 'SelectedPath' bindings
			if (GetBindingExpression(SelectedPathProperty) is BindingExpression binding)
				binding.UpdateSource();
		}

		/// <summary>Watch for enter key presses</summary>
		private void HandlePreviewKeyUp(object sender, KeyEventArgs e)
		{
			if (e.Key != Key.Enter)
				return;

			Commit.Execute();
			e.Handled = true;
		}

		/// <summary>Perform validation on the path</summary>
		private ValidationResult DoValidation(object arg)
		{
			// 'arg' must be a string
			if (arg is not string path)
				return new ValidationResult(false, $"Value '{arg}' is not of the expected type");

			// Check the path has a valid form
			switch (PathType)
			{
				case EType.OpenFile:
				case EType.SaveFile:
				{
					if (!Path_.IsValidFilepath(path, RequireRoot))
						return new ValidationResult(false, $"Path '{path}' is not a valid file path string");
					break;
				}
				case EType.SelectDirectory:
				{
					if (!Path_.IsValidDirectory(path, RequireRoot))
						return new ValidationResult(false, $"Path '{path}' is not a valid directory path string");
					break;
				}
				default:
				{
					throw new Exception($"Unknown path type: {PathType}");
				}
			}

			// Path must exist?
			if (PathMustExist && !Path_.PathExists(path))
				return new ValidationResult(false, $"Path '{path}' does not exist");

			// Custom validation function
			return PathValidation != null
				? PathValidation(path)
				: ValidationResult.ValidResult;
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

