using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Win32;
using Rylogic.Common;
using Rylogic.Container;
using Rylogic.Extn;
using Rylogic.Gfx;
using Rylogic.Gui.WPF;
using Rylogic.Utility;

namespace LDraw.UI
{
	public sealed partial class ScriptUI :UserControl, IDockable, IDisposable, INotifyPropertyChanged
	{
		// Notes:
		//  - All scripts are created with an associated file. New scripts start with a
		//    temporary script filepath and get saved to a new location by the user.

		public ScriptUI(Model model, string name, string filepath, Guid context_id)
		{
			InitializeComponent();
			DockControl = new DockControl(this, $"Script-{context_id}")
			{
				ShowTitle = false,
				TabText = name,
				TabCMenu = (ContextMenu)FindResource("TabCMenu"),
				DestroyOnClose = true,
			};
			Model = model;
			ContextId = context_id;
			Filepath = filepath;
			ScriptName = name;
			Editor = m_scintilla_control;
			Scenes = new ListCollectionView(new List<SceneUIWrapper>());
			LdrAutoComplete = new View3d.AutoComplete();

			Render = Command.Create(this, RenderInternal);
			SaveScript = Command.Create(this, SaveScriptInternal);
			RemoveObjects = Command.Create(this, RemoveObjectsInternal);

			// If the temporary script exists, load it
			if (Path_.FileExists(Filepath))
				LoadFile();

			DataContext = this;
		}
		public void Dispose()
		{
			// Delete empty temporary scripts
			if (Model != null && Editor != null && Editor.TextLength == 0 && Model.IsTempScriptFilepath(Filepath))
				Path_.DelFile(Filepath, fail_if_missing: false);

			//Scene = null;
			Model = null!;
			Editor = null!;
			DockControl = null!;
			GC.SuppressFinalize(this);
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
					m_dock_control.ActiveChanged -= HandleSceneActive;
					m_dock_control.SavingLayout -= HandleSavingLayout;
					Util.Dispose(ref m_dock_control!);
				}
				m_dock_control = value;
				if (m_dock_control != null)
				{
					m_dock_control.SavingLayout += HandleSavingLayout;
					m_dock_control.ActiveChanged += HandleSceneActive;
				}

