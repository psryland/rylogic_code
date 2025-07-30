using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using LDraw.Dialogs;
using LDraw.UI;
using Microsoft.Win32;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Gfx;
using Rylogic.Gui.WPF;
using Rylogic.Utility;

namespace LDraw
{
	public partial class MainWindow :Window, INotifyPropertyChanged
	{
		public MainWindow(Model model)
		{
			InitializeComponent();
			Model = model;
			StatusMessage = "Idle";

			// Commands
			NewScript = Command.Create(this, NewScriptInternal);
			NewScene = Command.Create(this, NewSceneInternal);
			OpenFile = Command.Create(this, OpenFileInternal);
			SaveFile = Command.Create(this, SaveFileInternal);
			SaveFileAs = Command.Create(this, SaveFileAsInternal);
			ToggleStreaming = Command.Create(this, ToggleStreamingInternal);
			ShowPreferences = Command.Create(this, ShowPreferencesInternal);
			ShowExampleScript = Command.Create(this, ShowExampleScriptInternal);
			ShowAbout = Command.Create(this, ShowAboutInternal);
			Exit = Command.Create(this, ExitInternal);

			m_recent_files.Import(Model.Settings.RecentFiles);
			m_recent_files.RecentFileSelected = fp => AddFileSource(fp);

			InitDockContainer();

			DataContext = this;
		}
		protected override void OnClosing(CancelEventArgs e)
		{
			m_dc.LayoutChanged -= SaveLayout;
			base.OnClosing(e);
		}
		protected override void OnClosed(EventArgs e)
		{
			Util.DisposeRange(m_dc.AllContent.OfType<IDisposable>());
			m_dc.Dispose();
			Model = null!;
			base.OnClosed(e);
		}
		protected override void OnPreviewKeyDown(KeyEventArgs e)
		{
			if (e.Key == Key.Tab && Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && m_dc.ActivePane != null)
			{
				var steps = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? -1 : +1;
				m_dc.ActivePane.CycleVisibleContent(steps);
				e.Handled = true;
			}
			base.OnPreviewKeyDown(e);
		}
		protected override void OnPreviewDrop(DragEventArgs e)
		{
			if (e.Data.GetDataPresent(DataFormats.FileDrop) &&
				e.Data.GetData(DataFormats.FileDrop) is string[] files)
			{
				// If dropped on a specific scene, make that the selected scene for the source
				var scenes = e.Source is SceneUI scene
					? new[] { scene }
					: null;

				// Add each file as a source
				foreach (var file in files)
					AddFileSource(file, scenes);

				e.Handled = true;
			}

			base.OnPreviewDrop(e);
		}

		/// <summary>App logic</summary>
		public Model Model
		{
			get => m_model;
			private set
			{
				if (m_model == value) return;
				if (m_model != null)
				{
					m_model.FileOpening -= HandleFileOpened;
					m_model.ParsingProgressChanged -= HandleParsingProgressChanged;
					m_model.SourcesChanged -= HandleSourcesChanged;
					Util.Dispose(ref m_model!);
				}
				m_model = value;
				if (m_model != null)
				{
					m_model.SourcesChanged += HandleSourcesChanged;
					m_model.ParsingProgressChanged += HandleParsingProgressChanged;
					m_model.FileOpening += HandleFileOpened;
				}

				// Handlers
				void HandleFileOpened(object? sender, ValueEventArgs<string> e)
				{
					m_recent_files.Add(e.Value);
					Model.Settings.RecentFiles = m_recent_files.Export();
				}
				void HandleParsingProgressChanged(object? sender, EventArgs args)
				{
					NotifyPropertyChanged(nameof(ParsingProgress));
				}
				void HandleSourcesChanged(object? sender, EventArgs e)
				{
					NotifyPropertyChanged(nameof(StreamingState));
				}
			}
		}
		private Model m_model = null!;

		/// <summary>The current active content in the dock container</summary>
		public IDockable? ActiveContent
		{
			get => m_active_content;
			set
			{
				if (m_active_content == value) return;
				if (m_active_content != null)
				{
					if (m_active_content is ScriptUI script)
						script.PropertyChanged -= HandleScriptPropertyChanged;
				}
				m_active_content = value;
				if (m_active_content != null)
				{
					if (m_active_content is ScriptUI script)
						script.PropertyChanged -= HandleScriptPropertyChanged;
				}

				NotifyPropertyChanged(nameof(ActiveScene));
				NotifyPropertyChanged(nameof(ActiveScript));
				NotifyPropertyChanged(nameof(ScriptHasFocus));
				NotifyPropertyChanged(nameof(ScriptHasFocusAndNeedsSave));
				NotifyPropertyChanged(nameof(ActiveContent));

				void HandleScriptPropertyChanged(object? sender, PropertyChangedEventArgs e)
				{
					if (sender is not ScriptUI script) return;
					if (e.PropertyName == nameof(ScriptUI.SaveNeeded))
						NotifyPropertyChanged(nameof(ScriptHasFocusAndNeedsSave));
				}
			}
		}
		private IDockable? m_active_content;

