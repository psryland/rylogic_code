using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.AddIn;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using ICSharpCode.AvalonEdit.Indentation;
using ICSharpCode.AvalonEdit.Rendering;
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
		//  - Each ScriptUI is responsible for the objects it creates and the scenes those objects are
		//    added to. Closing a ScriptUI removes its objects from any associated scenes.
		//  - Watching for changed script files is independent of the Ldr sources CheckForChangedSources
		//    because a script is not necessarily rendered in the view and therefore not a source. Both
		//    file watching systems are needed, since assets need to update as well.

		static ScriptUI()
		{
			// Register the LdrScript syntax definitions
			var stream = typeof(ScriptUI).Assembly.GetManifestResourceStream("LDraw.res.LdrSyntaxRules.xshd") ?? throw new Exception("LdrSyntaxRules.xshd embedded resource not found");
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
				TabToolTip = filepath,
				TabCMenu = TabCMenu(),
				DestroyOnClose = true,
			};
			LastUpdateTime = TimeSpan.Zero;
			Context = new Context(model, name, context_id);
			LdrAutoComplete = new View3d.AutoComplete();
			Filepath = filepath;
			Editor = m_editor;

			Render = Command.Create(this, RenderInternal);
			SaveScript = Command.Create(this, SaveScriptInternal);
			RemoveObjects = Command.Create(this, RemoveObjectsInternal);
			CloseScript = Command.Create(this, CloseScriptInternal);
			IndentSelection = Command.Create(this, IndentSelectionInternal);
			CommentOutSelection = Command.Create(this, CommentOutSelectionInternal);
			UncommentSelection = Command.Create(this, UncommentSelectionInternal);

			// If the temporary script exists, load it
			if (Path_.FileExists(Filepath))
				LoadFile();

			DataContext = this;
		}
		protected override void OnPreviewGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
		{
			base.OnPreviewGotKeyboardFocus(e);
			CheckForChangedScript();
		}
		public void Dispose()
		{
			// Remove objects from all scenes
			Context.SelectedScenes = Array.Empty<SceneUI>();
			Model.Scripts.Remove(this);

			Editor = null!;
			Context = null!;
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
				void HandleActiveChanged(object? sender, ActiveContentChangedEventArgs e)
				{
					// When activated, restore focus to the editor
					if (DockControl.IsActiveContent)
						Editor.Focus();

					//	Options.BkColour = args.ContentNew == this ? Color.LightSteelBlue : Color.LightGray;
					//	Invalidate();
				}
				void HandleSavingLayout(object? sender, DockContainerSavingLayoutEventArgs e)
				{
					if (!Model.IsTempScriptFilepath(Filepath))
						e.Node.Add2(nameof(Filepath), Filepath, false);
				}
			}
		}
		private DockControl m_dock_control = null!;

		/// <summary>The context id and scenes that associated objects are added to</summary>
		public Context Context
		{
			get => m_context;
			private set
			{
				if (m_context == value) return;
				if (m_context != null)
				{
					Log.EntriesChanged -= HandleLogEntriesChanged;
					m_context.PropertyChanged -= HandlePropertyChanged;
					Util.Dispose(ref m_context!);
				}
				m_context = value;
				if (m_context != null)
				{
					m_context.PropertyChanged += HandlePropertyChanged;
					Log.EntriesChanged += HandleLogEntriesChanged;
				}

				// Handlers
				void HandlePropertyChanged(object? sender, PropertyChangedEventArgs e)
				{
					switch (e.PropertyName)
					{
					case nameof(Context.Name):
						{
							// Update the tab text
							if (DockControl != null)
								DockControl.TabText = ScriptName ?? string.Empty;

							NotifyPropertyChanged(nameof(ScriptName));
							break;
						}
					}
				}
				void HandleLogEntriesChanged(object? sender, EventArgs e)
				{
					RefreshErrorMarkers();
				}
			}
		}
		private Context m_context = null!;

		/// <summary>Auto complete provider for LDraw script</summary>
		private View3d.AutoComplete LdrAutoComplete { get; }

		/// <summary>App logic</summary>
		public Model Model => Context.Model;

		/// <summary>The name assigned to this script UI</summary>
		public string ScriptName => Context.Name;

		/// <summary>Context id for objects created by this scene</summary>
		public Guid ContextId => Context.ContextId;

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
					Context.Name = Path_.FileName(m_filepath);

				DockControl.TabToolTip = m_filepath;
			}
		}
		private FileInfo? FileInfo => Path_.FileExists(Filepath) ? new FileInfo(Filepath) : null;
		private string m_filepath = null!;
		private FileInfo? m_last_fileinfo;

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
					m_text_editor.MouseHoverStopped += HandleMouseHoverStopped;
					m_text_editor.MouseHover += HandleMouseHover;
					m_text_editor.TextArea.Caret.PositionChanged -= HandleCaretPositionChanged;
					m_text_editor.TextArea.SelectionChanged -= HandleSelectionChanged;
					m_text_editor.Document.TextChanged -= HandleTextChanged;
					m_text_editor.PreviewKeyDown -= HandleKeyDown;

					// Drop the text marker service
					m_text_marker_service = null!;

					// Release the fields collection
					m_fields = null;

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
					// Set general properties
					m_text_editor.TextArea.SelectionCornerRadius = 1.0;

					// Add support for searching
					m_search_ui = SearchPanel.Install(m_text_editor);

					// Add support for folding
					m_folding_mgr = FoldingManager.Install(m_text_editor.TextArea);
					m_folding_timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
					m_folding_timer.Tick += UpdateFoldings;
					m_folding_timer.Start();

					// Add indentation support
					m_text_editor.TextArea.IndentationStrategy = new IndentStrategy(m_text_editor);

					// Add underline errors support
					m_text_marker_service = new TextMarkerService(m_text_editor.Document);
					m_text_editor.TextArea.TextView.BackgroundRenderers.Add(m_text_marker_service);
					m_text_editor.TextArea.TextView.LineTransformers.Add(m_text_marker_service);
					var services = (IServiceContainer?)m_text_editor.Document.ServiceProvider.GetService(typeof(IServiceContainer)) ?? throw new NullReferenceException("IServiceContainer service not available");
					services.AddService(typeof(ITextMarkerService), m_text_marker_service);

					// Adjust key bindings
					AvalonEditCommands.DeleteLine.InputGestures.Clear();
					AvalonEditCommands.IndentSelection.InputGestures.Clear();

					// Hook up handlers
					m_text_editor.PreviewKeyDown += HandleKeyDown;
					m_text_editor.Document.TextChanged += HandleTextChanged;
					m_text_editor.TextArea.SelectionChanged += HandleSelectionChanged;
					m_text_editor.TextArea.Caret.PositionChanged += HandleCaretPositionChanged;
					m_text_editor.MouseHover += HandleMouseHover;
					m_text_editor.MouseHoverStopped += HandleMouseHoverStopped;
				}

				// Handlers
				TextSegmentCollection<TextSegment> FindFields()
				{
					var doc = Editor.Document;
					const string field_pattern = @"(<\w+>)|((?:\w+\s*\|\s*)+\w+)";
					var search = SearchStrategyFactory.Create(field_pattern, true, false, SearchMode.RegEx);
					var fields = new TextSegmentCollection<TextSegment>();
					fields.AddRange(search.FindAll(doc, 0, doc.TextLength).Cast<TextSegment>());
					return fields;
				}
				void HandleKeyDown(object? sender, KeyEventArgs e)
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
							if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
							{
								var doc = Editor.Document;
								var text_length = doc.TextLength;

								// Clear the selection so that find isn't limited to the selected text
								if (e.Key == Key.Up)
									Editor.Select(Editor.SelectionStart, 0);
								else
									Editor.Select(Editor.SelectionStart + Editor.SelectionLength, 0);

								// Update the cache of field text segments
								m_fields ??= FindFields();

								// Search for a field identifier
								var seg = m_fields.FindFirstSegmentWithStartAfter(Editor.CaretOffset);
								if (e.Key == Key.Down)
								{
									seg ??= m_fields.FirstSegment;
								}
								if (e.Key == Key.Up)
								{
									if (seg != null) seg = m_fields.GetPreviousSegment(seg);
									seg ??= m_fields.LastSegment;
								}
								if (seg == null)
									break;

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
				void HandleTextChanged(object? sender, EventArgs e)
				{
					// Invalidate cached field matches
					m_fields = null;
				}
				void HandleSelectionChanged(object? sender, EventArgs e)
				{
					NotifyPropertyChanged(nameof(CaretPositionDescription));
				}
				void HandleCaretPositionChanged(object? sender, EventArgs e)
				{
					NotifyPropertyChanged(nameof(CaretPositionDescription));
					NotifyPropertyChanged(nameof(Location));
				}
				void HandleMouseHover(object? sender, MouseEventArgs e)
				{
					var view = m_text_editor.TextArea.TextView;
					var pt = e.GetPosition(view);

					// If the mouse is hovering over an error marker, display a tooltip with the error message
					if (view.GetPositionFloor(pt + view.ScrollOffset) is TextViewPosition pos)
					{
						// Look for error markers at the mouse position
						var ofs = Editor.Document.GetOffset(pos.Line, pos.Column);
						if (m_text_marker_service.GetMarkersAtOffset(ofs).FirstOrDefault() is ITextMarker marker)
						{
							m_tt_errors ??= new ToolTip();
							m_tt_errors.Closed += delegate { m_tt_errors = null; };
							m_tt_errors.PlacementTarget = view;
							m_tt_errors.Placement = PlacementMode.Mouse;
							m_tt_errors.StaysOpen = true;
							m_tt_errors.Content = new TextBlock
							{
								Text = (string?)marker.ToolTip,
								TextWrapping = TextWrapping.Wrap,
							};
							m_tt_errors.IsOpen = true;
							e.Handled = true;
						}
					}
				}
				void HandleMouseHoverStopped(object? sender, MouseEventArgs e)
				{
					if (m_tt_errors != null)
						m_tt_errors.IsOpen = false;
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

					// Determine the current indent level
					var indent_level = 0;
					var current_indent = doc.GetText(TextUtilities.GetLeadingWhitespace(doc, doc.GetLineByOffset(completion.StartOffset)));
					for (; current_indent.StartsWith(Editor.Options.IndentationString); ++indent_level)
						current_indent = current_indent.Substring(Editor.Options.IndentationString.Length);

					// Replace with the expanded template
					var text = View3d.AutoComplete.ExpandTemplate(completion.Template, View3d.AutoComplete.EExpandFlags.Optionals, indent_level, Editor.Options.IndentationString);
					doc.Replace(completion.StartOffset, end - completion.StartOffset, text);
				}
				void UpdateFoldings(object? sender, EventArgs e)
				{
					var foldings = new List<NewFolding>();
					using var reader = Editor.Document.CreateReader();
					using var src = (Src)new TextReaderSrc(reader);
					var com = new InComment();

					var last_newline_offset = 0;
					var start_offsets = new Stack<int>();
					var embedded_offset = int.MaxValue;
					foreach (var (ch, i) in src.WithIndex())
					{
						if (com.WithinComment(src, 0))
							continue;

						if (ch == '\n')
						{
							last_newline_offset = i + 1;
						}
						else if (ch == '{')
						{
							start_offsets.Push(i);
						}
						else if (ch == '}' && start_offsets.Count != 0)
						{
							var start = start_offsets.Pop();
							if (start < last_newline_offset)
								foldings.Add(new NewFolding(start, i + 1));
						}
						else if (src.Match("#embedded"))
						{
							embedded_offset = i + "#embedded".Length;
						}
						else if (src.Match("#end"))
						{
							if (embedded_offset < last_newline_offset)
								foldings.Add(new NewFolding(embedded_offset, i + "#end".Length));
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
		private TextSegmentCollection<TextSegment>? m_fields = null;
		private TextMarkerService m_text_marker_service = null!;
		private ToolTip? m_tt_errors;

		/// <summary>The current position in the editor</summary>
		public TextLocation Location
		{
			get => Editor.Document.GetLocation(Editor.CaretOffset);
			set => Editor.CaretOffset = Editor.Document.GetOffset(value);
		}
		public void ScrollTo(int line, int column, bool move_caret)
		{
			if (move_caret)
				Editor.CaretOffset = Editor.Document.GetOffset(line, column);

			Editor.ScrollTo(line, column);
		}

		/// <summary>The range of selected characters</summary>
		public string CaretPositionDescription
		{
			get
			{
				var ss = Editor.SelectionStart;
				var sl = Editor.SelectionLength;
				return sl == 0 ? $"Pos: {Editor.CaretOffset}" : $"Sel: {ss}..{ss + sl} ({sl})";
			}
		}

		/// <summary>Update the objects associated with 'ContextId' within View3D's object store</summary>
		private void UpdateObjects()
		{
			LastUpdateTime = Log.Elapsed;
			Log.Clear(x => Path_.Compare(x.File, Filepath) == 0);

			var scenes = Context.SelectedScenes.ToArray();
			var include_paths = Model.Settings.IncludePaths;
			var selection = Editor.SelectionLength != 0 ? Editor.TextArea.Selection.GetText() : null;

			// Load the script file in a background thread
			ThreadPool.QueueUserWorkItem(x =>
			{
				if (selection == null)
					Model.View3d.LoadScriptFromFile(Filepath, ContextId, include_paths, OnAdd);
				else
					Model.View3d.LoadScriptFromString(selection, ContextId, include_paths, OnAdd);

				void OnAdd(Guid id, bool before)
				{
					if (before)
						Model.Clear(scenes, id);
					else
						Model.AddObjects(scenes, id);

					RefreshErrorMarkers();
				}
			});
		}
		private TimeSpan LastUpdateTime;

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
				var dlg = new OpenFileDialog { Title = "Load Script", Filter = Model.EditableFilesFilter };
				if (dlg.ShowDialog(App.Current.MainWindow) != true) return;
				filepath = dlg.FileName ?? throw new Exception("Invalid filepath selected");
			}
			if (!File.Exists(filepath))
			{
				throw new FileNotFoundException("Load script file failed", filepath);
			}

			// Load the file into the editor
			Editor.Text = File.ReadAllText(filepath);

			// Save the filepath
			Filepath = filepath;

			// Record the file state when last loaded
			m_last_fileinfo = FileInfo;

			// Sync'd with disk
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
				var dlg = new SaveFileDialog { Title = "Save Script", Filter = Model.EditableFilesFilter };
				if (dlg.ShowDialog(App.Current.MainWindow) != true) return;
				filepath = dlg.FileName ?? throw new Exception("Invalid filepath selected");
			}

			// Save the file from the editor
			File.WriteAllText(filepath, Editor.Text);

			// Save the filepath
			Filepath = filepath;

			// Record the file state when last saved
			m_last_fileinfo = FileInfo;

			// Sync'd with disk
			SaveNeeded = false;
		}
		public void SaveFile()
		{
			SaveFile(Filepath);
		}

		/// <summary>Test the script file for external changes</summary>
		public void CheckForChangedScript()
		{
			// If this script has never been saved, no changes
			if (!File.Exists(Filepath))
				return;

			// Look for changes
			var fileinfo = FileInfo;
			if (m_last_fileinfo != null && fileinfo != null &&
				((m_last_fileinfo.LastWriteTimeUtc == fileinfo.LastWriteTimeUtc) ||
				(m_last_fileinfo.Length == fileinfo.Length && Editor.Text == File.ReadAllText(Filepath))))
			{
				m_last_fileinfo = fileinfo;
				return;
			}

			// The file has changed. Prompt if needed.
			if (SaveNeeded)
			{
				var dlg = new MsgBox(Window.GetWindow(this),
					$"{Path_.FileName(Filepath)} has changed on disk.\n" +
					$"\n" +
					$"'Overwrite' = save and replace the version on disk.\n" +
					$"'Discard' = discard local changes and reload from disk.\n" +
					$"'Ignore' = continue with the local version without saving.\n",
					"Script Changed on Disk", MsgBox.EButtons.OverwriteDiscardIgnore);
				dlg.ShowDialog();
				switch (dlg.Result)
				{
				case MsgBox.EResult.Overwrite:
					SaveFile(Filepath);
					break;
				case MsgBox.EResult.Discard:
					LoadFile(Filepath);
					break;
				case MsgBox.EResult.Ignore:
					m_last_fileinfo = fileinfo;
					SaveNeeded = true;
					break;
				default:
					throw new Exception($"{dlg.Result} unexpected");
				}
			}
			else if (!(Model.Settings.ReloadChangedScripts is bool reload))
			{
				var dlg = new MsgBox(Window.GetWindow(this),
					$"'{Filepath}' has changed on disk.\n" +
					$"\n" +
					$"'Reload' = reload the script from disk.\n" +
					$"'Ignore' = continue with the local version.\n",
					"Script Changed on Disk", MsgBox.EButtons.ReloadIgnore)
				{ ShowAlwaysCheckbox = true };
				dlg.ShowDialog();
				switch (dlg.Result)
				{
				case MsgBox.EResult.Reload:
					LoadFile(Filepath);
					if (dlg.Always) Model.Settings.ReloadChangedScripts = true;
					break;
				case MsgBox.EResult.Ignore:
					if (dlg.Always) Model.Settings.ReloadChangedScripts = false;
					m_last_fileinfo = fileinfo;
					SaveNeeded = true;
					break;
				default:
					throw new Exception($"{dlg.Result} unexpected");
				}
			}
			else if (reload)
			{
				LoadFile(Filepath);
			}
		}

		/// <summary>Update the error markers into this script</summary>
		private void RefreshErrorMarkers()
		{
			m_text_marker_service.RemoveAll(_ => true);
			foreach (var le in Log.Entries)
			{
				if (Path_.Compare(le.File, Filepath) != 0) continue;
				if (le.Elapsed < LastUpdateTime) continue;

				var line = Editor.Document.GetLineByNumber(le.Line);
				var marker = m_text_marker_service.Create(line.Offset, line.Length);
				marker.MarkerTypes = ETextMarkerTypes.SquigglyUnderline | ETextMarkerTypes.LineInScrollBar;
				marker.MarkerColor = Colors.Red;
				marker.ToolTip = le.Message;
			}
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
			if (SaveNeeded)
				SaveFile();

			var scenes = Context.SelectedScenes.ToArray();
			if (scenes.Length == 0)
				return;

			UpdateObjects();
		}

		/// <summary>Save the contents of the script to file</summary>
		public Command SaveScript { get; }
		private void SaveScriptInternal()
		{
			SaveFile();
		}

		/// <summary>Remove objects associated with this script from the selected scenes</summary>
		public Command RemoveObjects { get; }
		private void RemoveObjectsInternal()
		{
			var scenes = Context.SelectedScenes.ToArray();
			if (scenes.Length == 0) return;
			Model.Clear(scenes, ContextId);
		}

		/// <summary>Close and remove this script</summary>
		public Command CloseScript { get; }
		private void CloseScriptInternal()
		{
			Dispose();
		}

		/// <summary>Indent the selected text</summary>
		public Command IndentSelection { get; }
		private void IndentSelectionInternal()
		{
			AvalonEditCommands.IndentSelection.Execute(null, null);
		}

		/// <summary>Comment out the selected lines using line comments</summary>
		public Command CommentOutSelection { get; }
		private void CommentOutSelectionInternal()
		{
			var doc = Editor.Document;
			var beg = doc.GetLineByOffset(Editor.SelectionStart);
			var end = doc.GetLineByOffset(Editor.SelectionStart + Editor.SelectionLength);

			// Find the smallest indent whitespace
			int len = int.MaxValue;
			for (var line = beg; line != null && line.Offset <= end.Offset; line = line.NextLine)
			{
				var segment = TextUtilities.GetLeadingWhitespace(doc, line);
				if (segment.Length > len) continue;
				len = segment.Length;
			}
			for (var line = beg; line != null && line.Offset <= end.Offset; line = line.NextLine)
			{
				var segment = doc.GetText(line.Offset, len);
				doc.Replace(line.Offset, len, segment + "//");
			}
		}

		/// <summary>Remove one layer of line comments from the selected lines</summary>
		public Command UncommentSelection  { get; }
		private void UncommentSelectionInternal()
		{
			var doc = Editor.Document;
			var beg = doc.GetLineByOffset(Editor.SelectionStart);
			var end = doc.GetLineByOffset(Editor.SelectionStart + Editor.SelectionLength);
			for (var line = beg; line != null && line.Offset <= end.Offset; line = line.NextLine)
			{
				var segment = TextUtilities.GetLeadingWhitespace(doc, line);
				if (line.EndOffset - segment.EndOffset >= 2 && doc.GetText(segment.EndOffset, 2) == "//")
					doc.Replace(segment.EndOffset, 2, string.Empty);
			}
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

		/// <summary>Indentation strategy for the script editor</summary>
		private class IndentStrategy :IIndentationStrategy
		{
			private readonly TextEditor m_editor;
			public IndentStrategy(TextEditor editor)
			{
				m_editor = editor;
			}
			public void IndentLine(TextDocument document, DocumentLine line)
			{
				if (line.PreviousLine != null)
				{
					// Read the indent from the previous line
					var prev_indent_seg = TextUtilities.GetLeadingWhitespace(document, line.PreviousLine);

					// Get the current indentation from 'line'
					var curr_indent_seg = TextUtilities.GetLeadingWhitespace(document, line);
					
					// The indent text from the previous line
					var indent = document.GetText(prev_indent_seg);

					//// If the previous line starts with '{', add one indent
					//if (prev_indent_seg.EndOffset < document.TextLength &&
					//	document.GetText(prev_indent_seg.EndOffset, 1) == "{")
					//	indent += m_editor.Options.IndentationString;
					//
					//// If the current line starts with '}', remove one indent
					//if (curr_indent_seg.EndOffset < document.TextLength && 
					//	document.GetText(curr_indent_seg.EndOffset, 1) == "}" &&
					//	indent.EndsWith(m_editor.Options.IndentationString))
					//	indent = indent.Substring(0, indent.Length - m_editor.Options.IndentationString.Length);

					// Copy the indentation to 'line'. 'OffsetChangeMappingType.RemoveAndInsert' guarantees the caret moves behind the new indentation.
					document.Replace(curr_indent_seg.Offset, curr_indent_seg.Length, indent, OffsetChangeMappingType.RemoveAndInsert);
				}
			}
			public void IndentLines(TextDocument document, int line_beg, int line_end)
			{
				for (var i = line_beg; i != line_end; ++i)
				{
					var line = document.GetLineByNumber(i);
					IndentLine(document, line);
				}
			}
		}
	}
}