				// Handlers
				void HandleSceneActive(object sender, ActiveContentChangedEventArgs args)
				{
					//	Options.BkColour = args.ContentNew == this ? Color.LightSteelBlue : Color.LightGray;
					//	Invalidate();
				}
				void HandleSavingLayout(object sender, DockContainerSavingLayoutEventArgs args)
				{
					//	args.Node.Add2(nameof(Camera), Camera, false);
				}
			}
		}
		private DockControl m_dock_control = null!;

		/// <summary>The name assigned to this script UI</summary>
		public string ScriptName
		{
			get => m_script_name;
			set
			{
				if (m_script_name == value) return;
				m_script_name = value;
				if (DockControl != null)
					DockControl.TabText = m_script_name ?? string.Empty;

				NotifyPropertyChanged(nameof(ScriptName));
			}
		}
		private string m_script_name = null!;

		/// <summary>App logic</summary>
		public Model Model
		{
			get => m_model;
			private set
			{
				if (m_model == value) return;
				if (m_model != null)
				{
					m_model.Scenes.CollectionChanged -= HandleScenesCollectionChanged;
					m_model.Scripts.Remove(this);
				}
				m_model = value;
				if (m_model != null)
				{
					// Don't add this script to m_model.Scripts, that's the caller's choice.
					m_model.Scenes.CollectionChanged += HandleScenesCollectionChanged;
				}

				// Handler
				void HandleScenesCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
				{
					if (Scenes.SourceCollection is List<SceneUIWrapper> scenes)
					{
						var current = Scene;
						scenes.Assign(Model.Scenes.Select(x => new SceneUIWrapper(x)));
						Scenes.Refresh();
						if (Scene == null && scenes.FirstOrDefault(x => x.SceneUI == current) is SceneUIWrapper s)
							Scenes.MoveCurrentToOrFirst(s);
						else
							Scenes.MoveCurrentToFirst();
					}
				}
			}
		}
		private Model m_model = null!;

		/// <summary>Add settings</summary>
		public SettingsData Settings => Model.Settings;

		/// <summary>The text editor control</summary>
		public ScintillaControl Editor
		{
			get => m_editor;
			set
			{
				if (m_editor == value) return;
				if (m_editor != null)
				{
					m_editor.TextChanged -= HandleTextChanged;
				}
				m_editor = value;
				if (m_editor != null)
				{
					m_editor.TextChanged += HandleTextChanged;
				}

				// Handler
				void HandleTextChanged(object sender, EventArgs e)
				{
					SaveNeeded = true;
				}
			}
		}
		private ScintillaControl m_editor = null!;

		/// <summary>The available scenes</summary>
		public ICollectionView Scenes { get; }

		/// <summary>The scene that this script renders to</summary>
		public SceneUI? Scene => Scenes.CurrentAs<SceneUIWrapper>()?.SceneUI;

		/// <summary>Context id for objects created by this scene</summary>
		public Guid ContextId { get; }

		/// <summary>The filepath for this script</summary>
		public string Filepath
		{
			get => m_filepath;
			set
			{
				if (m_filepath == value) return;

				// Update the ScriptName if the new filepath isn't a temp script, and the old name was not set by the user
				var update_name = !Model.IsTempScriptFilepath(value) && (Model.IsGeneratedScriptName(ScriptName) || ScriptName == Path_.FileTitle(m_filepath));

				// Set the new filepath
				m_filepath = value;

				// Update the script name if it hasn't been changed by the user
				if (update_name)
					ScriptName = Path_.FileTitle(m_filepath);
			}
		}
		private string m_filepath = null!;

		/// <summary>Auto complete provider for LDraw script</summary>
		private View3d.AutoComplete LdrAutoComplete;

		/// <summary>Text file types that can be edited in the script UI</summary>
		private string EditableFilesFilter => Util.FileDialogFilter("Script Files", "*.ldr", "Text Files", "*.txt", "Comma Separated Values", "*.csv");

		/// <summary>Load script from a file</summary>
		public void LoadFile(string? filepath = null)
		{
			// Prompt for a filepath if not given
			if (filepath == null || filepath.Length == 0)
			{
				var dlg = new OpenFileDialog { Title = "Load Script", Filter = EditableFilesFilter };
				if (dlg.ShowDialog(App.Current.MainWindow) != true) return;
				filepath = dlg.FileName ?? throw new Exception("Invalid filepath selected");
			}

			// Save the filepath
			Filepath = filepath;

			// Load the file into the editor
			Editor.Text = File.ReadAllText(filepath);
			SaveNeeded = false;
		}
		public void LoadFile()
		{
			LoadFile(Filepath);
		}

		/// <summary>Save the script in this editor to a file</summary>
		public void SaveFile(string? filepath = null)
		{
			// Prompt for a filepath if not given
			if (filepath == null || filepath.Length == 0)
			{
				var dlg = new SaveFileDialog { Title = "Save Script", Filter = EditableFilesFilter };
				if (dlg.ShowDialog(App.Current.MainWindow) != true) return;
				filepath = dlg.FileName ?? throw new Exception("Invalid filepath selected");
			}

			// Save the filepath
			Filepath = filepath;

			// Save the file from the editor
			File.WriteAllText(filepath, Editor.Text);
			SaveNeeded = false;
		}
		public void SaveFile()
		{
			SaveFile(Filepath);
		}

		/// <summary>True if the script has been edited without being saved</summary>
		public bool SaveNeeded
		{
			get => m_save_needed;
			set
			{
				if (m_save_needed == value) return;
				m_save_needed = value;

				// Add/Remove a '*' from the tab
				DockControl.TabText = DockControl.TabText.TrimEnd('*') + (m_save_needed ? "*" : string.Empty);

				// Notify
				NotifyPropertyChanged(nameof(SaveNeeded));
			}
		}
		private bool m_save_needed;

		/// <summary>Render the contents of this script file in the selected scene</summary>
		public Command Render { get; }
		private void RenderInternal()
		{
			SaveFile();
			if (Scene == null) return;
			Model.AddScript(Editor.Text, scenes: new[] { Scene });
		}

		/// <summary>Save the contents of the script to file</summary>
		public Command SaveScript { get; }
		private void SaveScriptInternal()
		{
			SaveFile();
		}

		/// <summary>Remove objects associated with this script from the scene</summary>
		public Command RemoveObjects { get; }
		private void RemoveObjectsInternal()
		{
			if (Scene == null) return;
			Model.Clear(new[] { Scene }, new[] { ContextId }, 1, 0);
		}

		/// <summary></summary>
		public event PropertyChangedEventHandler? PropertyChanged;
		private void NotifyPropertyChanged(string prop_name)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
		}

		/// <summary>This is a fix for a weird bug that prevents SceneUI being used in a combo box</summary>
		private class SceneUIWrapper
		{
			public SceneUIWrapper(SceneUI scene) { SceneUI = scene; }
			public SceneUI SceneUI { get; }
			public string SceneName => SceneUI.SceneName;
		}
	}
}