		/// <summary>The active content cast to specific types for binding</summary>
		public SceneUI? ActiveScene => ActiveContent as SceneUI;
		public ScriptUI? ActiveScript => ActiveContent as ScriptUI;

		/// <summary>True if the active content in the dock container is a script ui</summary>
		public bool ScriptHasFocus => m_dc.ActiveDockable is ScriptUI;

		/// <summary>True if the active content in the dock container is a script ui and its content needs saving</summary>
		public bool ScriptHasFocusAndNeedsSave => m_dc.ActiveDockable is ScriptUI script && script.SaveNeeded;

		/// <summary>Status bar text</summary>
		public string StatusMessage
		{
			get => m_status_message ?? string.Empty;
			set
			{
				if (m_status_message == value) return;
				m_status_message = value;
				NotifyPropertyChanged(nameof(StatusMessage));
			}
		}
		private string? m_status_message;

		/// <summary>The text description of the streaming state</summary>
		public View3d.EStreamingState StreamingState => Model.View3d.StreamingState;

		/// <summary>Non-null while script parsing is in progress</summary>
		public ParsingProgressData? ParsingProgress => Model.ParsingProgress;

		/// <summary>Set the initial state of the dock container</summary>
		private void InitDockContainer()
		{
			// Set options
			m_dc.Options.AlwaysShowTabs = true;

			// Add a log window
			var log = new LogUI(Model);
			m_dc.Add(log, EDockSite.Right).IsAutoHide = true;

			// Add the sources list window
			var sources = new SourcesListUI(Model);
			m_dc.Add(sources, EDockSite.Left).IsAutoHide = true;

			// Add a main scene window
			var scene = Model.Scenes.Add2(new SceneUI(Model, Model.GenerateSceneName()));
			m_dc.Add(scene, EDockSite.Centre);

			// Restore the layout
			m_dc.LoadLayout(Model.Settings.UILayout, (name, type, udat) =>
			{
				// Create scenes that are saved in the layout
				if (type == typeof(SceneUI).FullName)
					return Model.Scenes.Add2(new SceneUI(Model, name)).DockControl;

				// Ignore scripts that are in the settings
				return null;
			});

			// Update our reference to the active content whenever it changes in the dock container
			m_dc.ActiveContentChanged += delegate { ActiveContent = m_dc.ActiveDockable; };
			m_dc.LayoutChanged += SaveLayout;
			scene.DockControl.IsActiveContent = true;

			// Add the menu for dock container windows
			m_menu.Items.Insert(m_menu.Items.Count - 1, m_dc.WindowsMenu());
		}

		/// <summary>Persist the current layout to settings</summary>
		private void SaveLayout(object? sender = null, EventArgs? args = null)
		{
			Model.Settings.UILayout = m_dc.SaveLayout();
		}

		/// <summary>Open 'source' in a script editor</summary>
		public void OpenInEditor(Source source)
		{
			try
			{
				// Open 'source' in an editor window
				var ui = Model.OpenInEditor(source) ?? throw new Exception("Failed to open the editor for the new script file");

				// Display the editor
				m_dc.Add(ui, EDockSite.Left).IsFloating = true;
			}
			catch (Exception ex)
			{
				Log.Write(ELogLevel.Info, ex, "Opening source in editor failed.", string.Empty, 0);
				MsgBox.Show(this, $"Opening source in editor failed.\n{ex.Message}", Util.AppProductName, MsgBox.EButtons.OK, MsgBox.EIcon.Information);
			}
		}

		/// <summary>Add a file source</summary>
		public Source? AddFileSource(string? filepath, IList<SceneUI>? scenes = null)
		{
			try
			{
				// Prompt for a filepath if none provided
				if (filepath == null || filepath.Length == 0)
				{
					var dlg = new OpenFileDialog { Title = "Open an Ldraw supported file", Filter = Model.SupportedFilesFilter };
					if (dlg.ShowDialog(App.Current.MainWindow) != true)
						return null;
					
					filepath = dlg.FileName ?? throw new FileNotFoundException($"A invalid filepath was selected");
				}

				// If the file doesn't exist reject it
				if (!Path_.FileExists(filepath))
					throw new FileNotFoundException($"File '{filepath}' does not exist");

				// Add the file as a new source
				var src = Model.AddFileSource(filepath);
				src.SelectedScenes.AddRange(scenes ?? []);
				return src;
			}
			catch (Exception ex)
			{
				Log.Write(ELogLevel.Info, ex, "Add file source failed.", filepath ?? string.Empty, 0);
				MsgBox.Show(Owner, $"Add file source failed.\n{ex.Message}", Util.AppProductName, MsgBox.EButtons.OK, MsgBox.EIcon.Information);
				return null;
			}
		}

