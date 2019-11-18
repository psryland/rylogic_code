using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using ICSharpCode.AvalonEdit.Indentation;
using ICSharpCode.AvalonEdit.Search;
using Microsoft.Win32;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Gfx;
using Rylogic.Gui.WPF;
using Rylogic.Script;
using Rylogic.Str;
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

		static ScriptUI()
		{
			// Register the LdrScript syntax definitions
			var stream = typeof(ScriptUI).Assembly.GetManifestResourceStream("LDraw.res.LdrSyntaxRules.xshd");
			using var reader = new XmlTextReader(stream);
			var syntax_rules = HighlightingLoader.Load(reader, HighlightingManager.Instance);
			HighlightingManager.Instance.RegisterHighlighting("Ldr", new[] { ".ldr" }, syntax_rules);

		}
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
			Model = model;
			ContextId = context_id;
			Filepath = filepath;
			ScriptName = name;
			Editor = m_editor;
			LdrAutoComplete = new View3d.AutoComplete();
			AvailableScenes = new ListCollectionView(new List<SceneWrapper>());

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
			//todo if (Model != null && Editor != null && Editor.TextLength == 0 && Model.IsTempScriptFilepath(Filepath))
			//todo 	Path_.DelFile(Filepath, fail_if_missing: false);

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
					m_dock_control.ActiveChanged -= HandleActiveChanged;
					m_dock_control.SavingLayout -= HandleSavingLayout;
					Util.Dispose(ref m_dock_control!);
				}
				m_dock_control = value;
				if (m_dock_control != null)
				{
					m_dock_control.SavingLayout += HandleSavingLayout;
					m_dock_control.ActiveChanged += HandleActiveChanged;
				}

				// Handlers
				void HandleActiveChanged(object sender, ActiveContentChangedEventArgs e)
				{
					// When activated, restore focus to the editor
					if (DockControl.IsActiveContent)
						Editor.Focus();

					//	Options.BkColour = args.ContentNew == this ? Color.LightSteelBlue : Color.LightGray;
					//	Invalidate();
				}
				void HandleSavingLayout(object sender, DockContainerSavingLayoutEventArgs e)
				{
					if (!Model.IsTempScriptFilepath(Filepath))
						e.Node.Add2(nameof(Filepath), Filepath, false);
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
					var scenes = (List<SceneWrapper>)AvailableScenes.SourceCollection;
					scenes.Sync(Model.Scenes.Select(x => new SceneWrapper(x, this)));
					if (!SelectedScenes.Any() && scenes.Count != 0) scenes[0].Selected = true;
					AvailableScenes.Refresh();
				}
			}
		}
		private Model m_model = null!;

		/// <summary>Add settings</summary>
		public SettingsData Settings => Model.Settings;

		/// <summary>The text editor control</summary>
		public TextEditor Editor
		{
			get => m_text_editor;
			private set
			{
				if (m_text_editor == value) return;
				if (m_text_editor != null)
				{
					// Unhook handlers
					m_text_editor.PreviewKeyDown -= HandleKeyDown;

					// Release the template fields collection
					m_fields = null!;

					// Uninstall folding support
					m_folding_timer.Stop();
					FoldingManager.Uninstall(m_folding_mgr);
					m_folding_mgr = null!;

					// Uninstall search support
					m_search_ui.Uninstall();
					m_search_ui = null!;
				}
				m_text_editor = value;
				if (m_text_editor != null)
				{
					// Add support for searching
					m_search_ui = SearchPanel.Install(m_text_editor);

					// Add support for folding
					m_folding_mgr = FoldingManager.Install(m_text_editor.TextArea);
					m_folding_timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
					m_folding_timer.Tick += UpdateFoldings;
					m_folding_timer.Start();

					// Add indentation support
					m_text_editor.TextArea.IndentationStrategy = new DefaultIndentationStrategy();

					// Template fields
					m_fields = new TextSegmentCollection<TextSegment>(m_text_editor.Document);

					// Hook up handlers
					m_text_editor.PreviewKeyDown += HandleKeyDown;
				}

				// Handlers
				void HandleKeyDown(object sender, KeyEventArgs e)
				{
					switch (e.Key)
					{
					case Key.Space:
						{
							// Display the auto complete or call tip
							if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
							{
								if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
									ShowCallTip();
								else
									ShowAutoComplete();
								e.Handled = true;
							}
							break;
						}
					case Key.Up:
					case Key.Down:
						{
							// Quickly navigate to "<field>" or "Se|ect" fields
							if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && m_fields.Count != 0)
							{
								// Clear the selection so that find isn't limited to the selected text
								if (e.Key == Key.Up)
									Editor.Select(Editor.SelectionStart, 0);
								else
									Editor.Select(Editor.SelectionStart + Editor.SelectionLength, 0);

								// Search for a field identifier
								var seg = m_fields.FindFirstSegmentWithStartAfter(Editor.CaretOffset);
								if (e.Key == Key.Down)
									seg = seg != null ? seg : m_fields.FirstSegment;
								if (e.Key == Key.Up)
									seg = seg != null ? m_fields.GetPreviousSegment(seg) : m_fields.LastSegment;
								if (seg == null)
									break;

								var doc = Editor.Document;
								var text_length = doc.TextLength;

								// Expand the range to include '[' and ']' characters
								int beg = seg.StartOffset, end = seg.EndOffset;
								for (; beg > 0 && doc.GetCharAt(beg - 1) == '['; --beg) { }
								for (; end < text_length && doc.GetCharAt(end) == ']'; ++end) { }
								Editor.Select(beg, end - beg);
								e.Handled = true;
							}
							break;
						}
					case Key.D1:
					case Key.D2:
					case Key.D3:
					case Key.D4:
					case Key.D5:
					case Key.D6:
					case Key.D7:
					case Key.D8:
						{
							if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
							{
								var fold = !Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);
								var level = (int)e.Key - (int)Key.D1;

								var stack = new Stack<FoldingSection>();
								foreach (var folding in m_folding_mgr.AllFoldings)
								{
									for (; stack.Count != 0 && !stack.Peek().Contains(folding);)
										stack.Pop();

									// Fold/Unfold everything deeper than 'level'
									if (stack.Count >= level)
										folding.IsFolded = fold;

									stack.Push(folding);
								}

								e.Handled = true;
							}
							break;
						}
					}
				}
				void ShowAutoComplete()
				{
					// Find the start of the word
					var pos = Editor.CaretOffset;
					var line = Editor.Document.GetLineByOffset(pos);
					var text = Editor.Document.GetText(line.Offset, pos - line.Offset);
					var b = text.LastIndexOf(x => !IsLdrObjectTypeChar(x));
					var partial = text.Substring(b + 1);

					// Determine the ldr script object parent
					var start = pos - partial.Length;
					var address = View3d.AddressAt(Editor.Document.GetText(0, start));

					// Get the completion possibilities
					var templates = LdrAutoComplete.Lookup(partial, true, LdrAutoComplete[address]);

					// Display the auto completion matches
					var completion_ui = new CompletionWindow(Editor.TextArea);
					completion_ui.CompletionList.CompletionData.AddRange(templates.Select(x => new CompletionItem(x, address, start, DoAutoComplete)));
					if (completion_ui.CompletionList.CompletionData.Count != 0)
						completion_ui.Show();
				}
				void ShowCallTip()
				{
					// Determine the ldr script object parent and lookup the template
					// Note: text.Length != Editor.CurrentPos because of multi-byte characters
					var address = View3d.AddressAt(Editor.Document.GetText(0, Editor.CaretOffset));
					var template = LdrAutoComplete[address];
					if (template == null)
						return;

					var flags = View3d.AutoComplete.EExpandFlags.OptionalChildTemplates | View3d.AutoComplete.EExpandFlags.Optionals;
					var insight = new InsightWindow(Editor.TextArea)
					{
						Content = View3d.AutoComplete.ExpandTemplate(template, flags, 0, "  "),
						SizeToContent = SizeToContent.WidthAndHeight
					};
					insight.Show();
				}
				void DoAutoComplete(CompletionItem completion)
				{
					var doc = Editor.Document;
					var text_length = doc.TextLength;

					// Determine the range of the text to replace
					var end = completion.StartOffset;
					for (; end != text_length && IsLdrObjectTypeChar(doc.GetCharAt(end)); ++end) { }

					// Replace with the expanded template
					var indent_level = 0;
					var text = View3d.AutoComplete.ExpandTemplate(completion.Template, View3d.AutoComplete.EExpandFlags.Optionals, indent_level, Editor.Options.IndentationString);
					doc.Replace(completion.StartOffset, end - completion.StartOffset, text);

					// Update the collection of fields
					m_fields.Clear();
					const string field_pattern = @"(<\w+>)|((?:\w+\s*\|\s*)+\w+)";
					var search = SearchStrategyFactory.Create(field_pattern, true, false, SearchMode.RegEx);
					m_fields.AddRange(search.FindAll(doc, 0, doc.TextLength).Cast<TextSegment>());
				}
				void UpdateFoldings(object sender, EventArgs e)
				{
					var foldings = new List<NewFolding>();
					using var reader = Editor.Document.CreateReader();
					using var src = (Src)new TextReaderSrc(reader);
					var com = new InComment();

					var last_newline_offset = 0;
					var start_offsets = new Stack<int>();
					var embedded_offset = int.MaxValue;
					foreach (var ch in src.WithIndex())
					{
						if (com.WithinComment(src, 0))
							continue;

						if (ch == '\n')
						{
							last_newline_offset = ch.Index + 1;
						}
						else if (ch == '{')
						{
							start_offsets.Push(ch.Index);
						}
						else if (ch == '}' && start_offsets.Count != 0)
						{
							var start = start_offsets.Pop();
							if (start < last_newline_offset)
								foldings.Add(new NewFolding(start, ch.Index + 1));
						}
						else if (src.Match("#embedded"))
						{
							embedded_offset = ch.Index + "#embedded".Length;
						}
						else if (src.Match("#end"))
						{
							if (embedded_offset < last_newline_offset)
								foldings.Add(new NewFolding(embedded_offset, ch.Index + "#end".Length));
							embedded_offset = int.MaxValue;
						}
					}
					foldings.Sort((a, b) => a.StartOffset.CompareTo(b.StartOffset));
					m_folding_mgr.UpdateFoldings(foldings, -1);
				}
			}
		}
		private TextEditor m_text_editor = null!;
		private SearchPanel m_search_ui = null!;
		private FoldingManager m_folding_mgr = null!;
		private DispatcherTimer m_folding_timer = null!;
		private TextSegmentCollection<TextSegment> m_fields = null!;

		/// <summary>The available scenes</summary>
		public ICollectionView AvailableScenes { get; }

		/// <summary>The scene that this script renders to</summary>
		public IEnumerable<SceneUI> SelectedScenes
		{
			get
			{
				var available = (List<SceneWrapper>)AvailableScenes.SourceCollection;
				return available.Where(x => x.Selected).Select(x => x.SceneUI);
			}
		}

		/// <summary>A string description of the output scenes for this script</summary>
		public string SelectedScenesDescription
		{
			get
			{
				var desc = string.Join(",", SelectedScenes.Select(x => x.SceneName));
				return desc.Length != 0 ? desc : "None";
			}
		}

		/// <summary>True if there is a scene to render to</summary>
		public bool CanRender => SelectedScenes.Any();

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
		private View3d.AutoComplete LdrAutoComplete { get; }

		/// <summary>Text file types that can be edited in the script UI</summary>
		private string EditableFilesFilter => Util.FileDialogFilter("Script Files", "*.ldr", "Text Files", "*.txt", "Comma Separated Values", "*.csv");

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
			var scenes = SelectedScenes.ToArray();
			if (scenes.Length == 0) return;
			Model.AddScript(Editor.Text, scenes, ContextId);
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
			var scenes = SelectedScenes.ToArray();
			if (scenes.Length == 0) return;
			Model.Clear(scenes, new[] { ContextId }, 1, 0);
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

		/// <summary>Character class for Ldr Object types</summary>
		private static bool IsLdrObjectTypeChar(char ch)
		{
			return char.IsLetterOrDigit(ch) || ch == '*' || ch == '_';
		}

		/// <summary>Binding wrapper for a scene</summary>
		private class SceneWrapper
		{
			// Notes:
			//  - This wrapper is needed because when UIElement objects are used as the items
			//    of a combo box it treats them as child controls, becoming their parent.

			private readonly ScriptUI m_owner;
			public SceneWrapper(SceneUI scene, ScriptUI owner)
			{
				SceneUI = scene;
				m_owner = owner;
			}

			/// <summary>The wrapped scene</summary>
			public SceneUI SceneUI { get; }

			/// <summary>The name of the wrapped scene</summary>
			public string SceneName => SceneUI.SceneName;

			/// <summary>True if the scene is selected</summary>
			public bool Selected
			{
				get => m_selected;
				set
				{
					m_selected = value;
					m_owner.NotifyPropertyChanged(nameof(ScriptUI.SelectedScenes));
					m_owner.NotifyPropertyChanged(nameof(ScriptUI.SelectedScenesDescription));
					m_owner.NotifyPropertyChanged(nameof(ScriptUI.CanRender));
				}
			}
			private bool m_selected;

			/// <summary></summary>
			public static implicit operator SceneUI?(SceneWrapper? x) => x?.SceneUI;

			/// <summary></summary>
			public override bool Equals(object obj)
			{
				return obj is SceneWrapper wrapper && ReferenceEquals(SceneUI, wrapper.SceneUI);
			}
			public override int GetHashCode()
			{
				return SceneUI.GetHashCode();
			}
		}

		/// <summary>Auto completion item</summary>
		private class CompletionItem :ICompletionData
		{
			private readonly Action<CompletionItem> m_on_complete;
			public CompletionItem(View3d.AutoComplete.Template template, string parent_address, int start_offset, Action<CompletionItem> on_complete)
			{
				Template = template;
				LdrParentAddress = parent_address;
				StartOffset = start_offset;
				m_on_complete = on_complete;
			}
			public View3d.AutoComplete.Template Template { get; }
			public int StartOffset { get; }
			public string LdrParentAddress { get; }
			public ImageSource? Image => null;
			public string Text => $"*{Template.Keyword}";
			public object Content => Text;
			public object? Description => null;
			public double Priority => 1.0;
			public void Complete(TextArea text_area, ISegment completionSegment, EventArgs e) => m_on_complete(this);
		}
	}
}






