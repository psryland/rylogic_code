using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using Microsoft.Win32;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Gfx;
using Rylogic.Gui.WPF;
using Rylogic.Script;
using Rylogic.Utility;

namespace LDraw.UI
{
	public sealed partial class ScriptUI :UserControl, IDockable, IDisposable, INotifyPropertyChanged
	{
		// Notes:
		//  - All scripts are created with an associated file. New scripts start with a
		//    temporary script filepath and get saved to a new location by the user.
		//  - The Scenes collection view uses a wrapper around SceneUI because Combobox.ItemsSource
		//    treats the items as child controls and tried to become their parent.

		public ScriptUI(Model model, string name, string filepath, Guid context_id)
		{
			InitializeComponent();
			DockControl = new DockControl(this, $"Script-{context_id}")
			{
				ShowTitle = false,
				TabText = name,
				TabCMenu = TabCMenu(),
				DestroyOnClose = true,
			};
			Bind = new BindingWrapper(this);
			Model = model;
			ContextId = context_id;
			Filepath = filepath;
			ScriptName = name;
			Editor = m_scintilla_control;
			Scenes = new ListCollectionView(new List<SceneUI.BindingWrapper>());
			m_ldr_auto_complete = new View3d.AutoComplete();

			Render = Command.Create(this, RenderInternal);
			SaveScript = Command.Create(this, SaveScriptInternal);
			RemoveObjects = Command.Create(this, RemoveObjectsInternal);
			CloseScript = Command.Create(this, CloseScriptInternal);

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
					Dispatcher.BeginInvoke(() => HandleScenesCollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset)));
				}

				// Handlers
				void HandleScenesCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
				{
					// Refresh the scenes collection
					var scenes = (List<SceneUI.BindingWrapper>)Scenes.SourceCollection;
					using var restore_current = Scope.Create(() => Scenes.CurrentItem, s => Scenes.MoveCurrentToOrFirst(s));
					scenes.Assign(Model.Scenes.Select(x => x.Bind));
					Scenes.Refresh();
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
					m_editor.CallTip -= HandleCallTip;
					m_editor.AutoComplete -= HandleAutoComplete;
					m_editor.AutoCompleteSelection -= HandleAutoCompleteSelection;
					m_editor.SelectionChanged -= HandleSelectionChanged;
					m_editor.TextChanged -= HandleTextChanged;
					m_editor.HandleCreated -= HandleSCHandleCreated;
				}
				m_editor = value;
				if (m_editor != null)
				{
					m_editor.HandleCreated += HandleSCHandleCreated;
					m_editor.TextChanged += HandleTextChanged;
					m_editor.SelectionChanged += HandleSelectionChanged;
					m_editor.AutoCompleteSelection += HandleAutoCompleteSelection;
					m_editor.AutoComplete += HandleAutoComplete;
					m_editor.CallTip += HandleCallTip;
				}

				// Handler
				void HandleSCHandleCreated(object sender, EventArgs e)
				{
					m_editor.AutoCompleteIgnoreCase = true;
					m_editor.AutoCompleteSeparator = '\u001b';
					m_editor.AutoCompleteMaxWidth = 80;
				}
				void HandleTextChanged(object sender, EventArgs e)
				{
					SaveNeeded = true;
				}
				void HandleSelectionChanged(object sender, EventArgs e)
				{
					// Caret moved
				}
				void HandleAutoComplete(object sender, ScintillaControl.AutoCompleteEventArgs e)
				{
					e.Completions.Assign(m_ldr_auto_complete.Lookup(e.Partial).Select(x => $"*{x.Keyword}"));
					e.Handled = true;
				}
				void HandleAutoCompleteSelection(object sender, ScintillaControl.AutoCompleteSelectionEventArgs e)
				{
					// Cancel the auto complete because adding the template text is manageed here
					e.Cancel = true;

					// Find the selected template
					var template = m_ldr_auto_complete.Lookup(e.Completion).FirstOrDefault();
					if (template == null)
						return;

					var line = Editor.GetCurLine(out var caret_offset);

					// Determine the indent level and style
					var indent_level = line.CountWhile(x => x == ' ' || x == '\t');
					var indent_text = indent_level != 0 && line[0] == ' ' ? "    " : "\t";
					if (indent_text[0] == ' ') indent_level /= indent_text.Length;

					// Delete text from 'StartPosition' to the first white space after the caret position
					Editor.TargetBeg = e.Position;
					Editor.TargetEnd = Editor.CurrentPos + line.Skip(caret_offset).CountWhile(x => Str_.IsIdentifier(x, false));
					Editor.ReplaceTarget(string.Empty);

					// Create the auto complete handler
					m_auto_completer = new AutoCompleter(template, e.Position, indent_level, indent_text);

					// Add the initial auto complete text
					using var caret = Editor.SelectionScope();
					Editor.InsertText(e.Position, m_auto_completer.Text.ToString());

					// todo:
					//   restore caret position
					//   
				}
				void HandleCallTip(object sender, ScintillaControl.CallTipEventArgs e)
				{
					var line = Editor.GetCurLine(out var caret_offset);

					// Search backward from the caret position to the prior keyword
					for (; caret_offset != 0 && line[caret_offset] != '*'; --caret_offset) { }
					if (line[caret_offset] != '*')
						return;

					// Read the keyword
					if (!Extract.Identifier(out var keyword, new StringSrc(line, caret_offset + 1)))
						return;

					// Lookup the templates that match this keyword
					int index = 0;
					var def = new StringBuilder();
					foreach (var template in m_ldr_auto_complete.Lookup(keyword))
						def.Append((char)++index).Append(template.Description).Append('\n');

					// todo: use a template description where child templates are named but not expanded

					e.Definition = def.ToString().Summary(20);
					e.Handled = true;
				}
			}
		}
		private ScintillaControl m_editor = null!;

		/// <summary>The available scenes</summary>
		public ICollectionView Scenes { get; }

		/// <summary>The scene that this script renders to</summary>
		public SceneUI? Scene => Scenes.CurrentAs<SceneUI.BindingWrapper>();

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
		private View3d.AutoComplete m_ldr_auto_complete;

		/// <summary>Auto complete handler</summary>
		private AutoCompleter? m_auto_completer;

		/// <summary>Text file types that can be edited in the script UI</summary>
		private string EditableFilesFilter => Util.FileDialogFilter("Script Files", "*.ldr", "Text Files", "*.txt", "Comma Separated Values", "*.csv");

		/// <summary>A wrapper for binding to this scene</summary>
		public BindingWrapper Bind { get; }

		/// <summary>Return the tab context menu</summary>
		private ContextMenu TabCMenu()
		{
			var cmenu = (ContextMenu)FindResource("TabCMenu");
			cmenu.DataContext = this;
			return cmenu;
		}

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

		/// <summary>Close and remove this script</summary>
		public Command CloseScript { get; }
		private void CloseScriptInternal()
		{
			Model.Scripts.Remove(this);
			Dispose();
		}

		/// <summary></summary>
		public event PropertyChangedEventHandler? PropertyChanged;
		private void NotifyPropertyChanged(string prop_name)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
		}

		/// <summary></summary>
		public class BindingWrapper
		{
			// Notes:
			//  - This wrapper is needed because when UIElement objects are used as the items
			//    of a combo box it treats them as child controls, becoming their parent.

			public BindingWrapper(ScriptUI script) { ScriptUI = script; }
			public ScriptUI ScriptUI { get; }
			public string ScriptName => ScriptUI.ScriptName;
			public static implicit operator ScriptUI?(BindingWrapper? x) => x?.ScriptUI;
		}
	}
}