		/// <summary>Add a new Script to the view</summary>
		public Command NewScript { get; }
		private void NewScriptInternal()
		{
			try
			{
				// Create a new script file
				var filepath = Model.CreateNewScriptFile();
				var src = AddFileSource(filepath) ?? throw new Exception("Failed to an empty script file");

				// Open it in an editor
				var ui = Model.OpenInEditor(src) ?? throw new Exception("Failed to open the editor for the new script file");

				// Display the editor
				m_dc.Add(ui, EDockSite.Left).IsFloating = true;
			}
			catch (Exception ex)
			{
				Log.Write(ELogLevel.Info, ex, "Create new script failed.", string.Empty, 0);
				MsgBox.Show(this, $"Create new script failed.\n{ex.Message}", Util.AppProductName, MsgBox.EButtons.OK, MsgBox.EIcon.Information);
			}
		}

		/// <summary>Add a new Scene to the view</summary>
		public Command NewScene { get; }
		private void NewSceneInternal()
		{
			try
			{
				var scene = Model.Scenes.Add2(new SceneUI(Model, Model.GenerateSceneName()));
				m_dc.Add(scene, EDockSite.Centre);
			}
			catch (Exception ex)
			{
				Log.Write(ELogLevel.Info, ex, "Create new scene failed.", string.Empty, 0);
				MsgBox.Show(this, $"Create new scene failed.\n{ex.Message}", Util.AppProductName, MsgBox.EButtons.OK, MsgBox.EIcon.Information);
			}
		}

		/// <summary>Open a file</summary>
		public Command OpenFile { get; }
		private void OpenFileInternal()
		{
			AddFileSource(null);
		}

		/// <summary>Save the currently focused script</summary>
		public Command SaveFile { get; }
		private void SaveFileInternal()
		{
			if (m_dc.ActiveDockable is not ScriptUI script)
				return;

			script.SaveScript.Execute();
		}

		/// <summary>Save the currently focused script</summary>
		public Command SaveFileAs { get; }
		private void SaveFileAsInternal()
		{
			if (m_dc.ActiveDockable is not ScriptUI script)
				return;

			script.SaveScript.Execute();
			throw new NotImplementedException("Save as not supported");
		}

		/// <summary>Enable/Disable listening for streaming connections</summary>
		public Command ToggleStreaming { get; }
		private void ToggleStreamingInternal()
		{
			var currently_active = StreamingState != View3d.EStreamingState.Disconnected;
			Model.View3d.Streaming(!currently_active, Model.Settings.StreamingPort);
			NotifyPropertyChanged(nameof(StreamingState));
		}

		/// <summary>Show application preferences</summary>
		public Command ShowPreferences { get; }
		private void ShowPreferencesInternal()
		{
			if (m_settings_ui == null)
			{
				m_settings_ui = new SettingsUI(this, Model.Settings);
				m_settings_ui.Closed += delegate { m_settings_ui = null; };
				m_settings_ui.Show();
			}
			m_settings_ui.Focus();
		}
		private SettingsUI? m_settings_ui;

		/// <summary>Show a dialog containing the example script</summary>
		public Command ShowExampleScript { get; }
		private void ShowExampleScriptInternal()
		{
			if (m_example_script_ui == null)
			{
				m_example_script_ui = new ExampleScriptUI { Icon = Icon };
				m_example_script_ui.Closed += delegate { m_example_script_ui = null; };
				m_example_script_ui.Show();
			}
			m_example_script_ui.Focus();
		}
		private ExampleScriptUI? m_example_script_ui;

		/// <summary>Show the about box</summary>
		public Command ShowAbout { get; }
		private void ShowAboutInternal()
		{
			new AboutUI(this).ShowDialog();
		}

		/// <summary>Shutdown the application</summary>
		public Command Exit { get; }
		private void ExitInternal()
		{
			Close();
		}

		/// <summary></summary>
		public event PropertyChangedEventHandler? PropertyChanged;
		private void NotifyPropertyChanged(string prop_name)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
		}
	}
}