#if false
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
					m_editor.PreviewKeyDown -= HandlePreviewKeyDown;
					m_editor.AutoComplete -= HandleAutoComplete;
					m_editor.AutoCompleteSelection -= HandleAutoCompleteSelection;
					m_editor.SelectionChanged -= HandleSelectionChanged;
					m_editor.TextChanged -= HandleTextChanged;
					m_editor.Name = string.Empty;
				}
				m_editor = value;
				if (m_editor != null)
				{
					m_editor.Name = ScriptName;
					m_editor.AutoCompleteIgnoreCase = true;
					m_editor.AutoCompleteSeparator = '\u001b';
					m_editor.AutoCompleteMaxWidth = 80;
					m_editor.TextChanged += HandleTextChanged;
					m_editor.SelectionChanged += HandleSelectionChanged;
					m_editor.AutoCompleteSelection += HandleAutoCompleteSelection;
					m_editor.AutoComplete += HandleAutoComplete;
					m_editor.PreviewKeyDown += HandlePreviewKeyDown;
					m_editor.CallTip += HandleCallTip;
				}

				// Handler
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
					// Use 'IsWordChar' to set the partial word so that '*' characters are included.
					var line = Editor.GetCurLine(out var caret_offset);
					var i = caret_offset;
					for (; i-- != 0 && IsWordChar(line[i]);) { }
					e.PartialWord = line.Substring(i + 1, caret_offset - i - 1);

					// Determine the ldr script object parent
					var address = View3d.AddressAt(Editor.TextRange(0L, e.Position - e.PartialWord.Length));

					// Return child templates and root level templates
					var templates = LdrAutoComplete.Lookup(e.PartialWord, true, LdrAutoComplete[address]);
					e.Completions.Assign(templates.Select(x => $"*{x.Keyword}"));
					e.Handled = true;
				}
				void HandleAutoCompleteSelection(object sender, ScintillaControl.AutoCompleteSelectionEventArgs e)
				{
					// Determine the ldr script object parent
					var address = View3d.AddressAt(Editor.TextRange(0L, e.Position));
					address = $"{address}{(address.Length != 0 ? "." : "")}{e.Completion}";

					// Find the selected template
					var template = LdrAutoComplete[address];
					if (template == null)
						return;

					var line_index = Editor.LineIndexFromPosition(e.Position);
					var line_start = Editor.LineRange(line_index).Beg;
					var line = Editor.GetLine(line_index);

					// Use 'IsWordChar' to set the character range to be replaced
					long beg = (int)(e.Position - line_start), end = beg;
					for (; end != line.Length && IsWordChar(line[(int)end]); ++end) { }
					e.ReplaceRange = new Range(line_start + beg, line_start + end);

					// Set the completion text. The default caret position should still be correct
					e.Completion = View3d.AutoComplete.ExpandTemplate(template, View3d.AutoComplete.EExpandFlags.Optionals, Editor.LineIndentationLevel(line_index), Editor.IndentString);
					e.Handled = true;
				}
				void HandleCallTip(object sender, ScintillaControl.CallTipEventArgs e)
				{
					// Determine the ldr script object parent and lookup the template
					// Note: text.Length != Editor.CurrentPos because of multi-byte characters
					var address = View3d.AddressAt(Editor.TextRange(0L, Editor.CurrentPos));
					var template = LdrAutoComplete[address];
					if (template == null)
						return;

					var flags = View3d.AutoComplete.EExpandFlags.OptionalChildTemplates | View3d.AutoComplete.EExpandFlags.Optionals;
					e.Definition = View3d.AutoComplete.ExpandTemplate(template, flags, 0, "  ");
					e.Handled = true;
				}
				void HandlePreviewKeyDown(object sender, KeyEventArgs e)
				{
					// Quickly navigate to "<field>" or "Se|ect" fields
					if ((e.Key == Key.Down || e.Key == Key.Up) && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
					{
						// Clear the selection so that find isn't limited to the selected text
						if (e.Key == Key.Up)
							Editor.CurrentPos = Editor.Anchor = Editor.SelectionStart;
						else
							Editor.CurrentPos = Editor.Anchor = Editor.SelectionEnd;

						// Search for a field identifier
						const string field_pattern = @"(<\w+>)|((?:\w+\s*\|\s*)+\w+)";
						var rng = e.Key == Key.Up
							? Editor.Find(Sci.EFindOption.Regexp | Sci.EFindOption.Cxx11regex, field_pattern, new Range(Editor.CurrentPos, 0))
							: Editor.Find(Sci.EFindOption.Regexp | Sci.EFindOption.Cxx11regex, field_pattern, new Range(Editor.CurrentPos, int.MaxValue));

						// If found, select the field and consume the tab character
						if (!rng.Empty)
						{
							// Expand the range to include '[' and ']' characters
							var line = Editor.LineFromPosition(rng.Beg, out var line_range);
							for (int i = (int)(rng.Beg - line_range.Beg); i - 1 != -1 && line[i - 1] == '['; --rng.Beg, --i) { }
							for (int i = (int)(rng.End - line_range.Beg); i != line.Length && line[i] == ']'; ++rng.End, ++i) { }
							Editor.Selection = rng;
							Editor.ScrollCaret();
							e.Handled = true;
						}
					}
				}
				static bool IsWordChar(char ch)
				{
					return char.IsLetterOrDigit(ch) || ch == '*' || ch == '_';
				}
			}
		}
		private ScintillaControl m_editor = null!;
#endif
